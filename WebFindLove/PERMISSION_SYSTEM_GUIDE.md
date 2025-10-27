# Hệ thống Phân Quyền Động (Dynamic Permission System)

## 📋 Tổng quan

Hệ thống phân quyền động cho phép quản lý chi tiết quyền truy cập của từng Role dựa trên Controller và Action. Permissions được tự động đồng bộ từ code vào database khi ứng dụng khởi động.

## 🏗️ Kiến trúc

### 1. **Entities**
- `Permission`: Quyền truy cập (Module.Action format)
- `RolePermission`: Bảng liên kết giữa Role và Permission (Many-to-Many)

### 2. **Components**

```
┌─────────────────────────────────────────────────────────┐
│                  Permission System                       │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  PermissionSeeder (Startup)                             │
│  └─> Quét Controllers/Actions                           │
│      └─> Sync vào bảng Permissions                      │
│                                                          │
│  RolePermissionRepository                                │
│  └─> CRUD operations cho permissions                    │
│                                                          │
│  RolePermissionService                                   │
│  └─> Business logic                                     │
│                                                          │
│  RolePermissionsController                               │
│  └─> UI quản lý permissions                             │
│                                                          │
│  PermissionAuthorizeAttribute                            │
│  └─> Kiểm tra quyền truy cập                            │
│                                                          │
│  AuthController                                          │
│  └─> Lưu permissions vào cookie claims                  │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

## 🚀 Cách sử dụng

### 1. **Quản lý Permissions trong Admin**

#### Truy cập trang quản lý:
```
/Roles/Index → Chọn Role → Click "Quản lý quyền"
```

#### Giao diện quản lý:
- **Tổng quan**: Hiển thị tổng số module, quyền, và số quyền đã cấp
- **Quick Actions**: Chọn tất cả / Bỏ chọn tất cả
- **Module Cards**: 
  - Mỗi module có checkbox "Chọn tất cả" để cấp/thu hồi toàn bộ quyền trong module
  - Danh sách chi tiết các action với checkbox riêng lẻ
- **Lưu thay đổi**: Sync permissions với database

### 2. **Áp dụng phân quyền trong Controller**

#### Cách 1: Phân quyền cho toàn bộ Controller
```csharp
using WebFindLove.Helper.Authorization;

[PermissionAuthorize("Users.Index", "Users.Details")]
public class UsersController : BaseController
{
    // Chỉ user có quyền "Users.Index" HOẶC "Users.Details" mới truy cập được
}
```

#### Cách 2: Phân quyền cho từng Action
```csharp
public class UsersController : BaseController
{
    [PermissionAuthorize("Users.Index")]
    public async Task<IActionResult> Index()
    {
        // Chỉ user có quyền "Users.Index" mới truy cập được
    }

