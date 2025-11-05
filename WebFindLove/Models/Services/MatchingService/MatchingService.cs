using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.Repositories.UserRepo;
using WebFindLove.Models.Repositories.UserPreferenceRepo;
using WebFindLove.Models.Repositories.MatchResultRepo;
using WebFindLove.Models.UnitOfWork;
using WebFindLove.Models.Services.EmbeddingService;
using WebFindLove.Models.Services.OpenAIChatService;
using WebFindLove.Models.Options;
using Microsoft.Extensions.Options;

namespace WebFindLove.Models.Services.MatchingService
{
    /// <summary>
    /// Service implementation for computing user matching based on embeddings
    /// </summary>
    public class MatchingService : IMatchingService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserPreferenceRepository _preferenceRepository;
        private readonly IMatchResultRepository _matchResultRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmbeddingService _embeddingService;
        private readonly IOpenAIChatService _chatService;
        private readonly ILogger<MatchingService> _logger;
        private readonly MatchingOptions _matchingOptions;

        public MatchingService(
            IUserRepository userRepository,
            IUserPreferenceRepository preferenceRepository,
            IMatchResultRepository matchResultRepository,
            IUnitOfWork unitOfWork,
            ILogger<MatchingService> logger,
            IEmbeddingService embeddingService,
            IOpenAIChatService chatService,
            IOptions<MatchingOptions> matchingOptions)
        {
            _userRepository = userRepository;
            _preferenceRepository = preferenceRepository;
            _matchResultRepository = matchResultRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _embeddingService = embeddingService;
            _chatService = chatService;
            _matchingOptions = matchingOptions.Value;
        }

        /// <summary>
        /// Lấy danh sách ứng viên phù hợp dựa trên UserPreference
        /// </summary>
        public async Task<List<User>> GetCandidateUsersAsync(Guid userId, UserPreference preference, int maxCandidates = 20)
        {
            try
            {
                _logger.LogInformation("Getting candidate users for user {UserId}", userId);

                var query = _userRepository.FindAll(
                    u => u.Id != userId && u.IsActive,
                    u => u.Preference
                );

                // Filter by gender preference
                if (!string.IsNullOrEmpty(preference.PreferredGender) && 
                    preference.PreferredGender.ToLower() != "all")
                {
                    query = query.Where(u => u.Gender != null && 
                        u.Gender.ToLower() == preference.PreferredGender.ToLower());
                }

                // Filter by age range
                if (preference.AgeMin.HasValue || preference.AgeMax.HasValue)
                {
                    var currentYear = DateTime.UtcNow.Year;
                    
                    if (preference.AgeMin.HasValue)
                    {
                        var maxBirthYear = currentYear - preference.AgeMin.Value;
                        query = query.Where(u => u.DateOfBirth.HasValue && 
                            u.DateOfBirth.Value.Year <= maxBirthYear);
                    }

                    if (preference.AgeMax.HasValue)
                    {
                        var minBirthYear = currentYear - preference.AgeMax.Value;
                        query = query.Where(u => u.DateOfBirth.HasValue && 
                            u.DateOfBirth.Value.Year >= minBirthYear);
                    }
                }

                // Filter by height range (optional)
                if (preference.MinHeight.HasValue)
                {
                    query = query.Where(u => u.Height.HasValue && 
                        u.Height.Value >= preference.MinHeight.Value);
                }

                if (preference.MaxHeight.HasValue)
                {
                    query = query.Where(u => u.Height.HasValue && 
                        u.Height.Value <= preference.MaxHeight.Value);
                }

                // Only get users who have ProfileEmbedding
                query = query.Where(u => u.ProfileEmbedding != null && u.ProfileEmbedding != "");

                // Take max candidates
                var candidates = await query.Take(maxCandidates).ToListAsync();

                _logger.LogInformation("Found {Count} candidate users for user {UserId}", 
                    candidates.Count, userId);

                return candidates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting candidate users for user {UserId}", userId);
                return new List<User>();
            }
        }

