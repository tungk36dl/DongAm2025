# Clean Architecture / Layered Architecture - WebFindLove

## 📐 Tổng quan kiến trúc

Dự án WebFindLove được xây dựng theo **Clean Architecture / Layered Architecture** chuẩn trong .NET với flow:

```
Controller → Service → Repository → UnitOfWork → DbContext
```

## 🏗️ Cấu trúc chi tiết

### Layer 1: Presentation Layer (Controllers)
**Vị trí**: `WebFindLove/Controllers/`

Controllers chỉ xử lý HTTP requests/responses và gọi Services.

**Ví dụ**:
```csharp
public class UsersController : BaseController
{
    private readonly IUserService _userService;  // ← Inject Service Interface
    
    public async Task<IActionResult> Index([FromQuery] UserSearch? search)
    {
        var resp = await _userService.GetAllAsync(search);  // ← Gọi Service
        return View(resp.Data);
    }
}
```

### Layer 2: Application/Service Layer
**Vị trí**: `WebFindLove/Models/Services/`

Services chứa business logic và validation, gọi Repositories để truy cập data.

#### 2.1 User Module

**Files**:
- `IUserService.cs` - Interface cho UserService
- `UserService.cs` - Implementation với business logic
- `Dto/UserSearch.cs` - Search criteria
- `Dto/UserDto.cs` - Data Transfer Object
- `ViewModels/UserCreateVM.cs` - View Model cho Create
- `ViewModels/UserUpdateVM.cs` - View Model cho Update

**Pattern**:
```csharp
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;  // ← Inject Repository Interface
    
    public async Task<DataResponse<User>> AddAsync(User user)
    {
        // 1. Validation
        Validator.ValidateObject(user, ctx, validateAllProperties: true);
        
        // 2. Business logic (check uniqueness)
        var exists = await _userRepository.AnyAsync(u => u.Email == user.Email);
        
        // 3. Repository operations
        _userRepository.Add(user);
        
        // 4. Commit via UnitOfWork
        await _unitOfWork.SaveChangesAsync();
        
        return new DataResponse<User> { Success = true, Data = user };
    }
}
```

#### 2.2 Role Module

**Files**:
- `IRoleService.cs` - Interface cho RoleService
- `RoleService.cs` - Implementation với business logic
- `Dto/RoleSearch.cs` - Search criteria
- `Dto/RoleDto.cs` - Data Transfer Object với UserCount
- `ViewModels/RoleCreateVM.cs` - View Model cho Create
- `ViewModels/RoleUpdateVM.cs` - View Model cho Update

**Pattern** (giống User):
```csharp
public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRoleRepository _roleRepository;  // ← Inject Repository Interface
    
    public async Task<DataResponse<object>> DeleteAsync(Guid id)
    {
        // 1. Get with relationships
        var role = await _roleRepository.GetWithUsersAsync(id);
        
        // 2. Business validation
        if (role.Users?.Count > 0)
        {
            return new DataResponse<object> 
            { 
                Success = false, 
                Message = $"Cannot delete. Role is used by {role.Users.Count} user(s)." 
            };
        }
        
        // 3. Repository operation
        _roleRepository.Remove(role);
        
        // 4. Commit via UnitOfWork
        await _unitOfWork.SaveChangesAsync();
        
        return new DataResponse<object> { Success = true };
    }
}
```

#### 2.3 Service Registration

**File**: `Models/Services/ServiceRegistration.cs`

```csharp
public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService.UserService>();
        services.AddScoped<IRoleService, RoleService.RoleService>();
        return services;
    }
}
```

### Layer 3: Infrastructure/Repository Layer
**Vị trí**: `WebFindLove/Models/Repositories/`

Repositories trừu tượng hóa data access, kế thừa từ GenericRepository.

#### 3.1 Generic Repository

**Files**:
- `UnitOfWork/IGenericRepository.cs` - Interface cơ sở
- `UnitOfWork/GenericRepository.cs` - Implementation cơ sở

