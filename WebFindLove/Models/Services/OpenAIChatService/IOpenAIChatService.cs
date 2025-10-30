namespace WebFindLove.Models.Services.OpenAIChatService
{
    public interface IOpenAIChatService
    {

   
            /// <summary>
            /// Gửi text lên OpenAI để chuyển về dạng khẳng định, tích cực, chuẩn hóa tiếng Việt
            /// </summary>
            Task<string> NormalizeTextAsync(string input);
       
    }
}
