using System;

namespace WebFindLove.Models.Options
{
    /// <summary>
    /// Strongly-typed options for the OpenAI configuration section in appsettings.json
    /// </summary>
    public class OpenAIOptions
    {
        public string? ApiKey { get; set; } = string.Empty;
        public string? EmbeddingModel { get; set; } = "text-embedding-3-small";
        public string? ChatModel { get; set; } = "gpt-4o-mini";
        public int? MaxRetries { get; set; } = 3;
        public int? TimeoutSeconds { get; set; } = 30;
    }
}