**Các methods cung cấp**:
```csharp
public interface IGenericRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> FindByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includeProperties);
    
    IQueryable<TEntity> FindAll(
        Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[] includeProperties);
    
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    // ... more methods
}
```

#### 3.2 User Repository

**Files**:
- `Repositories/UserRepo/IUserRepository.cs`
- `Repositories/UserRepo/UserRepository.cs`

**Pattern**:
```csharp
// Interface kế thừa từ IGenericRepository
public interface IUserRepository : IGenericRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email);  // ← Custom method
}

// Implementation kế thừa từ GenericRepository
public class UserRepository : GenericRepository<User, Guid>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    // Implement custom method
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}
```

#### 3.3 Role Repository

**Files**:
- `Repositories/RoleRepo/IRoleRepository.cs`
- `Repositories/RoleRepo/RoleRepository.cs`

**Pattern** (giống User):
```csharp
// Interface kế thừa từ IGenericRepository
public interface IRoleRepository : IGenericRepository<Role, Guid>
{
    Task<Role?> GetByNameAsync(string name);           // ← Custom method 1
    Task<Role?> GetWithUsersAsync(Guid id);            // ← Custom method 2
}

// Implementation kế thừa từ GenericRepository
public class RoleRepository : GenericRepository<Role, Guid>, IRoleRepository
{
    public RoleRepository(AppDbContext context) : base(context) { }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<Role?> GetWithUsersAsync(Guid id)
    {
        return await _context.Roles
            .Include(r => r.Users)  // ← Eager loading relationships
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
```

#### 3.4 Repository Registration

**File**: `Models/Repositories/RepositoryRegistration.cs`

```csharp
public static class RepositoryRegistration
{
    public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        return services;
    }
}
```

### Layer 4: Unit of Work Pattern
**Vị trí**: `WebFindLove/Models/UnitOfWork/`

UnitOfWork quản lý transactions và DbContext lifecycle.

**Files**:
- `IUnitOfWork.cs` - Interface
- `UnitOfWork.cs` - Implementation

**Pattern**:
```csharp
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();  // ← Single point of commit
    }
}
```

### Layer 5: Data Access Layer (DbContext)
**Vị trí**: `WebFindLove/Models/AppDbContext.cs`

DbContext là lớp cuối cùng tương tác với database.

```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Entity configurations
        // Relationships
        // Constraints
    }
}
```

## 📊 Flow diagram

### Create User Flow:

```
UsersController.Create(UserCreateVM)
    ↓
UserService.AddAsync(User)
    ↓ (validation & business logic)
IUserRepository.Add(User)
    ↓
GenericRepository<User>.Add(User)
    ↓
UnitOfWork.SaveChangesAsync()
    ↓
AppDbContext.SaveChangesAsync()
    ↓
SQL Database
```

### Delete Role Flow:

```
RolesController.DeleteConfirmed(Guid id)
    ↓
RoleService.DeleteAsync(id)
    ↓
IRoleRepository.GetWithUsersAsync(id)  // Check relationships
    ↓
GenericRepository<Role>.FindByIdAsync(id, r => r.Users)
    ↓
Business validation (check user count)
    ↓
IRoleRepository.Remove(role)
    ↓
GenericRepository<Role>.Remove(role)
    ↓
UnitOfWork.SaveChangesAsync()
    ↓
AppDbContext.SaveChangesAsync()
    ↓
SQL Database
```

## 🎯 Lợi ích của Clean Architecture

### 1. Separation of Concerns
- **Controllers**: HTTP handling only
- **Services**: Business logic & validation
- **Repositories**: Data access only
- **UnitOfWork**: Transaction management
- **DbContext**: Database operations

### 2. Testability
Dễ dàng test từng layer riêng biệt:

