using System;
using System.Threading.Tasks;

namespace WebFindLove.Models.Services.EmbeddingService
{
    /// <summary>
    /// Service for generating text embeddings using OpenAI API
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Sinh đoạn mô tả ngắn bằng tiếng Việt về người dùng từ thông tin profile
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>Đoạn text mô tả profile</returns>
        string GenerateProfileText(User user);

        /// <summary>
        /// Sinh đoạn mô tả gu người yêu mong muốn bằng tiếng Việt
        /// </summary>
        /// <param name="preference">UserPreference entity</param>
        /// <returns>Đoạn text mô tả preference</returns>
        string GeneratePreferenceText(UserPreference preference);

        /// <summary>
        /// Gọi OpenAI API để sinh vector embedding từ text
        /// </summary>
        /// <param name="text">Text cần embedding</param>
        /// <returns>Vector embedding dạng float[]</returns>
        Task<float[]?> GetEmbeddingAsync(string text);

        /// <summary>
        /// Sinh profile text + embedding và lưu vào User entity
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>DataResponse indicating success or failure</returns>
        Task<DataResponse<User>> SaveProfileEmbeddingAsync(User user);

        /// <summary>
        /// Sinh preference text + embedding và lưu vào UserPreference entity
        /// </summary>
        /// <param name="preference">UserPreference entity</param>
        /// <returns>DataResponse indicating success or failure</returns>
        Task<DataResponse<UserPreference>> SavePreferenceEmbeddingAsync(UserPreference preference);
    }
}

