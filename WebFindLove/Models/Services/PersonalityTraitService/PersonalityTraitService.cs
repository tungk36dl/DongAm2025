using WebFindLove.Models.Repositories.PersonalityTraitRepo;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Services.PersonalityTraitService
{
    public class PersonalityTraitService : IPersonalityTraitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPersonalityTraitRepository _repository;
        private readonly ILogger<PersonalityTraitService> _logger;

        public PersonalityTraitService(
            IUnitOfWork unitOfWork,
            IPersonalityTraitRepository repository,
            ILogger<PersonalityTraitService> logger)
        {
            _unitOfWork = unitOfWork;
            _repository = repository;
            _logger = logger;
        }

        public async Task<DataResponse<PersonalityTrait>> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting personality trait by ID: {Id}", id);
                var trait = await _repository.FindByIdAsync(id);
                
                if (trait == null)
                {
                    return new DataResponse<PersonalityTrait> { Success = false, Message = "Personality trait not found" };
                }

                return new DataResponse<PersonalityTrait> { Success = true, Data = trait };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personality trait: {Id}", id);
                return new DataResponse<PersonalityTrait> { Success = false, Message = "Failed to get trait", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<PersonalityTrait>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Getting personality trait for user: {UserId}", userId);
                var trait = await _repository.GetByUserIdAsync(userId);
                
                if (trait == null)
                {
                    return new DataResponse<PersonalityTrait> { Success = false, Message = "Personality trait not found" };
                }

                return new DataResponse<PersonalityTrait> { Success = true, Data = trait };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trait for user: {UserId}", userId);
                return new DataResponse<PersonalityTrait> { Success = false, Message = "Failed to get trait", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<List<PersonalityTrait>>> GetByMbtiTypeAsync(string mbtiType)
        {
            try
            {
                _logger.LogInformation("Getting personality traits by MBTI type: {MbtiType}", mbtiType);
                var traits = await _repository.GetByMbtiTypeAsync(mbtiType);

                return new DataResponse<List<PersonalityTrait>> { Success = true, Data = traits };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting traits by MBTI type: {MbtiType}", mbtiType);
                return new DataResponse<List<PersonalityTrait>> { Success = false, Message = "Failed to get traits", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<PersonalityTrait>> CreateOrUpdateAsync(PersonalityTrait model, Guid? userId = null)
        {
            try
            {
                _logger.LogInformation("Creating/updating personality trait for user: {UserId}", model.UserId);

                var existing = await _repository.GetByUserIdAsync(model.UserId);
                
                if (existing != null)
                {
                    // Update
                    existing.MbtiType = model.MbtiType;
                    existing.TraitsJson = model.TraitsJson;
                    existing.AiSummary = model.AiSummary;
                    existing.CompatibilityWeight = model.CompatibilityWeight;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = userId;

                    _repository.Update(existing);
                    await _unitOfWork.SaveChangesAsync();

                    return new DataResponse<PersonalityTrait> { Success = true, Data = existing, Message = "Personality trait updated successfully" };
                }
                else
                {
                    // Create
                    model.CreatedAt = DateTime.UtcNow;
                    model.CreatedBy = userId;
                    _repository.Add(model);
                    await _unitOfWork.SaveChangesAsync();

                    return new DataResponse<PersonalityTrait> { Success = true, Data = model, Message = "Personality trait created successfully" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating personality trait for user: {UserId}", model.UserId);
                return new DataResponse<PersonalityTrait> { Success = false, Message = "Failed to save trait", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting personality trait: {Id}", id);
                var trait = await _repository.FindByIdAsync(id);
                if (trait == null)
                {
                    return new DataResponse<bool> { Success = false, Message = "Personality trait not found" };
                }
                _repository.Remove(trait);
                await _unitOfWork.SaveChangesAsync();

                return new DataResponse<bool> { Success = true, Data = true, Message = "Personality trait deleted successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting personality trait: {Id}", id);
                return new DataResponse<bool> { Success = false, Message = "Failed to delete trait", ErrorDetails = ex.Message };
            }
        }
    }
}