    [PermissionAuthorize("Users.Edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        // Chỉ user có quyền "Users.Edit" mới truy cập được
    }

    [PermissionAuthorize("Users.Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Chỉ user có quyền "Users.Delete" mới truy cập được
    }
}
```

#### Cách 3: Yêu cầu nhiều quyền
```csharp
// Yêu cầu CÓ ÍT NHẤT 1 trong các quyền (OR logic)
[PermissionAuthorize("Users.Edit", "Users.Delete")]
public async Task<IActionResult> Update(Guid id)
{
    // User cần có "Users.Edit" HOẶC "Users.Delete"
}

// Yêu cầu CÓ TẤT CẢ các quyền (AND logic)
[PermissionAuthorize(true, "Users.Edit", "Users.Delete")]
public async Task<IActionResult> UpdateAndDelete(Guid id)
{
    // User cần có "Users.Edit" VÀ "Users.Delete"
}
```

#### Cách 4: Không yêu cầu permission cụ thể (chỉ cần login)
```csharp
[Authorize] // Dùng Authorize thông thường
public async Task<IActionResult> Profile()
{
    // Chỉ cần đăng nhập, không cần permission cụ thể
}
```

### 3. **Luồng hoạt động của Permission System**

```
1. Application Start
   ↓
2. PermissionSeeder.SyncPermissions()
   ├─> Quét tất cả Controllers
   ├─> Lấy tất cả Action methods (IActionResult/Task<IActionResult>)
   ├─> Tạo Permission format: {Module}.{Action}
   ├─> Check duplicate và insert vào DB
   └─> Log kết quả
   ↓
3. User Login
   ├─> Xác thực username/password
   ├─> Lấy permissions của user's role
   ├─> Tạo Claims (NameIdentifier, Name, Email, Role, Permission[])
   ├─> Lưu vào Cookie
   └─> Redirect to Home
   ↓
4. User truy cập trang có [PermissionAuthorize]
   ├─> PermissionAuthorizeAttribute.OnAuthorization()
   ├─> Kiểm tra user đã login chưa
   ├─> Lấy permissions từ Claims
   ├─> So sánh với required permissions
   ├─> IF Admin role → Allow (bypass)
   ├─> IF có permission → Allow
   └─> ELSE → 403 Forbidden / Redirect AccessDenied
```

## 📊 Cấu trúc Database

### Bảng Permissions
```sql
CREATE TABLE Permissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Module NVARCHAR(100) NOT NULL,         -- Tên Controller (vd: "Users")
    Action NVARCHAR(100) NOT NULL,         -- Tên Action (vd: "Edit")
    Name NVARCHAR(255) NOT NULL UNIQUE,    -- Format: "Users.Edit"
    Description NVARCHAR(255),
    IsActive BIT NOT NULL DEFAULT 1
)
```

### Bảng RolePermissions
```sql
CREATE TABLE RolePermissions (
    RoleId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (RoleId, PermissionId),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
)
```

## 🔑 Format của Permission

Permission được tạo theo format: **`{Module}.{Action}`**

### Ví dụ:
```
Users.Index          → GET /Users/Index
Users.Create         → GET/POST /Users/Create
Users.Edit           → GET/POST /Users/Edit
Users.Delete         → POST /Users/Delete
Matching.FindMatches → GET /Matching/FindMatches
Messages.SendMessage → POST /Messages/SendMessage
```

## 🎯 Các tính năng đặc biệt

### 1. **Admin bypass**
- User có Role = "Admin" sẽ tự động có tất cả quyền
- Không cần cấp permissions riêng lẻ cho Admin

### 2. **Auto-sync permissions**
- Mỗi lần khởi động ứng dụng, hệ thống tự động:
  - Quét tất cả Controllers và Actions
  - Thêm permissions mới (nếu có)
  - Không xóa permissions cũ (để giữ lịch sử)

### 3. **AJAX Support**
- `PermissionAuthorizeAttribute` tự động phát hiện AJAX request
- Trả về JSON với status 403 thay vì redirect

### 4. **Detailed Logging**
- Log mọi thao tác: sync permissions, assign/remove, access denied
- Log level: Information cho success, Warning cho access denied

## 📝 Ví dụ thực tế

### Scenario 1: Quản lý Users
```csharp
[PermissionAuthorize("Users.Index")]
public async Task<IActionResult> Index()
{
    // Xem danh sách users
}

[PermissionAuthorize("Users.Create")]
public async Task<IActionResult> Create()
{
    // Tạo user mới
}

[PermissionAuthorize("Users.Edit")]
public async Task<IActionResult> Edit(Guid id)
{
    // Chỉnh sửa user
}

[PermissionAuthorize("Users.Delete")]
[HttpPost]
public async Task<IActionResult> Delete(Guid id)
{
    // Xóa user
}
```

### Scenario 2: Phân quyền theo cấp độ
```csharp
// Role: Admin → Có tất cả quyền (auto)
// Role: Manager → Có: Users.Index, Users.Edit
// Role: User → Có: Users.Index (chỉ xem)
```

Cấp quyền trong admin panel:
1. Truy cập `/Roles/Index`
2. Chọn Role "Manager"
3. Click "Quản lý quyền"
4. Trong module "Users":
   - Check: Index, Edit
   - Uncheck: Create, Delete
5. Click "Lưu thay đổi"

## 🛠️ Troubleshooting

### Lỗi: Permission không được đồng bộ
**Nguyên nhân**: PermissionSeeder không chạy hoặc gặp lỗi

**Giải pháp**:
1. Kiểm tra log khi khởi động app
2. Tìm dòng: `🔍 Bắt đầu quét Controllers để đồng bộ Permissions...`
3. Nếu không thấy → Kiểm tra `Program.cs`:
   ```csharp
   using (var scope = app.Services.CreateScope())
   {
       var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
       PermissionSeeder.SyncPermissions(db);
   }
   ```

### Lỗi: User có quyền nhưng vẫn bị chặn
**Nguyên nhân**: Cookie chưa được refresh sau khi cấp quyền

**Giải pháp**:
1. Logout và Login lại để refresh cookie claims
2. Hoặc implement automatic cookie refresh khi permissions thay đổi

### Lỗi: 403 Forbidden khi truy cập
**Nguyên nhân**: 
- User không có quyền cần thiết
- PermissionAuthorize config sai

**Giải pháp**:
1. Kiểm tra log: `User {Username} attempted to access {Controller}.{Action} without required permissions`
2. Xem permissions của user trong trang AccessDenied
3. Cấp quyền phù hợp trong admin panel

## 🔒 Best Practices

### 1. Đặt tên Controller và Action rõ ràng
```csharp
// ✅ Good
public class UsersController 
{
    public async Task<IActionResult> Index() { }
    public async Task<IActionResult> Edit(Guid id) { }
}
// Permissions: Users.Index, Users.Edit

// ❌ Bad
public class ManageController 
{
    public async Task<IActionResult> DoSomething() { }
}
// Permission: Manage.DoSomething (không rõ ràng)
```

### 2. Nhóm permissions theo module
```csharp
// Module: Users
- Users.Index
- Users.Create
- Users.Edit
- Users.Delete

// Module: Matching
- Matching.FindMatches
- Matching.MutualMatches
- Matching.ViewProfile
```

### 3. Áp dụng phân quyền ở Controller level khi phù hợp
```csharp
// Nếu toàn bộ controller cần 1 quyền
[PermissionAuthorize("Admin.Manage")]
public class AdminController : BaseController
{
    // Tất cả actions đều cần quyền "Admin.Manage"
}

// Nếu mỗi action cần quyền riêng
public class UsersController : BaseController
{
    [PermissionAuthorize("Users.Index")]
    public async Task<IActionResult> Index() { }
    
