namespace WebFindLove.Models.Services.PersonalityTraitService
{
    public interface IPersonalityTraitService
    {
        Task<DataResponse<PersonalityTrait>> GetByIdAsync(Guid id);
        Task<DataResponse<PersonalityTrait>> GetByUserIdAsync(Guid userId);
        Task<DataResponse<List<PersonalityTrait>>> GetByMbtiTypeAsync(string mbtiType);
        Task<DataResponse<PersonalityTrait>> CreateOrUpdateAsync(PersonalityTrait model, Guid? userId = null);
        Task<DataResponse<bool>> DeleteAsync(Guid id);
    }
}

