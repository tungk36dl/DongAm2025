# WebFindLove - Project Completion Guide

## ✅ Đã hoàn thành

### 1. Entity Models ✅
- [x] `User.cs` - Đã cập nhật với đầy đủ profile fields
- [x] `Role.cs` - Đã cập nhật
- [x] `UserPreference.cs` - Mới tạo
- [x] `PersonalityTrait.cs` - Mới tạo
- [x] `MatchResult.cs` - Mới tạo
- [x] `Photo.cs` - Mới tạo
- [x] `Message.cs` - Mới tạo

### 2. Database Context ✅
- [x] `AppDbContext.cs` - Đã cập nhật với:
  - Tất cả DbSets
  - Entity configurations
  - Relationships (1:1, 1:many, many:many)
  - Indexes
  - Check constraints
  - Seed data

### 3. Existing Modules ✅
- [x] User Module (Service → Repository → DbContext) ✅
- [x] Role Module (Service → Repository → DbContext) ✅

## 📋 Cần hoàn thiện

Dựa trên pattern của User và Role modules, bạn cần tạo các modules sau:

### Module 1: UserPreference

#### 1. Repository Layer
**`Models/Repositories/UserPreferenceRepo/IUserPreferenceRepository.cs`**
```csharp
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.UserPreferenceRepo
{
    public interface IUserPreferenceRepository : IGenericRepository<UserPreference, Guid>
    {
        Task<UserPreference?> GetByUserIdAsync(Guid userId);
        Task<bool> ExistsForUserAsync(Guid userId);
    }
}
```

**`Models/Repositories/UserPreferenceRepo/UserPreferenceRepository.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.UserPreferenceRepo
{
    public class UserPreferenceRepository : GenericRepository<UserPreference, Guid>, IUserPreferenceRepository
    {
        public UserPreferenceRepository(AppDbContext context) : base(context) { }

        public async Task<UserPreference?> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserPreferences
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<bool> ExistsForUserAsync(Guid userId)
        {
            return await _context.UserPreferences.AnyAsync(p => p.UserId == userId);
        }
    }
}
```

#### 2. Service Layer
**`Models/Services/UserPreferenceService/IUserPreferenceService.cs`**
**`Models/Services/UserPreferenceService/UserPreferenceService.cs`**
**`Models/Services/UserPreferenceService/Dto/UserPreferenceSearch.cs`**
**`Models/Services/UserPreferenceService/ViewModels/UserPreferenceCreateVM.cs`**
**`Models/Services/UserPreferenceService/ViewModels/UserPreferenceUpdateVM.cs`**

#### 3. Controller
**`Controllers/UserPreferencesController.cs`**

#### 4. Views
- `Views/UserPreferences/Index.cshtml`
- `Views/UserPreferences/Create.cshtml`
- `Views/UserPreferences/Edit.cshtml`
- `Views/UserPreferences/Details.cshtml`
- `Views/UserPreferences/Delete.cshtml`

### Module 2: PersonalityTrait
Tương tự như UserPreference với:
- Repository (IPersonalityTraitRepository + PersonalityTraitRepository)
- Service (IPersonalityTraitService + PersonalityTraitService)
- Controller (PersonalityTraitsController)
- Views (Index, Create, Edit, Details, Delete)

### Module 3: Photo
Tương tự nhưng có thêm:
- Upload ảnh functionality
- Image resizing/optimization
- Set primary photo logic

### Module 4: MatchResult
Đặc biệt quan trọng - AI Matching:
- Repository với methods tìm matches
- Service với AI logic (hoặc tích hợp API)
- Calculate match score
- Controller với API endpoints
- Views để hiển thị matches

### Module 5: Message
Real-time messaging:
- Repository với pagination
- Service với conversation grouping
- Controller với SignalR (optional)
- Views với chat UI

## 🔧 Registration Updates

### 1. Repository Registration
**`Models/Repositories/RepositoryRegistration.cs`**
```csharp
public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
{
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IRoleRepository, RoleRepository>();
    services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
    services.AddScoped<IPersonalityTraitRepository, PersonalityTraitRepository>();
    services.AddScoped<IMatchResultRepository, MatchResultRepository>();
    services.AddScoped<IPhotoRepository, PhotoRepository>();
    services.AddScoped<IMessageRepository, MessageRepository>();
    return services;
}
```

### 2. Service Registration
**`Models/Services/ServiceRegistration.cs`**
```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IUserService, UserService.UserService>();
    services.AddScoped<IRoleService, RoleService.RoleService>();
    services.AddScoped<IUserPreferenceService, UserPreferenceService.UserPreferenceService>();
    services.AddScoped<IPersonalityTraitService, PersonalityTraitService.PersonalityTraitService>();
    services.AddScoped<IMatchResultService, MatchResultService.MatchResultService>();
    services.AddScoped<IPhotoService, PhotoService.PhotoService>();
    services.AddScoped<IMessageService, MessageService.MessageService>();
    return services;
}
```

## 📊 Architecture Pattern (Cho tất cả modules)

```
Controller 
    ↓ (inject IService)
Service
    ↓ (inject IRepository + IUnitOfWork + ILogger)
Repository
    ↓ (inherit GenericRepository)
GenericRepository
    ↓ (inject AppDbContext)
AppDbContext
    ↓
Database
```

