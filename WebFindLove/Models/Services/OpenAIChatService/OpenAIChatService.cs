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

        public async Task<string> GenerateMatchReasonTwoAsync(string userAProfile, string userAPreference, string userBProfile, string userBPreference)
        {
            try
            {
                var prompt = $"""
Bạn là chuyên gia tư vấn hẹn hò. Hãy phân tích vì sao hai người dưới đây phù hợp với nhau, dựa trên mô tả cá nhân và gu người yêu của cả hai. Viết bằng tiếng Việt, tự nhiên, tích cực, 3-5 câu, tập trung vào điểm chung và bổ trợ.

— Người A —
Hồ sơ: {userAProfile}
Gu mong muốn: {userAPreference}

— Người B —
Hồ sơ: {userBProfile}
Gu mong muốn: {userBPreference}

Yêu cầu:
- Trả về CHỈ phần giải thích ngắn gọn (không tiêu đề, không đánh số).
- Không thêm thông tin không có trong dữ liệu.
""";

                var response = await _chatClient.CompleteChatAsync(new ChatMessage[]
                {
                    new SystemChatMessage("Bạn là chuyên gia tư vấn hẹn hò, viết súc tích, tiếng Việt tự nhiên."),
                    new UserChatMessage(prompt)
                });

                var text = response.Value.Content[0].Text?.Trim();
                return string.IsNullOrWhiteSpace(text) ? "" : text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating match reason via OpenAI");
                return string.Empty;
            }
        }

        public async Task<string> GenerateMatchReasonOneAsync(string userAPreference, string userBProfile)
        {
            try
            {
                var prompt = $"""
Bạn là chuyên gia tư vấn hẹn hò. Dựa trên GU mong muốn của Người A và HỒ SƠ của Người B, hãy giải thích ngắn gọn (2-4 câu, tiếng Việt tự nhiên) vì sao Người B phù hợp với điều Người A đang tìm kiếm. Tập trung vào điểm khớp nổi bật, không thêm thắt ngoài dữ liệu.

— Gu mong muốn của Người A —
{userAPreference}

— Hồ sơ của Người B —
{userBProfile}

Yêu cầu: Chỉ trả về đoạn giải thích, không tiêu đề, không đánh số.
""";

                var response = await _chatClient.CompleteChatAsync(new ChatMessage[]
                {
                    new SystemChatMessage("Bạn là chuyên gia tư vấn hẹn hò, viết súc tích, tiếng Việt tự nhiên."),
                    new UserChatMessage(prompt)
                });

                var text = response.Value.Content[0].Text?.Trim();
                return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating one-way match reason via OpenAI");
                return string.Empty;
            }
        }
    }
}
