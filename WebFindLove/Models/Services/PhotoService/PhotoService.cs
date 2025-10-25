using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.Repositories.PhotoRepo;
using WebFindLove.Models.Services.PhotoService.Dto;
using WebFindLove.Models.Services.PhotoService.ViewModels;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Services.PhotoService
{
    /// <summary>
    /// Service implementation cho Photo business logic
    /// </summary>
    public class PhotoService : IPhotoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPhotoRepository _photoRepository;
        private readonly ILogger<PhotoService> _logger;

        public PhotoService(
            IUnitOfWork unitOfWork,
            IPhotoRepository photoRepository,
            ILogger<PhotoService> logger)
        {
            _unitOfWork = unitOfWork;
            _photoRepository = photoRepository;
            _logger = logger;
        }

        public async Task<DataResponse<List<PhotoDto>>> GetAllAsync(PhotoSearch? search = null)
        {
            try
            {
                _logger.LogInformation("Getting all photos with search: {@Search}", search);

                var query = _photoRepository.FindAll(p => true);

                // Apply filters
                if (search != null)
                {
                    if (search.UserId.HasValue)
                        query = query.Where(p => p.UserId == search.UserId.Value);

                    if (search.IsPrimary.HasValue)
                        query = query.Where(p => p.IsPrimary == search.IsPrimary.Value);

                    if (search.IsActive.HasValue)
                        query = query.Where(p => p.IsActive == search.IsActive.Value);

                    if (!string.IsNullOrWhiteSpace(search.SearchTerm))
                        query = query.Where(p => p.Description!.Contains(search.SearchTerm));
                }

                // Include User info
                query = query.Include(p => p.User);

                // Apply sorting
                query = search?.SortBy?.ToLower() switch
                {
                    "createdat" => search.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                    _ => query.OrderByDescending(p => p.IsPrimary).ThenByDescending(p => p.CreatedAt)
                };

                var photos = await query.ToListAsync();

                var photoDtos = photos.Select(p => new PhotoDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    PhotoUrl = p.PhotoUrl,
                    IsPrimary = p.IsPrimary,
                    IsActive = p.IsActive,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt,
                    UserName = p.User?.UserName
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} photos", photoDtos.Count);
                return new DataResponse<List<PhotoDto>>
                {
                    Success = true,
                    Data = photoDtos,
                    Message = $"Retrieved {photoDtos.Count} photo(s)"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting photos");
                return new DataResponse<List<PhotoDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve photos",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<PhotoDto>> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting photo by ID: {PhotoId}", id);

                var photo = await _photoRepository
                    .FindAll(p => p.Id == id)
                    .Include(p => p.User)
                    .FirstOrDefaultAsync();

                if (photo == null)
                {
                    _logger.LogWarning("Photo not found: {PhotoId}", id);
                    return new DataResponse<PhotoDto>
                    {
                        Success = false,
                        Message = "Photo not found"
                    };
                }

                var photoDto = new PhotoDto
                {
                    Id = photo.Id,
                    UserId = photo.UserId,
                    PhotoUrl = photo.PhotoUrl,
                    IsPrimary = photo.IsPrimary,
                    IsActive = photo.IsActive,
                    Description = photo.Description,
                    CreatedAt = photo.CreatedAt,
                    UserName = photo.User?.UserName
                };

                _logger.LogInformation("Successfully retrieved photo: {PhotoId}", id);
                return new DataResponse<PhotoDto>
                {
                    Success = true,
                    Data = photoDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting photo: {PhotoId}", id);
                return new DataResponse<PhotoDto>
                {
                    Success = false,
                    Message = "Failed to retrieve photo",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<List<PhotoDto>>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Getting photos for user: {UserId}", userId);

                var photos = await _photoRepository.GetByUserIdAsync(userId);

                var photoDtos = photos.Select(p => new PhotoDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    PhotoUrl = p.PhotoUrl,
                    IsPrimary = p.IsPrimary,
                    IsActive = p.IsActive,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} photos for user: {UserId}", photoDtos.Count, userId);
                return new DataResponse<List<PhotoDto>>
                {
                    Success = true,
                    Data = photoDtos,
                    Message = $"Retrieved {photoDtos.Count} photo(s)"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting photos for user: {UserId}", userId);
                return new DataResponse<List<PhotoDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve photos",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<PhotoDto>> GetPrimaryPhotoAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("Getting primary photo for user: {UserId}", userId);

                var photo = await _photoRepository.GetPrimaryPhotoAsync(userId);

                if (photo == null)
                {
                    _logger.LogWarning("Primary photo not found for user: {UserId}", userId);
                    return new DataResponse<PhotoDto>
                    {
                        Success = false,
                        Message = "Primary photo not found"
                    };
                }

                var photoDto = new PhotoDto
                {
                    Id = photo.Id,
                    UserId = photo.UserId,
                    PhotoUrl = photo.PhotoUrl,
                    IsPrimary = photo.IsPrimary,
                    IsActive = photo.IsActive,
                    Description = photo.Description,
                    CreatedAt = photo.CreatedAt
                };

                return new DataResponse<PhotoDto>
                {
                    Success = true,
                    Data = photoDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting primary photo for user: {UserId}", userId);
                return new DataResponse<PhotoDto>
                {
                    Success = false,
                    Message = "Failed to retrieve primary photo",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<PhotoDto>> CreateAsync(PhotoCreateVM model, Guid? createdBy = null)
        {
            try
            {
                _logger.LogInformation("Creating photo for user: {UserId}", model.UserId);

                // If this is set as primary, unset other primary photos
                if (model.IsPrimary)
                {
                    var existingPrimary = await _photoRepository.GetPrimaryPhotoAsync(model.UserId);
                    if (existingPrimary != null)
                    {
                        existingPrimary.IsPrimary = false;
                        _photoRepository.Update(existingPrimary);
                    }
                }

                var photo = new Photo
                {
                    UserId = model.UserId,
                    PhotoUrl = model.PhotoUrl,
                    IsPrimary = model.IsPrimary,
                    Description = model.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _photoRepository.Add(photo);
                await _unitOfWork.SaveChangesAsync();

                var photoDto = new PhotoDto
                {
                    Id = photo.Id,
                    UserId = photo.UserId,
                    PhotoUrl = photo.PhotoUrl,
                    IsPrimary = photo.IsPrimary,
                    IsActive = photo.IsActive,
                    Description = photo.Description,
                    CreatedAt = photo.CreatedAt
                };

                _logger.LogInformation("Successfully created photo: {PhotoId}", photo.Id);
                return new DataResponse<PhotoDto>
                {
                    Success = true,
                    Data = photoDto,
                    Message = "Photo created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating photo for user: {UserId}", model.UserId);
                return new DataResponse<PhotoDto>
                {
                    Success = false,
                    Message = "Failed to create photo",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<PhotoDto>> UpdateAsync(PhotoUpdateVM model, Guid? updatedBy = null)
        {
            try
            {
                _logger.LogInformation("Updating photo: {PhotoId}", model.Id);

                var photo = await _photoRepository.FindByIdAsync(model.Id);
                if (photo == null)
                {
                    _logger.LogWarning("Photo not found: {PhotoId}", model.Id);
                    return new DataResponse<PhotoDto>
                    {
                        Success = false,
                        Message = "Photo not found"
                    };
                }

                // If this is set as primary, unset other primary photos
                if (model.IsPrimary && !photo.IsPrimary)
                {
                    var existingPrimary = await _photoRepository.GetPrimaryPhotoAsync(photo.UserId);
                    if (existingPrimary != null)
                    {
                        existingPrimary.IsPrimary = false;
                        _photoRepository.Update(existingPrimary);
                    }
                }

                photo.PhotoUrl = model.PhotoUrl;
                photo.IsPrimary = model.IsPrimary;
                photo.IsActive = model.IsActive;
                photo.Description = model.Description;
                photo.UpdatedAt = DateTime.UtcNow;
                photo.UpdatedBy = updatedBy;

                _photoRepository.Update(photo);
                await _unitOfWork.SaveChangesAsync();

                var photoDto = new PhotoDto
                {
                    Id = photo.Id,
                    UserId = photo.UserId,
                    PhotoUrl = photo.PhotoUrl,
                    IsPrimary = photo.IsPrimary,
                    IsActive = photo.IsActive,
                    Description = photo.Description,
                    CreatedAt = photo.CreatedAt
                };

                _logger.LogInformation("Successfully updated photo: {PhotoId}", photo.Id);
                return new DataResponse<PhotoDto>
                {
                    Success = true,
                    Data = photoDto,
                    Message = "Photo updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating photo: {PhotoId}", model.Id);
                return new DataResponse<PhotoDto>
                {
                    Success = false,
                    Message = "Failed to update photo",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<bool>> SetPrimaryPhotoAsync(Guid photoId, Guid userId)
        {
            try
            {
                _logger.LogInformation("Setting primary photo: {PhotoId} for user: {UserId}", photoId, userId);

                var photo = await _photoRepository.FindByIdAsync(photoId);
                if (photo == null || photo.UserId != userId)
                {
                    _logger.LogWarning("Photo not found or access denied: {PhotoId}", photoId);
                    return new DataResponse<bool>
                    {
                        Success = false,
                        Message = "Photo not found or access denied"
                    };
                }

                // Unset current primary
                var currentPrimary = await _photoRepository.GetPrimaryPhotoAsync(userId);
                if (currentPrimary != null && currentPrimary.Id != photoId)
                {
                    currentPrimary.IsPrimary = false;
                    _photoRepository.Update(currentPrimary);
                }

                // Set new primary
                photo.IsPrimary = true;
                photo.UpdatedAt = DateTime.UtcNow;
                _photoRepository.Update(photo);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully set primary photo: {PhotoId}", photoId);
                return new DataResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Primary photo set successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary photo: {PhotoId}", photoId);
                return new DataResponse<bool>
                {
                    Success = false,
                    Message = "Failed to set primary photo",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<DataResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting photo: {PhotoId}", id);

                var photo = await _photoRepository.FindByIdAsync(id);
                if (photo == null)
                {
                    _logger.LogWarning("Photo not found: {PhotoId}", id);
                    return new DataResponse<bool>
                    {
                        Success = false,
                        Message = "Photo not found"
                    };
                }

                // Soft delete
                photo.IsActive = false;
                photo.UpdatedAt = DateTime.UtcNow;
                _photoRepository.Update(photo);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted photo: {PhotoId}", id);
                return new DataResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Photo deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo: {PhotoId}", id);
                return new DataResponse<bool>
                {
                    Success = false,
                    Message = "Failed to delete photo",
                    ErrorDetails = ex.Message
                };
            }
        }
    }
}

