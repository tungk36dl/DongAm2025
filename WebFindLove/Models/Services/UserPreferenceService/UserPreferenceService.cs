using WebFindLove.Models.Repositories.UserPreferenceRepo;
using WebFindLove.Models.UnitOfWork;
using WebFindLove.Models.Services.EmbeddingService;

namespace WebFindLove.Models.Services.UserPreferenceService
{
    public class UserPreferenceService : IUserPreferenceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserPreferenceRepository _repository;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<UserPreferenceService> _logger;

        public UserPreferenceService(
            IUnitOfWork unitOfWork,
            IUserPreferenceRepository repository,
            IEmbeddingService embeddingService,
            ILogger<UserPreferenceService> logger)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task<DataResponse<UserPreference>> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting user preference by ID: {Id}", id);
                var preference = await _repository.FindByIdAsync(id);
                
                if (preference == null)
                {
                    return new DataResponse<UserPreference> { Success = false, Message = "Preference not found" };
                }

                return new DataResponse<UserPreference> { Success = true, Data = preference };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user preference: {Id}", id);
                return new DataResponse<UserPreference> { Success = false, Message = "Failed to get preference", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<UserPreference>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Getting preference for user: {UserId}", userId);
                var preference = await _repository.GetByUserIdAsync(userId);
                
                if (preference == null)
                {
                    return new DataResponse<UserPreference> { Success = false, Message = "Preference not found" };
                }

                return new DataResponse<UserPreference> { Success = true, Data = preference };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preference for user: {UserId}", userId);
                return new DataResponse<UserPreference> { Success = false, Message = "Failed to get preference", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<UserPreference>> CreateOrUpdateAsync(UserPreference model, Guid? userId = null)
        {
            try
            {
                _logger.LogInformation("Creating/updating preference for user: {UserId}", model.UserId);

                var existing = await _repository.GetByUserIdAsync(model.UserId);
                
                if (existing != null)
                {
                    // Update
                    existing.PreferredGender = model.PreferredGender;
                    existing.AgeMin = model.AgeMin;
                    existing.AgeMax = model.AgeMax;
                    existing.MinHeight = model.MinHeight;
                    existing.MaxHeight = model.MaxHeight;
                    existing.LocationPreference = model.LocationPreference;
                    existing.PersonalityPreference = model.PersonalityPreference;
                    existing.InterestPreference = model.InterestPreference;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = userId;

                    _repository.Update(existing);
                    await _unitOfWork.SaveChangesAsync();

                    // Generate and save preference embedding
                    _logger.LogInformation("Generating preference embedding for user {UserId}", model.UserId);
                    var embeddingResult = await _embeddingService.SavePreferenceEmbeddingAsync(existing);
                    if (!embeddingResult.Success)
                    {
                        _logger.LogWarning("Failed to generate preference embedding for user {UserId}: {Message}", 
                            model.UserId, embeddingResult.Message);
                        // Continue even if embedding fails - không block việc cập nhật preference
                    }
                    else
                    {
                        _logger.LogInformation("Preference embedding generated successfully for user {UserId}", model.UserId);
                    }

                    return new DataResponse<UserPreference> { Success = true, Data = existing, Message = "Preference updated successfully" };
                }
                else
                {
                    // Create
                    model.CreatedAt = DateTime.UtcNow;
                    model.CreatedBy = userId;
                    _repository.Add(model);
                    await _unitOfWork.SaveChangesAsync();

                    // Generate and save preference embedding
                    _logger.LogInformation("Generating preference embedding for user {UserId}", model.UserId);
                    var embeddingResult = await _embeddingService.SavePreferenceEmbeddingAsync(model);
                    if (!embeddingResult.Success)
                    {
                        _logger.LogWarning("Failed to generate preference embedding for user {UserId}: {Message}", 
                            model.UserId, embeddingResult.Message);
                        // Continue even if embedding fails - không block việc tạo preference
                    }
                    else
                    {
                        _logger.LogInformation("Preference embedding generated successfully for user {UserId}", model.UserId);
                    }

                    return new DataResponse<UserPreference> { Success = true, Data = model, Message = "Preference created successfully" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating preference for user: {UserId}", model.UserId);
                return new DataResponse<UserPreference> { Success = false, Message = "Failed to save preference", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting user preference: {Id}", id);
                var preference = await _repository.FindByIdAsync(id);
                if (preference == null)
                {
                    return new DataResponse<bool> { Success = false, Message = "Preference not found" };
                }
                _repository.Remove(preference);
                await _unitOfWork.SaveChangesAsync();

                return new DataResponse<bool> { Success = true, Data = true, Message = "Preference deleted successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting preference: {Id}", id);
                return new DataResponse<bool> { Success = false, Message = "Failed to delete preference", ErrorDetails = ex.Message };
            }
        }
    }
}