        /// <summary>
        /// Tính độ tương đồng cosine giữa 2 embedding vectors
        /// </summary>
        public double ComputeCosineSimilarity(float[] vector1, float[] vector2)
        {
            try
            {
                if (vector1 == null || vector2 == null)
                {
                    _logger.LogWarning("Cannot compute cosine similarity: null vector");
                    return 0.0;
                }

                if (vector1.Length != vector2.Length)
                {
                    _logger.LogWarning("Cannot compute cosine similarity: vector length mismatch ({L1} vs {L2})", 
                        vector1.Length, vector2.Length);
                    return 0.0;
                }

                if (vector1.Length == 0)
                {
                    _logger.LogWarning("Cannot compute cosine similarity: empty vectors");
                    return 0.0;
                }

                // Compute dot product
                double dotProduct = 0.0;
                for (int i = 0; i < vector1.Length; i++)
                {
                    dotProduct += vector1[i] * vector2[i];
                }

                // Compute magnitudes
                double magnitude1 = 0.0;
                double magnitude2 = 0.0;
                for (int i = 0; i < vector1.Length; i++)
                {
                    magnitude1 += vector1[i] * vector1[i];
                    magnitude2 += vector2[i] * vector2[i];
                }
                magnitude1 = Math.Sqrt(magnitude1);
                magnitude2 = Math.Sqrt(magnitude2);

                if (magnitude1 == 0.0 || magnitude2 == 0.0)
                {
                    _logger.LogWarning("Cannot compute cosine similarity: zero magnitude vector");
                    return 0.0;
                }

                // Compute cosine similarity
                double cosineSimilarity = dotProduct / (magnitude1 * magnitude2);

                return cosineSimilarity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing cosine similarity");
                return 0.0;
            }
        }

        /// <summary>
        /// Parse embedding string (JSON) thành float array
        /// </summary>
        public float[]? ParseEmbedding(string? embeddingJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(embeddingJson))
                {
                    return null;
                }

