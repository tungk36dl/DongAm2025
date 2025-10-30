using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using WebFindLove.Models.Repositories.UserRepo;
using WebFindLove.Models.Repositories.UserPreferenceRepo;
using WebFindLove.Models.UnitOfWork;
using WebFindLove.Models.Services.OpenAIChatService;
using Microsoft.Extensions.Options;
using WebFindLove.Models.Options;

namespace WebFindLove.Models.Services.EmbeddingService
{
    /// <summary>
    /// Service implementation for generating text embeddings using OpenAI API
    /// </summary>
    public class EmbeddingService : IEmbeddingService
    {
        private readonly OpenAIOptions _openAIOptions;
        private readonly ILogger<EmbeddingService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IUserPreferenceRepository _preferenceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly EmbeddingClient _embeddingClient;
        private readonly IOpenAIChatService _chatService;
        private readonly string _embeddingModel;

        public EmbeddingService(
            IOptions<OpenAIOptions> openAIOptions,
            ILogger<EmbeddingService> logger,
            IUserRepository userRepository,
            IUserPreferenceRepository preferenceRepository,
            IUnitOfWork unitOfWork,
            IOpenAIChatService chatService)
        {
            _openAIOptions = openAIOptions.Value;
            _logger = logger;
            _userRepository = userRepository;
            _preferenceRepository = preferenceRepository;
            _unitOfWork = unitOfWork;
            _chatService = chatService;

            // Get OpenAI configuration
            var apiKey = _openAIOptions.ApiKey;
            _embeddingModel = string.IsNullOrWhiteSpace(_openAIOptions.EmbeddingModel)
                ? "text-embedding-3-small"
                : _openAIOptions.EmbeddingModel;



            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_OPENAI_API_KEY_HERE")
            {
                _logger.LogWarning("OpenAI API key not configured. Embedding service will not work properly.");
            }

            // Initialize OpenAI Embedding Client
            _embeddingClient = new EmbeddingClient(_embeddingModel, apiKey);
        }

        /// <summary>
        /// Sinh đoạn mô tả ngắn bằng tiếng Việt về người dùng từ thông tin profile
        /// </summary>
        public string GenerateProfileText(User user)
        {
            try
            {
                var parts = new List<string>();

                // Tên và tuổi
                if (!string.IsNullOrWhiteSpace(user.FullName))
                {
                    parts.Add($"Tên: {user.FullName}");
                }

                if (user.DateOfBirth.HasValue)
                {
                    var age = DateTime.UtcNow.Year - user.DateOfBirth.Value.Year;
                    parts.Add($"{age} tuổi");
                }

                // Giới tính
                if (!string.IsNullOrWhiteSpace(user.Gender))
                {
                    var genderText = user.Gender.ToLower() switch
                    {
                        "male" => "Nam",
                        "female" => "Nữ",
                        _ => user.Gender
                    };
                    parts.Add($"Giới tính: {genderText}");
                }

                // Chiều cao
                if (user.Height.HasValue)
                {
                    parts.Add($"Chiều cao: {user.Height}cm");
                }

                // Nghề nghiệp
                if (!string.IsNullOrWhiteSpace(user.Occupation))
                {
                    parts.Add($"Nghề nghiệp: {user.Occupation}");
                }

                // Quê quán
                if (!string.IsNullOrWhiteSpace(user.Hometown))
                {
                    parts.Add($"Quê quán: {user.Hometown}");
                }

                // Địa chỉ hiện tại
                if (!string.IsNullOrWhiteSpace(user.Location))
                {
                    parts.Add($"Đang sống tại: {user.Location}");
                }

                // Tính cách
                if (!string.IsNullOrWhiteSpace(user.PersonalityType))
                {
                    parts.Add($"Nhóm tính cách: {user.PersonalityType}");
                }

                if (!string.IsNullOrWhiteSpace(user.PersonalityText))
                {
                    parts.Add($"Tính cách: {user.PersonalityText}");
                }

                // Tiểu sử
                if (!string.IsNullOrWhiteSpace(user.Bio))
                {
                    parts.Add($"Giới thiệu: {user.Bio}");
                }

                // Sở thích
                if (!string.IsNullOrWhiteSpace(user.Interests))
                {
                    parts.Add($"Sở thích: {user.Interests}");
                }

                var profileText = string.Join(". ", parts);
                _logger.LogDebug("Generated profile text for user {UserId}: {Text}", user.Id, profileText);

                return profileText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating profile text for user {UserId}", user.Id);
                return string.Empty;
            }
        }