```csharp
// Test Service với mock Repository
var mockRepo = new Mock<IUserRepository>();
mockRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
        .ReturnsAsync(false);

var service = new UserService(mockUnitOfWork, mockRepo.Object, mockLogger);
var result = await service.AddAsync(user);

Assert.True(result.Success);
```

### 3. Maintainability
- Thay đổi database provider: chỉ sửa Repository layer
- Thêm business rule: chỉ sửa Service layer
- Thay đổi UI: chỉ sửa Controller/View layer

### 4. Reusability
- Services có thể được dùng bởi Controllers, Background Jobs, APIs
- Repositories có thể được dùng bởi nhiều Services
- Generic Repository cung cấp base functionality cho tất cả entities

### 5. Dependency Inversion
Tất cả dependencies đều inject qua interfaces:

```csharp
// HIGH-LEVEL (Service) depends on ABSTRACTION (IRepository)
// NOT on LOW-LEVEL (Repository implementation)

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;  // ← Interface, not concrete class
    
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
}
```

## 📝 Dependency Injection Setup

**File**: `Program.cs`

```csharp
// 1. Generic Repository
builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

// 2. UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 3. Repositories (sử dụng extension method)
builder.Services.AddInfrastructureRepositories();
    // → IUserRepository → UserRepository
    // → IRoleRepository → RoleRepository

// 4. Services (sử dụng extension method)
builder.Services.AddApplicationServices();
    // → IUserService → UserService
    // → IRoleService → RoleService
```

## 🔄 So sánh User vs Role Module

| Aspect | User Module | Role Module |
|--------|-------------|-------------|
| **Service Interface** | `IUserService` | `IRoleService` |
| **Service Implementation** | `UserService` | `RoleService` |
| **Repository Interface** | `IUserRepository` | `IRoleRepository` |
| **Repository Implementation** | `UserRepository` | `RoleRepository` |
| **Generic Base** | `IGenericRepository<User, Guid>` | `IGenericRepository<Role, Guid>` |
| **Custom Methods** | `GetByEmailAsync()` | `GetByNameAsync()`, `GetWithUsersAsync()` |
| **Business Logic** | Email/Username uniqueness | Name uniqueness, User count validation |
| **Pattern** | Service → Repository → UnitOfWork | Service → Repository → UnitOfWork |

## ✅ Best Practices được áp dụng

### 1. Interface Segregation
Mỗi module có interface riêng với methods cần thiết:
```csharp
public interface IUserService
{
    Task<DataResponse<List<User>>> GetAllAsync(UserSearch? search = null);
    Task<DataResponse<User?>> GetByIdAsync(Guid id);
    Task<DataResponse<User>> AddAsync(User user);
    Task<DataResponse<User>> UpdateAsync(User user);
    Task<DataResponse<object>> DeleteAsync(Guid id);
}
```

### 2. Single Responsibility
Mỗi class có một trách nhiệm duy nhất:
- `UserService`: Business logic cho User
- `UserRepository`: Data access cho User
- `UnitOfWork`: Transaction management
- `UsersController`: HTTP request handling

### 3. DRY (Don't Repeat Yourself)
- GenericRepository cung cấp base CRUD operations
- Specific repositories chỉ implement custom methods
- Extension methods cho registration (AddApplicationServices, AddInfrastructureRepositories)

### 4. Explicit Dependencies
Tất cả dependencies được inject qua constructor:
```csharp
public UserService(
    IUnitOfWork unitOfWork, 
    IUserRepository userRepository, 
    ILogger<UserService> logger)
{
    _unitOfWork = unitOfWork;
    _userRepository = userRepository;
    _logger = logger;
}
```

### 5. Async/Await Pattern
Tất cả database operations đều async:
```csharp
public async Task<DataResponse<User>> AddAsync(User user)
{
    // async validation
    var exists = await _userRepository.AnyAsync(u => u.Email == user.Email);
    
    // sync operation
    _userRepository.Add(user);
    
    // async commit
    await _unitOfWork.SaveChangesAsync();
}
```

## 🎓 Conventions