## 🎯 Example: Creating PhotoService

```csharp
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

    public async Task<DataResponse<List<Photo>>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Getting photos for user: {UserId}", userId);
            
            var photos = await _photoRepository
                .FindAll(p => p.UserId == userId && p.IsActive)
                .OrderByDescending(p => p.IsPrimary)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
                
            return new DataResponse<List<Photo>> { Success = true, Data = photos };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting photos for user: {UserId}", userId);
            return new DataResponse<List<Photo>> 
            { 
                Success = false, 
                Message = "Failed to get photos.", 
                ErrorDetails = ex.Message 
            };
        }
    }

    public async Task<DataResponse<Photo>> SetPrimaryPhotoAsync(Guid photoId, Guid userId)
    {
        try
        {
            _logger.LogInformation("Setting primary photo: {PhotoId} for user: {UserId}", photoId, userId);
            
            // Remove current primary
            var currentPrimary = await _photoRepository
                .FindAll(p => p.UserId == userId && p.IsPrimary)
                .FirstOrDefaultAsync();
                
            if (currentPrimary != null)
            {
                currentPrimary.IsPrimary = false;
                _photoRepository.Update(currentPrimary);
            }
            
            // Set new primary
            var photo = await _photoRepository.FindByIdAsync(photoId);
            if (photo == null || photo.UserId != userId)
            {
                return new DataResponse<Photo> 
                { 
                    Success = false, 
                    Message = "Photo not found or access denied." 
                };
            }
            
            photo.IsPrimary = true;
            _photoRepository.Update(photo);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Primary photo set successfully: {PhotoId}", photoId);
            return new DataResponse<Photo> { Success = true, Data = photo };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary photo: {PhotoId}", photoId);
            return new DataResponse<Photo> 
            { 
                Success = false, 
                Message = "Failed to set primary photo.", 
                ErrorDetails = ex.Message 
            };
        }
    }
}
```

## 🗄️ Database Migration

Sau khi hoàn thành code, chạy migration:

```bash
# Add migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

## 🔍 Testing Checklist

Sau khi tạo xong từng module, test:

1. ✅ Repository methods work
2. ✅ Service business logic correct
3. ✅ Controller actions respond
4. ✅ Views display properly
5. ✅ Logging works
6. ✅ Validation works
7. ✅ Relationships load correctly (Include/ThenInclude)

## 📚 Additional Features to Consider

### 1. AI Matching Algorithm
**`Models/Services/MatchingService/IMatchingService.cs`**
```csharp
public interface IMatchingService
{
    Task<DataResponse<List<MatchResult>>> FindMatchesForUserAsync(Guid userId);
    Task<DataResponse<double>> CalculateMatchScoreAsync(Guid userId1, Guid userId2);
    Task<DataResponse<string>> GenerateAiReasoningAsync(Guid userId1, Guid userId2);
}
```

### 2. Real-time Messaging (SignalR)
**`Hubs/ChatHub.cs`**
```csharp
public class ChatHub : Hub
{
    public async Task SendMessage(string receiverId, string message)
    {
        await Clients.User(receiverId).SendAsync("ReceiveMessage", Context.UserIdentifier, message);
    }
}
```

### 3. File Upload Service
**`Models/Services/FileService/IFileService.cs`**
```csharp
public interface IFileService
{
    Task<DataResponse<string>> UploadPhotoAsync(IFormFile file, Guid userId);
    Task<DataResponse<bool>> DeletePhotoAsync(string photoUrl);
}
```

## 🎨 UI Enhancements

### Dashboard for Users
- Matches overview
- Recent messages
- Profile completion status
- Recommended matches

### Admin Dashboard
- User statistics
- Match statistics
- System health
- Reports

## 🚀 Next Steps

1. **Tạo từng module theo thứ tự ưu tiên:**
   - UserPreference (cần thiết cho matching)
   - PersonalityTrait (cần thiết cho matching)
   - Photo (cần cho profile)
   - MatchResult (core feature)
   - Message (communication)

2. **Implement AI Matching Service**
   - Integrate với OpenAI API hoặc custom algorithm
   - Calculate compatibility scores
   - Generate explanations

3. **Add Real-time Features**
   - SignalR for messaging
   - Notifications
   - Online status

4. **Testing & Refinement**
   - Unit tests
   - Integration tests
   - UI/UX improvements

## 📖 References

- **User Module**: `Models/Services/UserService/`
- **Role Module**: `Models/Services/RoleService/`
- **Architecture Doc**: `Clean_Architecture_Documentation.md`
- **Logging Doc**: `Logging_Documentation.md`

---

**Current Status**: 
- ✅ Entities Created
- ✅ DbContext Configured
- ⏳ Repositories (User, Role done - 5 more needed)
- ⏳ Services (User, Role done - 5 more needed)
- ⏳ Controllers (User, Role, Auth, Home, Admin done - 5 more needed)
- ⏳ Views (Auth, User, Role done - 5 more needed)

**Estimated Time**: 4-6 hours to complete all remaining modules following the established patterns.

