using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.UnitOfWork;
using WebFindLove.Models.Services.RoleService.Dto;
using WebFindLove.Models.Repositories.RoleRepo;

namespace WebFindLove.Models.Services.RoleService
{
    /// <summary>
    /// Service layer cho Role entity
    /// Pattern: RoleService → IRoleRepository → GenericRepository → UnitOfWork → DbContext
    /// Tuân theo Clean Architecture / Layered Architecture
    /// </summary>
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IUnitOfWork unitOfWork, IRoleRepository roleRepository, ILogger<RoleService> logger)
        {
            _unitOfWork = unitOfWork;
            _roleRepository = roleRepository;
            _logger = logger;
            _logger.LogInformation("RoleService initialized with IRoleRepository");
        }

        public async Task<DataResponse<List<Role>>> GetAllAsync(RoleSearch? search = null)
        {
            try
            {
                _logger.LogDebug("Getting all roles with search params: {@Search}", search);
                
                // Sử dụng FindAll từ IRoleRepository (kế thừa từ IGenericRepository)
                // Có thể include Users nếu cần
                IQueryable<Role> query = _roleRepository.FindAll();

                if (search != null)
                {
                    if (!string.IsNullOrWhiteSpace(search.Query))
                    {
                        var qstr = search.Query.Trim();
                        query = query.Where(r => r.Name.Contains(qstr) || 
                                               (r.Description != null && r.Description.Contains(qstr)));
                    }

                    if (search.IsActive.HasValue)
                        query = query.Where(r => r.IsActive == search.IsActive.Value);

                    // paging
                    var skip = (Math.Max(1, search.Page) - 1) * Math.Max(1, search.PageSize);
                    query = query.Skip(skip).Take(Math.Max(1, search.PageSize));
                }

                var data = await query.OrderBy(r => r.Name).ToListAsync();
                return new DataResponse<List<Role>> { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return new DataResponse<List<Role>> { Success = false, Message = "Failed to get roles.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Role?>> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("Getting role by ID: {RoleId}", id);
                
                // Sử dụng FindByIdAsync từ IRoleRepository với include Users
                var role = await _roleRepository.FindByIdAsync(id, r => r.Users);
                
                if (role != null)
                {
                    _logger.LogDebug("Role found: {RoleName}, UserCount: {UserCount}", role.Name, role.Users?.Count ?? 0);
                }
                
                return new DataResponse<Role?> { Success = true, Data = role };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role by id: {RoleId}", id);
                return new DataResponse<Role?> { Success = false, Message = $"Failed to get role by id: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Role>> AddAsync(Role role)
        {
            if (role == null)
            {
                _logger.LogWarning("AddAsync called with null role");
                return new DataResponse<Role> { Success = false, Message = "Role is required." };
            }

            try
            {
                _logger.LogDebug("Adding new role: {RoleName}", role.Name);
                
                // Validate data annotations
                var ctx = new ValidationContext(role);
                Validator.ValidateObject(role, ctx, validateAllProperties: true);

                // Check uniqueness of Name using IRoleRepository
                var fieldErrors = new Dictionary<string, List<string>>();
                if (!string.IsNullOrWhiteSpace(role.Name))
                {
                    var existsName = await _roleRepository.AnyAsync(r => r.Name == role.Name);
                    if (existsName)
                    {
                        _logger.LogWarning("Role name already exists: {RoleName}", role.Name);
                        fieldErrors.TryAdd(nameof(role.Name), new List<string>());
                        fieldErrors[nameof(role.Name)].Add("Role name already exists.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<Role>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                if (role.Id == Guid.Empty) role.Id = Guid.NewGuid();
                role.CreatedAt = DateTime.UtcNow;
                role.UpdatedAt = DateTime.UtcNow;

                // Sử dụng IRoleRepository để add
                _roleRepository.Add(role);
                
                // Sử dụng UnitOfWork để commit transaction
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Role added successfully: {RoleName}, RoleId: {RoleId}", role.Name, role.Id);
                return new DataResponse<Role> { Success = true, Data = role };
            }
            catch (ValidationException vex)
            {
                _logger.LogWarning("Validation failed when adding role: {Message}", vex.Message);
                return new DataResponse<Role> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding role: {RoleName}", role.Name);
                return new DataResponse<Role> { Success = false, Message = "Failed to add role.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Role>> UpdateAsync(Role role)
        {
            if (role == null)
            {
                _logger.LogWarning("UpdateAsync called with null role");
                return new DataResponse<Role> { Success = false, Message = "Role is required." };
            }

            try
            {
                _logger.LogDebug("Updating role: {RoleId}, {RoleName}", role.Id, role.Name);
                
                var ctx = new ValidationContext(role);
                Validator.ValidateObject(role, ctx, validateAllProperties: true);

                var fieldErrors = new Dictionary<string, List<string>>();
                // Check uniqueness excluding current role using IRoleRepository
                if (!string.IsNullOrWhiteSpace(role.Name))
                {
                    var existsName = await _roleRepository.AnyAsync(r => r.Id != role.Id && r.Name == role.Name);
                    if (existsName)
                    {
                        _logger.LogWarning("Role name already exists when updating: {RoleName}", role.Name);
                        fieldErrors.TryAdd(nameof(role.Name), new List<string>());
                        fieldErrors[nameof(role.Name)].Add("Role name already exists.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<Role>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                role.UpdatedAt = DateTime.UtcNow;
                
                // Sử dụng IRoleRepository để update
                _roleRepository.Update(role);
                
                // Sử dụng UnitOfWork để commit transaction
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Role updated successfully: {RoleName}, RoleId: {RoleId}", role.Name, role.Id);
                return new DataResponse<Role> { Success = true, Data = role };
            }
            catch (ValidationException vex)
            {
                _logger.LogWarning("Validation failed when updating role: {Message}", vex.Message);
                return new DataResponse<Role> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role: {RoleId}", role.Id);
                return new DataResponse<Role> { Success = false, Message = "Failed to update role.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<object>> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("Deleting role: {RoleId}", id);
                
                // Sử dụng GetWithUsersAsync từ IRoleRepository để lấy role với users
                var role = await _roleRepository.GetWithUsersAsync(id);
                if (role == null)
                {
                    _logger.LogWarning("Role not found for deletion: {RoleId}", id);
                    return new DataResponse<object> { Success = false, Message = "Role not found." };
                }

                // Check if role is being used by any users
                var userCount = role.Users?.Count ?? 0;
                if (userCount > 0)
                {
                    _logger.LogWarning("Cannot delete role {RoleId} - being used by {UserCount} user(s)", id, userCount);
                    return new DataResponse<object> 
                    { 
                        Success = false, 
                        Message = $"Cannot delete role. It is being used by {userCount} user(s)." 
                    };
                }

                // Sử dụng IRoleRepository để remove
                _roleRepository.Remove(role);
                
                // Sử dụng UnitOfWork để commit transaction
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Role deleted successfully: {RoleName}, RoleId: {RoleId}", role.Name, id);
                return new DataResponse<object> { Success = true, Data = null };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role: {RoleId}", id);
                return new DataResponse<object> { Success = false, Message = $"Failed to delete role: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<List<RoleDto>>> GetAllWithUserCountAsync()
        {
            try
            {
                _logger.LogDebug("Getting all roles with user count");
                
                // Sử dụng FindAll từ IRoleRepository với projection để tối ưu
                var roles = await _roleRepository.FindAll()
                    .Select(r => new RoleDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        IsActive = r.IsActive,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        UserCount = r.Users.Count
                    })
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {RoleCount} roles with user counts", roles.Count);
                return new DataResponse<List<RoleDto>> { Success = true, Data = roles };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles with user count");
                return new DataResponse<List<RoleDto>> { Success = false, Message = "Failed to get roles.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<bool>> IsNameExistsAsync(string name, Guid? excludeId = null)
        {
            try
            {
                _logger.LogDebug("Checking if role name exists: {RoleName}, ExcludeId: {ExcludeId}", name, excludeId);
                
                // Sử dụng AnyAsync từ IRoleRepository
                var exists = await _roleRepository.AnyAsync(r => r.Name == name && (excludeId == null || r.Id != excludeId));
                
                _logger.LogDebug("Role name exists check result: {Exists}", exists);
                return new DataResponse<bool> { Success = true, Data = exists };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role name existence: {RoleName}", name);
                return new DataResponse<bool> { Success = false, Message = "Failed to check role name.", ErrorDetails = ex.Message };
            }
        }
    }
}

