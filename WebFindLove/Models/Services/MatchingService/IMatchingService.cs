using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebFindLove.Models.Services.MatchingService
{
    /// <summary>
    /// Service for computing user matching based on embeddings
    /// </summary>
    public interface IMatchingService
    {
        /// <summary>
        /// Lấy danh sách ứng viên phù hợp dựa trên UserPreference
        /// </summary>
        /// <param name="userId">ID của user đang tìm kiếm</param>
        /// <param name="preference">Sở thích tìm kiếm của user</param>
        /// <param name="maxCandidates">Số lượng ứng viên tối đa (mặc định 20)</param>
        /// <returns>Danh sách user ứng viên</returns>
        Task<List<User>> GetCandidateUsersAsync(Guid userId, UserPreference preference, int maxCandidates = 20);

        /// <summary>
        /// Tính độ tương đồng cosine giữa 2 embedding vectors
        /// </summary>
        /// <param name="vector1">Vector 1</param>
        /// <param name="vector2">Vector 2</param>
        /// <returns>Độ tương đồng từ -1 đến 1 (1 là giống nhất)</returns>
        double ComputeCosineSimilarity(float[] vector1, float[] vector2);

        /// <summary>
        /// Tìm và lưu các match một chiều cho user (chỉ tính preference của A vs profile của B)
        /// </summary>
        /// <param name="userId">ID của user cần tìm match</param>
        /// <returns>DataResponse với danh sách MatchResult</returns>
        Task<DataResponse<List<MatchResult>>> FindOneWayMatchesAsync(Guid userId);

        /// <summary>
        /// Tìm và lưu các match tốt nhất cho user (tính cả 2 chiều)
        /// </summary>
        /// <param name="userId">ID của user cần tìm match</param>
        /// <returns>DataResponse với danh sách MatchResult</returns>
        Task<DataResponse<List<MatchResult>>> FindBestMatchesAsync(Guid userId);

        /// <summary>
        /// Parse embedding string (JSON) thành float array
        /// </summary>
        /// <param name="embeddingJson">JSON string chứa embedding vector</param>
        /// <returns>Float array hoặc null nếu parse thất bại</returns>
        float[]? ParseEmbedding(string? embeddingJson);
    }
}

