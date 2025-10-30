using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebFindLove.Models.Services.UserService.Dto;
using WebFindLove.Models.Services.UserService.ViewModels;

namespace WebFindLove.Models.Services
{
    public interface IUserService
    {
        Task<DataResponse<List<User>>> GetAllAsync(UserService.Dto.UserSearch? search = null);
        Task<DataResponse<User?>> GetByIdAsync(Guid id);
        Task<DataResponse<UserDto>> GetInfoAsync(Guid id);
        Task<DataResponse<User>> AddAsync(User user);
        Task<DataResponse<User>> UpdateAsync(User user);
        Task<DataResponse<object>> DeleteAsync(Guid id);
        Task<DataResponse<User>> UpdateAccountAsync(EditAccountVM model);
        Task<DataResponse<User>> UpdateProfileAsync(EditProfileVM model);
    }
}
