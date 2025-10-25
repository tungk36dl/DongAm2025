using WebFindLove.Models.Repositories.MatchResultRepo;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Services.MatchResultService
{
    public class MatchResultService : IMatchResultService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMatchResultRepository _repository;
        private readonly ILogger<MatchResultService> _logger;

        public MatchResultService(
            IUnitOfWork unitOfWork,
            IMatchResultRepository repository,
            ILogger<MatchResultService> logger)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _logger = logger;
        }

        public async Task<DataResponse<List<MatchResult>>> GetMatchesByUserIdAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Getting matches for user: {UserId}", userId);
                var matches = await _repository.GetMatchesByUserIdAsync(userId);

                return new DataResponse<List<MatchResult>> { Success = true, Data = matches, Message = $"Retrieved {matches.Count} match(es)" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting matches for user: {UserId}", userId);
                return new DataResponse<List<MatchResult>> { Success = false, Message = "Failed to get matches", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<List<MatchResult>>> GetTopMatchesAsync(Guid userId, int count = 10)
        {
            try
            {
                _logger.LogInformation("Getting top {Count} matches for user: {UserId}", count, userId);
                var matches = await _repository.GetTopMatchesAsync(userId, count);

                return new DataResponse<List<MatchResult>> { Success = true, Data = matches, Message = $"Retrieved {matches.Count} match(es)" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top matches for user: {UserId}", userId);
                return new DataResponse<List<MatchResult>> { Success = false, Message = "Failed to get matches", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<MatchResult>> GetMatchBetweenUsersAsync(Guid userId1, Guid userId2)
        {
            try
            {
                _logger.LogInformation("Getting match between users: {UserId1} and {UserId2}", userId1, userId2);
                var match = await _repository.GetMatchBetweenUsersAsync(userId1, userId2);

                if (match == null)
                {
                    return new DataResponse<MatchResult> { Success = false, Message = "Match not found" };
                }

                return new DataResponse<MatchResult> { Success = true, Data = match };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting match between users: {UserId1} and {UserId2}", userId1, userId2);
                return new DataResponse<MatchResult> { Success = false, Message = "Failed to get match", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<MatchResult>> CreateMatchAsync(Guid userId1, Guid userId2, double? score = null, string? reasoning = null)
        {
            try
            {
                _logger.LogInformation("Creating match between users: {UserId1} and {UserId2}", userId1, userId2);

                if (userId1 == userId2)
                {
                    return new DataResponse<MatchResult> { Success = false, Message = "Cannot match user with themselves" };
                }

                // Check if match already exists
                var existing = await _repository.GetMatchBetweenUsersAsync(userId1, userId2);
                if (existing != null)
                {
                    return new DataResponse<MatchResult> { Success = false, Message = "Match already exists" };
                }

                var match = new MatchResult
                {
                    UserId = userId1,
                    MatchedUserId = userId2,
                    MatchScore = score ?? 0,
                    AiReasoning = reasoning,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _repository.Add(match);
                await _unitOfWork.SaveChangesAsync();

                return new DataResponse<MatchResult> { Success = true, Data = match, Message = "Match created successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating match between users: {UserId1} and {UserId2}", userId1, userId2);
                return new DataResponse<MatchResult> { Success = false, Message = "Failed to create match", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<bool>> DeleteMatchAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting match: {Id}", id);
                
                var match = await _repository.FindByIdAsync(id);
                if (match == null)
                {
                    return new DataResponse<bool> { Success = false, Message = "Match not found" };
                }

                // Soft delete
                match.IsActive = false;
                match.UpdatedAt = DateTime.UtcNow;
                _repository.Update(match);
                await _unitOfWork.SaveChangesAsync();

                return new DataResponse<bool> { Success = true, Data = true, Message = "Match deleted successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting match: {Id}", id);
                return new DataResponse<bool> { Success = false, Message = "Failed to delete match", ErrorDetails = ex.Message };
            }
        }
    }
}

