namespace WebFindLove.Models.Services.UserPreferenceService
{
    public interface IUserPreferenceService
    {
        Task<DataResponse<UserPreference>> GetByIdAsync(Guid id);
        Task<DataResponse<UserPreference>> GetByUserIdAsync(Guid userId);
        Task<DataResponse<UserPreference>> CreateOrUpdateAsync(UserPreference model, Guid? userId = null);
        Task<DataResponse<bool>> DeleteAsync(Guid id);
    }
}

