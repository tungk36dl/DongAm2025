namespace WebFindLove.Models.Services.OpenAIChatService
{
    public interface IOpenAIChatService
    {

   
            /// <summary>
            /// Gửi text lên OpenAI để chuyển về dạng khẳng định, tích cực, chuẩn hóa tiếng Việt
            /// </summary>
            Task<string> NormalizeTextAsync(string input);
        
        /// <summary>
        /// Sinh lý do vì sao hai người phù hợp nhau dựa trên mô tả profile và gu của cả hai.
        /// </summary>
        Task<string> GenerateMatchReasonTwoAsync(string userAProfile, string userAPreference, string userBProfile, string userBPreference);
        Task<string> GenerateMatchReasonOneAsync( string userAPreference, string userBProfile);
       
    }
}