### Naming Conventions:
- **Interface**: `I{Entity}Service`, `I{Entity}Repository`
- **Implementation**: `{Entity}Service`, `{Entity}Repository`
- **Folder**: `{Entity}Service/`, `{Entity}Repo/`

### Method Naming:
- **Get multiple**: `GetAllAsync()`, `FindAll()`
- **Get single**: `GetByIdAsync()`, `FindByIdAsync()`
- **Create**: `AddAsync()`, `Add()`
- **Update**: `UpdateAsync()`, `Update()`
- **Delete**: `DeleteAsync()`, `Remove()`
- **Check existence**: `AnyAsync()`, `IsNameExistsAsync()`

### Response Pattern:
Sử dụng `DataResponse<T>` wrapper cho tất cả service methods:
```csharp
public class DataResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorDetails { get; set; }
}
```

## 📚 Thêm Module mới

Khi thêm module mới (ví dụ: Product), làm theo các bước:

### 1. Tạo Entity
```csharp
// Models/Entities/Product.cs
public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

### 2. Tạo Repository
```csharp
// Models/Repositories/ProductRepo/IProductRepository.cs
public interface IProductRepository : IGenericRepository<Product, Guid>
{
    Task<Product?> GetByNameAsync(string name);
}

// Models/Repositories/ProductRepo/ProductRepository.cs
public class ProductRepository : GenericRepository<Product, Guid>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }
    
    public async Task<Product?> GetByNameAsync(string name)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Name == name);
    }
}
```

### 3. Đăng ký Repository
```csharp
// Models/Repositories/RepositoryRegistration.cs
public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
{
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IRoleRepository, RoleRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();  // ← Add new
    return services;
}
```

### 4. Tạo Service
```csharp
// Models/Services/ProductService/IProductService.cs
public interface IProductService
{
    Task<DataResponse<List<Product>>> GetAllAsync(ProductSearch? search = null);
    Task<DataResponse<Product?>> GetByIdAsync(Guid id);
    Task<DataResponse<Product>> AddAsync(Product product);
    Task<DataResponse<Product>> UpdateAsync(Product product);
    Task<DataResponse<object>> DeleteAsync(Guid id);
}

// Models/Services/ProductService/ProductService.cs
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;
    
    public ProductService(
        IUnitOfWork unitOfWork, 
        IProductRepository productRepository, 
        ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _logger = logger;
    }
    
    // Implement methods...
}
```

### 5. Đăng ký Service
```csharp
// Models/Services/ServiceRegistration.cs
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IUserService, UserService.UserService>();
    services.AddScoped<IRoleService, RoleService.RoleService>();
    services.AddScoped<IProductService, ProductService.ProductService>();  // ← Add new
    return services;
}
```

### 6. Tạo Controller
```csharp
// Controllers/ProductsController.cs
public class ProductsController : BaseController
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;
    
    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }
    
    // Implement actions...
}
```

## 🎉 Tóm tắt

✅ **Role Module** đã được cấu trúc lại đúng chuẩn Clean Architecture giống **User Module**:

1. ✅ `IRoleRepository` + `RoleRepository` (thay vì dùng `IGenericRepository` trực tiếp)
2. ✅ `RoleService` inject `IRoleRepository` (dependency inversion)
3. ✅ Custom methods: `GetByNameAsync()`, `GetWithUsersAsync()`
4. ✅ Repository registration trong `RepositoryRegistration.cs`
5. ✅ Service registration trong `ServiceRegistration.cs`
6. ✅ Logging đầy đủ trong Service layer
7. ✅ UnitOfWork pattern cho transaction management
8. ✅ Separation of concerns rõ ràng

**Pattern flow hoàn chỉnh**:
```
Controller → IService → Service → IRepository → Repository → UnitOfWork → DbContext → Database
```

Dự án giờ đã tuân theo đúng chuẩn Clean Architecture / Layered Architecture trong .NET! 🚀