        /// <summary>
        /// Sinh đoạn mô tả gu người yêu mong muốn bằng tiếng Việt
        /// </summary>
        public string GeneratePreferenceText(UserPreference preference)
        {
            try
            {
                var parts = new List<string>();

                // Giới tính mong muốn
                if (!string.IsNullOrWhiteSpace(preference.PreferredGender))
                {
                    var genderText = preference.PreferredGender.ToLower() switch
                    {
                        "male" => "Nam",
                        "female" => "Nữ",
                        "all" => "Không phân biệt",
                        _ => preference.PreferredGender
                    };
                    parts.Add($"Tìm kiếm: {genderText}");
                }

                // Độ tuổi
                if (preference.AgeMin.HasValue || preference.AgeMax.HasValue)
                {
                    var ageRange = $"Độ tuổi: {preference.AgeMin ?? 18}-{preference.AgeMax ?? 100} tuổi";
                    parts.Add(ageRange);
                }

                // Chiều cao
                if (preference.MinHeight.HasValue || preference.MaxHeight.HasValue)
                {
                    var heightRange = $"Chiều cao: {preference.MinHeight ?? 100}-{preference.MaxHeight ?? 250}cm";
                    parts.Add(heightRange);
                }

                // Khu vực
                if (!string.IsNullOrWhiteSpace(preference.LocationPreference))
                {
                    parts.Add($"Khu vực: {preference.LocationPreference}");
                }

                // Tính cách mong muốn
                if (!string.IsNullOrWhiteSpace(preference.PersonalityPreference))
                {
                    parts.Add($"Tính cách mong muốn: {preference.PersonalityPreference}");
                }

                // Sở thích mong muốn
                if (!string.IsNullOrWhiteSpace(preference.InterestPreference))
                {
                    parts.Add($"Sở thích chung: {preference.InterestPreference}");
                }

                var preferenceText = string.Join(". ", parts);
                _logger.LogDebug("Generated preference text for user {UserId}: {Text}", preference.UserId, preferenceText);

                return preferenceText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preference text for user {UserId}", preference.UserId);
                return string.Empty;
            }
        }

        /// <summary>
        /// Gọi OpenAI API để sinh vector embedding từ text
        /// </summary>
        public async Task<float[]?> GetEmbeddingAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Cannot generate embedding for empty text");
                    return null;
                }

                _logger.LogDebug("Generating embedding for text: {Text}", text.Substring(0, Math.Min(100, text.Length)));

                // Call OpenAI API
                var embeddingResult = await _embeddingClient.GenerateEmbeddingAsync(text);
                
                if (embeddingResult?.Value != null)
                {
                    var embedding = embeddingResult.Value.ToFloats().ToArray();
                    _logger.LogInformation("Generated embedding with dimension: {Dimension}", embedding.Length);
                    return embedding;
                }

