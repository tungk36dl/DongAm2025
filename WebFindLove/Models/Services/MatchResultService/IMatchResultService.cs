namespace WebFindLove.Models.Services.MatchResultService
{
    public interface IMatchResultService
    {
        Task<DataResponse<List<MatchResult>>> GetMatchesByUserIdAsync(Guid userId);
        Task<DataResponse<List<MatchResult>>> GetTopMatchesAsync(Guid userId, int count = 10);
        Task<DataResponse<MatchResult>> GetMatchBetweenUsersAsync(Guid userId1, Guid userId2);
        Task<DataResponse<MatchResult>> CreateMatchAsync(Guid userId1, Guid userId2, double? score = null, string? reasoning = null);
        Task<DataResponse<bool>> DeleteMatchAsync(Guid id);
    }
}