    [PermissionAuthorize("Users.Edit")]
    public async Task<IActionResult> Edit(Guid id) { }
}
```

### 4. Sử dụng Admin bypass thông minh
- Admin tự động có tất cả quyền
- Không cần cấp permissions riêng lẻ
- Tập trung quản lý permissions cho các role khác

## 📚 API Reference

### RolePermissionService Methods

```csharp
// Lấy view model để quản lý permissions
Task<DataResponse<ManageRolePermissionsVM>> GetManagePermissionsViewModelAsync(Guid roleId)

// Cập nhật permissions cho role
Task<DataResponse<bool>> UpdateRolePermissionsAsync(UpdateRolePermissionsRequest request)

// Lấy danh sách permissions của user
Task<DataResponse<List<string>>> GetUserPermissionsAsync(Guid userId)

// Kiểm tra user có permission cụ thể không
Task<DataResponse<bool>> CheckUserPermissionAsync(Guid userId, string permissionName)
```

### RolePermissionRepository Methods

```csharp
Task<List<Permission>> GetPermissionsByRoleIdAsync(Guid roleId)
Task<Dictionary<string, List<Permission>>> GetAllPermissionsGroupedByModuleAsync()
Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
Task<bool> AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds)
Task<bool> SyncRolePermissionsAsync(Guid roleId, List<Guid> permissionIds)
```

## 🎓 Summary

Hệ thống phân quyền động của bạn bao gồm:

✅ **Tự động đồng bộ permissions** từ code vào database  
✅ **UI quản lý trực quan** với module/action grouping  
✅ **Cookie-based authorization** với Permission claims  
✅ **PermissionAuthorize Attribute** linh hoạt  
✅ **Admin bypass** tự động  
✅ **Logging chi tiết** để debug và audit  
✅ **AJAX support** cho modern UI  
✅ **AccessDenied page** thân thiện  

Hệ thống đã sẵn sàng để sử dụng! 🚀

