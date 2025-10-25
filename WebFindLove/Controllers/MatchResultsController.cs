using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Models;
using WebFindLove.Models.Services.MatchResultService;

namespace WebFindLove.Controllers
{
    [Authorize]
    public class MatchResultsController : BaseController
    {
        private readonly IMatchResultService _matchResultService;
        private readonly ILogger<MatchResultsController> _logger;

        public MatchResultsController(
            IMatchResultService matchResultService,
            ILogger<MatchResultsController> logger)
        {
            _matchResultService = matchResultService;
            _logger = logger;
            Logger = logger;
            _logger.LogInformation("MatchResultsController initialized");
        }

        // GET: MatchResults - My matches
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("GET My Matches - User: {Username}", CurrentUser?.UserName);

            var response = await _matchResultService.GetMatchesByUserIdAsync(UserId!.Value);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to get matches: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }

            return View(response.Data ?? new List<MatchResult>());
        }

        // GET: MatchResults/TopMatches
        public async Task<IActionResult> TopMatches(int count = 10)
        {
            _logger.LogInformation("GET Top Matches - User: {Username}, Count: {Count}",
                CurrentUser?.UserName, count);

            var response = await _matchResultService.GetTopMatchesAsync(UserId!.Value, count);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to get top matches: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }

            ViewData["Count"] = count;
            return View(response.Data ?? new List<MatchResult>());
        }

        // POST: MatchResults/Create (Admin only or from matching algorithm)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Guid userId1, Guid userId2, double? score, string? reasoning)
        {
            _logger.LogInformation("POST Create Match - User1: {User1}, User2: {User2}, Score: {Score}",
                userId1, userId2, score);

            var response = await _matchResultService.CreateMatchAsync(userId1, userId2, score, reasoning);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to create match: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }
            else
            {
                _logger.LogInformation("Match created successfully: {MatchId}", response.Data?.Id);
                TempData["SuccessMessage"] = "Match created successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: MatchResults/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("POST Delete Match - MatchId: {MatchId}, User: {Username}",
                id, CurrentUser?.UserName);

            // Verify ownership (user should only be able to delete their own matches)
            var allMatches = await _matchResultService.GetMatchesByUserIdAsync(UserId!.Value);
            if (allMatches.Success && allMatches.Data != null)
            {
                var match = allMatches.Data.FirstOrDefault(m => m.Id == id);
                if (match == null && UserRole != "Admin")
                {
                    _logger.LogWarning("Unauthorized delete attempt on match: {MatchId} by user: {Username}",
                        id, CurrentUser?.UserName);
                    TempData["ErrorMessage"] = "You don't have permission to delete this match.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var response = await _matchResultService.DeleteMatchAsync(id);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to delete match: {Message}", response.Message);
                TempData["ErrorMessage"] = response.Message;
            }
            else
            {
                _logger.LogInformation("Match deleted successfully: {MatchId}", id);
                TempData["SuccessMessage"] = "Match removed successfully!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

