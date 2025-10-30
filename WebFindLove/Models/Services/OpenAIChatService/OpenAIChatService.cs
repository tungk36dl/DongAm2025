using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using WebFindLove.Models.Options;

namespace WebFindLove.Models.Services.OpenAIChatService
{
    public class OpenAIChatService : IOpenAIChatService
    {
        private readonly OpenAIOptions _openAIOptions;
        private readonly ILogger<OpenAIChatService> _logger;
        private readonly string _chatAIModel;
        private readonly ChatClient _chatClient;


        public OpenAIChatService(IOptions<OpenAIOptions> options, ILogger<OpenAIChatService> logger)
        {
            _openAIOptions = options.Value;
            _logger = logger;

            // Get OpenAI configuration
            var apiKey = _openAIOptions.ApiKey;
            _chatAIModel = string.IsNullOrWhiteSpace(_openAIOptions.ChatModel)
                ? "gpt-4o-mini"
                : _openAIOptions.ChatModel;
            var client = new OpenAIClient(apiKey);
            _chatClient = client.GetChatClient(_chatAIModel);
        }

        public async Task<string> NormalizeTextAsync(string input)
        {
            // Placeholder until chat implementation wired; keep signature and options usage ready
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Here you would use _options.ApiKey and _options.ChatModel with your OpenAI client
            try
            {
                var prompt = $"""
Bạn là chuyên gia xử lý ngôn ngữ tự nhiên. Hãy chuẩn hóa đoạn mô tả tính cách hoặc gu người yêu mà người dùng nhập, 
để chuẩn bị cho bước tạo embedding (biểu diễn vector).

Yêu cầu:
- Viết lại nội dung theo văn phong tiếng Việt tự nhiên, đầy đủ và mạch lạc.
- Giữ nguyên toàn bộ ý nghĩa và bản chất (kể cả các đặc điểm tiêu cực hoặc phủ định như "không hút thuốc", "ghét nói tục").
- Chuyển các gạch đầu dòng hoặc danh sách rời rạc thành câu hoàn chỉnh.
- Mỗi đặc điểm nên được mô tả rõ ràng bằng câu khẳng định (có thể bao gồm phủ định, nhưng không mất nghĩa).
- Không thêm, không sửa đổi, không làm đẹp quá mức nội dung.
- Mục tiêu là tạo đoạn văn mô tả chính xác để phục vụ so sánh vector bằng cosine similarity.

Nội dung gốc:
{input}

Kết quả mong muốn (chỉ trả về đoạn mô tả cuối cùng, không giải thích):
""";

                // ✅ Cách mới đúng: dùng SystemChatMessage và UserChatMessage
                var response = await _chatClient.CompleteChatAsync(new ChatMessage[]
                    {
                        new SystemChatMessage("Bạn là chuyên gia ngôn ngữ tiếng Việt."),
                        new UserChatMessage(prompt)
                    });

                var normalized = response.Value.Content[0].Text;
                _logger.LogInformation("Normalized text: {Text}", normalized);

                return normalized?.Trim() ?? input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error normalizing text via OpenAI");
                return input; // fallback nếu lỗi
            }
        }
    }
}
