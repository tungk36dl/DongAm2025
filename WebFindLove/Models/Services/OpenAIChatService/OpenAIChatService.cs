using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebFindLove.Models.Options;

namespace WebFindLove.Models.Services.OpenAIChatService
{
    public class OpenAIChatService : IOpenAIChatService
    {
        private readonly OpenAIOptions _options;
        private readonly ILogger<OpenAIChatService> _logger;

        public OpenAIChatService(IOptions<OpenAIOptions> options, ILogger<OpenAIChatService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> NormalizeTextAsync(string input)
        {
            // Placeholder until chat implementation wired; keep signature and options usage ready
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Here you would use _options.ApiKey and _options.ChatModel with your OpenAI client
            await Task.CompletedTask;
            return input.Trim();
        }
    }
}
