using WebFindLove.Models.Services.PhotoService.Dto;
using WebFindLove.Models.Services.PhotoService.ViewModels;

namespace WebFindLove.Models.Services.PhotoService
{
    /// <summary>
    /// Interface cho Photo Service
    /// </summary>
    public interface IPhotoService
    {
        Task<DataResponse<List<PhotoDto>>> GetAllAsync(PhotoSearch? search = null);
        Task<DataResponse<PhotoDto>> GetByIdAsync(Guid id);
        Task<DataResponse<List<PhotoDto>>> GetByUserIdAsync(Guid userId);
        Task<DataResponse<PhotoDto>> GetPrimaryPhotoAsync(Guid userId);
        Task<DataResponse<PhotoDto>> CreateAsync(PhotoCreateVM model, Guid? createdBy = null);
        Task<DataResponse<PhotoDto>> UpdateAsync(PhotoUpdateVM model, Guid? updatedBy = null);
        Task<DataResponse<bool>> SetPrimaryPhotoAsync(Guid photoId, Guid userId);
        Task<DataResponse<bool>> DeleteAsync(Guid id);
    }
}

