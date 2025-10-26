using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models;
using WebFindLove.Models.Services.MatchingService;
using WebFindLove.Models.Services.MatchResultService;

namespace WebFindLove.Controllers
{
    [Authorize]
    public class MatchingController : BaseController
    {
        private readonly IMatchingService _matchingService;
        private readonly IMatchResultService _matchResultService;
        private readonly ILogger<MatchingController> _logger;

        public MatchingController(
            IMatchingService matchingService,
            IMatchResultService matchResultService,
            ILogger<MatchingController> logger)
        {
            _matchingService = matchingService;
            _matchResultService = matchResultService;
            _logger = logger;
            Logger = logger;
            _logger.LogInformation("MatchingController initialized");
        }

        // GET: Matching
        public IActionResult Index()
        {
            _logger.LogInformation("GET Matching Index - User: {Username}", CurrentUser?.UserName);
            return View();
        }

        // GET: Matching/FindMatches - Tìm người phù hợp với mình (one-way)
        public async Task<IActionResult> FindMatches()
        {
            _logger.LogInformation("GET FindMatches - User: {Username}", CurrentUser?.UserName);

            if (UserId == null)
            {
                _logger.LogWarning("User not authenticated");
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // Tính toán và lưu matches một chiều (chỉ tính preference A vs profile B)
                var result = await _matchingService.FindOneWayMatchesAsync(UserId.Value);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to find one-way matches: {Message}", result.Message);
                    TempData["ErrorMessage"] = result.Message;
                    return View(new List<MatchResult>());
                }

                _logger.LogInformation("Found {Count} one-way matches for user {UserId}", 
                    result.Data?.Count ?? 0, UserId.Value);

                return View(result.Data ?? new List<MatchResult>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding one-way matches for user {UserId}", UserId.Value);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tìm kiếm người phù hợp. Vui lòng thử lại sau.";
                return View(new List<MatchResult>());
            }
        }

        // GET: Matching/MutualMatches - Tìm người phù hợp hai chiều
        public async Task<IActionResult> MutualMatches()
        {
            _logger.LogInformation("GET MutualMatches - User: {Username}", CurrentUser?.UserName);

            if (UserId == null)
            {
                _logger.LogWarning("User not authenticated");
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // Tìm matches của user hiện tại (những người tôi thích)
                var myMatchesResult = await _matchingService.FindBestMatchesAsync(UserId.Value);

                if (!myMatchesResult.Success || myMatchesResult.Data == null || !myMatchesResult.Data.Any())
                {
                    _logger.LogWarning("No matches found: {Message}", myMatchesResult.Message);
                    TempData["ErrorMessage"] = myMatchesResult.Message ?? "Chưa có người phù hợp. Hãy hoàn thiện hồ sơ để tìm kiếm!";
                    return View(new List<MatchResult>());
                }

                // Lấy danh sách ID của những người tôi thích
                var myMatchedUserIds = myMatchesResult.Data.Select(m => m.MatchedUserId).ToList();

                // Lấy tất cả matches của những người đó một lần duy nhất (tối ưu query)
                var allTheirMatches = new Dictionary<Guid, List<Guid>>();
                
                foreach (var matchedUserId in myMatchedUserIds)
                {
                    var theirMatches = await _matchResultService.GetMatchesByUserIdAsync(matchedUserId);
                    
                    if (theirMatches.Success && theirMatches.Data != null)
                    {
                        // Lưu danh sách những người họ thích
                        var theirMatchedUserIds = theirMatches.Data
                            .Where(m => m.UserId == matchedUserId) // Chỉ lấy matches của họ (không phải matches với họ)
                            .Select(m => m.MatchedUserId)
                            .ToList();
                        
                        allTheirMatches[matchedUserId] = theirMatchedUserIds;
                    }
                }

                // Tìm mutual matches: những người tôi thích và họ cũng thích tôi
                var mutualMatches = myMatchesResult.Data
                    .Where(match => 
                        allTheirMatches.ContainsKey(match.MatchedUserId) && 
                        allTheirMatches[match.MatchedUserId].Contains(UserId.Value))
                    .OrderByDescending(m => m.MatchScore)
                    .ToList();

                _logger.LogInformation("Found {Count} mutual matches out of {Total} matches for user {UserId}", 
                    mutualMatches.Count, myMatchesResult.Data.Count, UserId.Value);

                if (!mutualMatches.Any())
                {
                    TempData["ErrorMessage"] = "Chưa có kết nối hai chiều. Hãy chủ động nhắn tin với những người phù hợp!";
                }

                return View(mutualMatches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding mutual matches for user {UserId}", UserId.Value);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tìm kiếm người phù hợp hai chiều. Vui lòng thử lại sau.";
                return View(new List<MatchResult>());
            }
        }

        // POST: Matching/DeleteMatch - Xóa một match
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMatch(Guid id)
        {
            _logger.LogInformation("POST DeleteMatch - MatchId: {MatchId}, User: {Username}", 
                id, CurrentUser?.UserName);

            try
            {
                var result = await _matchResultService.DeleteMatchAsync(id);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Đã xóa người này khỏi danh sách!";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting match {MatchId}", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa. Vui lòng thử lại sau.";
            }

            return RedirectToAction(nameof(FindMatches));
        }

        // POST: Matching/RefreshMatches - API endpoint để refresh matches
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshMatches()
        {
            _logger.LogInformation("POST RefreshMatches - User: {Username}", CurrentUser?.UserName);

            if (UserId == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            try
            {
                // Sử dụng one-way matching (preference A vs profile B)
                var result = await _matchingService.FindOneWayMatchesAsync(UserId.Value);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Đã cập nhật danh sách! Tìm thấy {result.Data?.Count ?? 0} người phù hợp.";
                    return Json(new 
                    { 
                        success = true, 
                        message = $"Tìm thấy {result.Data?.Count ?? 0} người phù hợp",
                        count = result.Data?.Count ?? 0
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing matches for user {UserId}", UserId.Value);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi cập nhật." });
            }
        }

        // GET: Matching/GetMatchCount - API endpoint để lấy số lượng matches
        [HttpGet]
        public async Task<IActionResult> GetMatchCount()
        {
            _logger.LogInformation("GET GetMatchCount - User: {Username}", CurrentUser?.UserName);

            if (UserId == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            try
            {
                var result = await _matchResultService.GetMatchesByUserIdAsync(UserId.Value);

                if (result.Success)
                {
                    return Json(new 
                    { 
                        success = true, 
                        count = result.Data?.Count ?? 0,
                        topScore = result.Data?.FirstOrDefault()?.MatchScore ?? 0
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message, count = 0 });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting match count for user {UserId}", UserId.Value);
                return Json(new { success = false, message = "Đã xảy ra lỗi.", count = 0 });
            }
        }
    }
}