                _logger.LogWarning("Failed to generate embedding: null result");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API for embedding");
                return null;
            }
        }

        /// <summary>
        /// Sinh profile text + embedding và lưu vào User entity
        /// </summary>
        public async Task<DataResponse<User>> SaveProfileEmbeddingAsync(User user)
        {
            try
            {
                if (user == null)
                {
                    return new DataResponse<User> 
                    { 
                        Success = false, 
                        Message = "User is null" 
                    };
                }

                _logger.LogInformation("Generating profile embedding for user {UserId}", user.Id);

                // Generate profile text
                var profileText = GenerateProfileText(user);
                if (string.IsNullOrWhiteSpace(profileText))
                {
                    _logger.LogWarning("Profile text is empty for user {UserId}", user.Id);
                    return new DataResponse<User> 
                    { 
                        Success = false, 
                        Message = "Cannot generate profile text" 
                    };
                }
                var profileTextNormalized = await _chatService.NormalizeTextAsync(profileText);
                if (string.IsNullOrWhiteSpace(profileTextNormalized))
                {
                    _logger.LogWarning("Profile text is empty for user {UserId}", user.Id);
                    return new DataResponse<User>
                    {
                        Success = false,
                        Message = "Cannot generate profile text"
                    };
                }

                user.ProfileText = profileTextNormalized;

                // Generate embedding
                var embedding = await GetEmbeddingAsync(profileTextNormalized);
                if (embedding == null || embedding.Length == 0)
                {
                    _logger.LogWarning("Failed to generate embedding for user {UserId}", user.Id);
                    return new DataResponse<User> 
                    { 
                        Success = false, 
                        Message = "Cannot generate embedding" 
                    };
                }

                // Save embedding as JSON string
                user.ProfileEmbedding = JsonSerializer.Serialize(embedding);

                // Update user in database
                user.UpdatedAt = DateTime.UtcNow;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully saved profile embedding for user {UserId}", user.Id);
                return new DataResponse<User> 
                { 
                    Success = true, 
                    Data = user,
                    Message = "Profile embedding saved successfully" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile embedding for user {UserId}", user?.Id);
                return new DataResponse<User> 
                { 
                    Success = false, 
                    Message = "Error saving profile embedding",
                    ErrorDetails = ex.Message 
                };
            }
        }

        /// <summary>
        /// Sinh preference text + embedding và lưu vào UserPreference entity
        /// </summary>
        public async Task<DataResponse<UserPreference>> SavePreferenceEmbeddingAsync(UserPreference preference)
        {
            try
            {
                if (preference == null)
                {
                    return new DataResponse<UserPreference> 
                    { 
                        Success = false, 
                        Message = "Preference is null" 
                    };
                }

                _logger.LogInformation("Generating preference embedding for user {UserId}", preference.UserId);

                // Generate preference text
                var preferenceText = GeneratePreferenceText(preference);
                if (string.IsNullOrWhiteSpace(preferenceText))
                {
                    _logger.LogWarning("Preference text is empty for user {UserId}", preference.UserId);
                    return new DataResponse<UserPreference> 
                    { 
                        Success = false, 
                        Message = "Cannot generate preference text" 
                    };
                }
                var preferenceTextNormalized = await _chatService.NormalizeTextAsync(preferenceText);
                if(string.IsNullOrWhiteSpace(preferenceTextNormalized))
                {
                    _logger.LogWarning("Preference text is empty for user {UserId}", preference.UserId);
                    return new DataResponse<UserPreference>
                    {
                        Success = false,
                        Message = "Cannot generate preference text"
                    };
                }



                preference.PreferenceText = preferenceTextNormalized;

                // Generate embedding
                var embedding = await GetEmbeddingAsync(preferenceTextNormalized);
                if (embedding == null || embedding.Length == 0)
                {
                    _logger.LogWarning("Failed to generate embedding for user preference {UserId}", preference.UserId);
                    return new DataResponse<UserPreference> 
                    { 
                        Success = false, 
                        Message = "Cannot generate embedding" 
                    };
                }

                // Save embedding as JSON string
                preference.PreferenceEmbedding = JsonSerializer.Serialize(embedding);
               
                // Update preference in database
                preference.UpdatedAt = DateTime.UtcNow;
                _preferenceRepository.Update(preference);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully saved preference embedding for user {UserId}", preference.UserId);
                return new DataResponse<UserPreference> 
                { 
                    Success = true, 
                    Data = preference,
                    Message = "Preference embedding saved successfully" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving preference embedding for user {UserId}", preference?.UserId);
                return new DataResponse<UserPreference> 
                { 
                    Success = false, 
                    Message = "Error saving preference embedding",
                    ErrorDetails = ex.Message 
                };
            }
        }



    }
}