                var embedding = JsonSerializer.Deserialize<float[]>(embeddingJson);
                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing embedding JSON");
                return null;
            }
        }

        /// <summary>
        /// Tìm và lưu các match một chiều cho user (chỉ tính preference của A vs profile của B)
        /// </summary>
        public async Task<DataResponse<List<MatchResult>>> FindOneWayMatchesAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Finding one-way matches for user {UserId}", userId);

                // Daily limit: max 2 one-way matches per day (UTC)
                var todayStart = DateTime.UtcNow.Date;
                var todayEnd = todayStart.AddDays(1);
                var oneWayCountToday = await _matchResultRepository.CountAsync(m =>
                    m.UserId == userId &&
                    m.MatchType == "OneWay" &&
                    m.CreatedAt >= todayStart && m.CreatedAt < todayEnd);

                var oneWayLimit = Math.Max(0, _matchingOptions?.OneWayDailyLimit ?? 2);
                if (oneWayCountToday >= oneWayLimit)
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Data = new List<MatchResult>(),
                        Message = $"Bạn đã sử dụng hết {oneWayLimit} lượt ghép một chiều hôm nay. Vui lòng thử lại vào ngày mai."
                    };
                }

                // Get user A and their preference
                var userA = await _userRepository.FindByIdAsync(userId, u => u.Preference);
                if (userA == null)
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var preferenceA = await _preferenceRepository.GetByUserIdAsync(userId);
                if (preferenceA == null)
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Message = "User preference not found. Please set your preferences first."
                    };
                }

                // Check if user A has embeddings
                if (string.IsNullOrEmpty(userA.ProfileEmbedding))
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Message = "User profile embedding not found. Please update your profile."
                    };
                }

                if (string.IsNullOrEmpty(preferenceA.PreferenceEmbedding))
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Message = "User preference embedding not found. Please update your preferences."
                    };
                }

                // Parse user A embeddings
                var preferenceEmbeddingA = ParseEmbedding(preferenceA.PreferenceEmbedding);

                if (preferenceEmbeddingA == null)
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Message = "Failed to parse preference embedding"
                    };
                }

                // Get candidate users
                var candidates = await GetCandidateUsersAsync(userId, preferenceA);
                if (candidates.Count == 0)
                {
                    _logger.LogInformation("No candidates found for user {UserId}", userId);
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = true,
                        Data = new List<MatchResult>(),
                        Message = "No matching candidates found"
                    };
                }

                _logger.LogInformation("Computing one-way match scores for {Count} candidates", candidates.Count);

                // Compute match scores for each candidate
                var matchResults = new List<MatchResult>();

                foreach (var candidateB in candidates)
                {
                    try
                    {
                        // Get candidate's profile embedding
                        if (string.IsNullOrEmpty(candidateB.ProfileEmbedding))
                        {
                            _logger.LogDebug("Skipping candidate {CandidateId}: missing profile embedding", candidateB.Id);
                            continue;
                        }

                        // Parse candidate embeddings
                        var profileEmbeddingB = ParseEmbedding(candidateB.ProfileEmbedding);

                        if (profileEmbeddingB == null)
                        {
                            _logger.LogDebug("Skipping candidate {CandidateId}: failed to parse profile embedding", candidateB.Id);
                            continue;
                        }

                        // Compute one-way similarity: What A wants vs What B is
                        var similarity = ComputeCosineSimilarity(preferenceEmbeddingA, profileEmbeddingB);

                        // Convert score from [-1, 1] to [0, 100]
                        var matchScore = (similarity + 1.0) / 2.0 * 100.0;

                        _logger.LogDebug("One-way match score for user {UserId} and {CandidateId}: {Score:F2} (similarity={Similarity:F3})",
                            userId, candidateB.Id, matchScore, similarity);

                        // Create match result
                        var matchResult = new MatchResult
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            MatchedUserId = candidateB.Id,
                            MatchScore = Math.Round(matchScore, 2),
                            AiReasoning = $"Độ phù hợp được tính dựa trên sở thích tìm kiếm của bạn và thông tin cá nhân của {candidateB.FullName ?? "người này"}. " +
                                        $"Điểm tương thích: {similarity:F3}. " +
                                        $"Người này phù hợp {matchScore:F1}% với những gì bạn đang tìm kiếm.",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        matchResults.Add(matchResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error computing one-way match for candidate {CandidateId}", candidateB.Id);
                    }
                }

                if (matchResults.Count == 0)
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = true,
                        Data = new List<MatchResult>(),
                        Message = "No valid matches found"
                    };
                }

                // Sort by match score descending and pick index based on how many one-way matches already made today (0-based)
                var orderedOneWay = matchResults.OrderByDescending(m => m.MatchScore).ToList();
                var pickIndex = Math.Max(0, Math.Min(oneWayCountToday, orderedOneWay.Count - 1));
                var topMatch = orderedOneWay.ElementAt(pickIndex);

                // Tag match type
                topMatch.MatchType = "OneWay";

                // Enrich top match with AI reasoning (one-way): A's preference vs B's profile
                var matchedUser = await _userRepository.FindByIdAsync(topMatch.MatchedUserId, u => u.Preference);
                if (matchedUser != null)
                {
                    var userAPrefText = preferenceA.PreferenceText;
                    var userBProfileText = matchedUser.ProfileText;

                    var reason = await _chatService.GenerateMatchReasonOneAsync(
                        userAPrefText,
                        userBProfileText
                    );

                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        topMatch.AiReasoning = reason;
                    }
                }

                // Mark old matches inactive (keep history for daily limit)
                var toDeactivateOneWay = await _matchResultRepository.GetMatchesByUserIdAsync(userId);
                foreach (var oldMatch in toDeactivateOneWay)
                {
                    oldMatch.IsActive = false;
                    oldMatch.UpdatedAt = DateTime.UtcNow;
                    _matchResultRepository.Update(oldMatch);
                }

                // Attach navigation and housekeeping

                topMatch.LastCalculatedAt = DateTime.UtcNow;

                // Save only the best match
                _matchResultRepository.Add(topMatch);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully saved the top one-way match for user {UserId}", userId);
                topMatch.User = userA;
                topMatch.MatchedUser = matchedUser;

                return new DataResponse<List<MatchResult>>
                {
                    Success = true,
                    Data = new List<MatchResult> { topMatch },
                    Message = "Found 1 best match"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding one-way matches for user {UserId}", userId);
                return new DataResponse<List<MatchResult>>
                {
                    Success = false,
                    Message = "Failed to find matches",
                    ErrorDetails = ex.Message
                };
            }
        }

        /// <summary>
        /// Tìm và lưu các match tốt nhất cho user (tính cả 2 chiều)
        /// </summary>
        public async Task<DataResponse<List<MatchResult>>> FindBestMatchesAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Finding best matches for user {UserId}", userId);

                // Daily limit: max 1 mutual match per day (UTC)
                var todayStart = DateTime.UtcNow.Date;
                var todayEnd = todayStart.AddDays(1);
                var mutualCountToday = await _matchResultRepository.CountAsync(m =>
                    m.UserId == userId &&
                    m.MatchType == "Mutual" &&
                    m.CreatedAt >= todayStart && m.CreatedAt < todayEnd);

                var mutualLimit = Math.Max(0, _matchingOptions?.MutualDailyLimit ?? 1);
                if (mutualCountToday >= mutualLimit)
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Data = new List<MatchResult>(),
                        Message = $"Bạn đã sử dụng hết {mutualLimit} lượt ghép hai chiều hôm nay. Vui lòng thử lại vào ngày mai."
                    };
                }

                // Get user A and their preference
                var userA = await _userRepository.FindByIdAsync(userId, u => u.Preference);
                if (userA == null)
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var preferenceA = await _preferenceRepository.GetByUserIdAsync(userId);
                if (preferenceA == null)
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Message = "User preference not found. Please set your preferences first."
                    };
                }

                // Check if user A has embeddings
                if (string.IsNullOrEmpty(userA.ProfileEmbedding))
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Message = "User profile embedding not found. Please update your profile."
                    };
                }

                if (string.IsNullOrEmpty(preferenceA.PreferenceEmbedding))
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Message = "User preference embedding not found. Please update your preferences."
                    };
                }

                // Parse user A embeddings
                var profileEmbeddingA = ParseEmbedding(userA.ProfileEmbedding);
                var preferenceEmbeddingA = ParseEmbedding(preferenceA.PreferenceEmbedding);

                if (profileEmbeddingA == null || preferenceEmbeddingA == null)
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = false,
                        Message = "Failed to parse embeddings"
                    };
                }

                // Get candidate users
                var candidates = await GetCandidateUsersAsync(userId, preferenceA);
                if (candidates.Count == 0)
                {
                    _logger.LogInformation("No candidates found for user {UserId}", userId);
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = true,
                        Data = new List<MatchResult>(),
                        Message = "No matching candidates found"
                    };
                }

                _logger.LogInformation("Computing match scores for {Count} candidates", candidates.Count);

                // Compute match scores for each candidate
                var matchResults = new List<MatchResult>();

                foreach (var candidateB in candidates)
                {
                    try
                    {
                        // Get candidate's preference
                        var preferenceB = await _preferenceRepository.GetByUserIdAsync(candidateB.Id);
                        if (preferenceB == null || 
                            string.IsNullOrEmpty(candidateB.ProfileEmbedding) || 
                            string.IsNullOrEmpty(preferenceB.PreferenceEmbedding))
                        {
                            _logger.LogDebug("Skipping candidate {CandidateId}: missing embeddings", candidateB.Id);
                            continue;
                        }

                        // Parse candidate embeddings
                        var profileEmbeddingB = ParseEmbedding(candidateB.ProfileEmbedding);
                        var preferenceEmbeddingB = ParseEmbedding(preferenceB.PreferenceEmbedding);

                        if (profileEmbeddingB == null || preferenceEmbeddingB == null)
                        {
                            _logger.LogDebug("Skipping candidate {CandidateId}: failed to parse embeddings", candidateB.Id);
                            continue;
                        }

                        // Compute similarities
                        // sim1: What A wants vs What B is
                        var sim1 = ComputeCosineSimilarity(preferenceEmbeddingA, profileEmbeddingB);

                        // sim2: What B wants vs What A is
                        var sim2 = ComputeCosineSimilarity(preferenceEmbeddingB, profileEmbeddingA);

                        // Average score (convert from [-1, 1] to [0, 100])
                        var matchScore = ((sim1 + sim2) / 2.0 + 1.0) / 2.0 * 100.0;

                        _logger.LogDebug("Match score for user {UserId} and {CandidateId}: {Score:F2} (sim1={Sim1:F3}, sim2={Sim2:F3})",
                            userId, candidateB.Id, matchScore, sim1, sim2);

                        // Create match result
                        var matchResult = new MatchResult
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            MatchedUserId = candidateB.Id,
                            MatchScore = Math.Round(matchScore, 2),
                            AiReasoning = $"Điểm tương thích được tính dựa trên độ phù hợp giữa sở thích tìm kiếm và thông tin cá nhân. " +
                                        $"Độ phù hợp của bạn với {candidateB.FullName ?? "người này"}: {sim1:F3}. " +
                                        $"Độ phù hợp của {candidateB.FullName ?? "người này"} với bạn: {sim2:F3}.",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        matchResults.Add(matchResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error computing match for candidate {CandidateId}", candidateB.Id);
                    }
                }

                if (matchResults.Count == 0)
                {
                    return new DataResponse<List<MatchResult>>
                    {
                        Success = true,
                        Data = new List<MatchResult>(),
                        Message = "No valid matches found"
                    };
                }

                // Sort by match score descending and keep only the best one
                var topMatch = matchResults
                    .OrderByDescending(m => m.MatchScore)
                    .First();

                // Enrich top match with AI reasoning (two-way): profiles + preferences of both sides
                var matchedUser = await _userRepository.FindByIdAsync(topMatch.MatchedUserId, u => u.Preference);
                if (matchedUser != null)
                {
                    var userAProfileText = userA.ProfileText;
                    var userAPrefText = preferenceA.PreferenceText;
                    var userBProfileText = matchedUser.ProfileText;
                    var preferenceB = await _preferenceRepository.GetByUserIdAsync(matchedUser.Id);
                    var userBPrefText = preferenceB != null ? preferenceB.PreferenceText : string.Empty;

                    var reason = await _chatService.GenerateMatchReasonTwoAsync(
                        userAProfileText,
                        userAPrefText,
                        userBProfileText,
                        userBPrefText
                    );

                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        topMatch.AiReasoning = reason;
                    }
                }

                // Mark old matches inactive (keep history for daily limit)
                var toDeactivateMutual = await _matchResultRepository.GetMatchesByUserIdAsync(userId);
                foreach (var oldMatch in toDeactivateMutual)
                {
                    oldMatch.IsActive = false;
                    oldMatch.UpdatedAt = DateTime.UtcNow;
                    _matchResultRepository.Update(oldMatch);
                }

                // Tag match type
                topMatch.MatchType = "Mutual";

                // Attach navigation and housekeeping
                topMatch.User = userA;
                topMatch.MatchedUser = matchedUser;
                topMatch.LastCalculatedAt = DateTime.UtcNow;

                // Save only the best match
                _matchResultRepository.Add(topMatch);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully saved the top mutual match for user {UserId}", userId);

                return new DataResponse<List<MatchResult>>
                {
                    Success = true,
                    Data = new List<MatchResult> { topMatch },
                    Message = "Found 1 best match"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding best matches for user {UserId}", userId);
                return new DataResponse<List<MatchResult>>
                {
                    Success = false,
                    Message = "Failed to find matches",
                    ErrorDetails = ex.Message
                };
            }
        }
    }
}

