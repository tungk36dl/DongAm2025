# Ví dụ Sử dụng Hệ thống Phân Quyền

## 📌 Ví dụ 1: Áp dụng phân quyền cho UsersController

```csharp
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Helper.Authorization;

namespace WebFindLove.Controllers
{
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // Chỉ cần đăng nhập, không cần permission cụ thể
        public async Task<IActionResult> Profile()
        {
            var userId = UserId; // Từ BaseController
            var user = await _userService.GetByIdAsync(userId.Value);
            return View(user.Data);
        }

        // Cần quyền "Users.Index"
        [PermissionAuthorize("Users.Index")]
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllAsync();
            return View(users.Data);
        }

        // Cần quyền "Users.Create"
        [PermissionAuthorize("Users.Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [PermissionAuthorize("Users.Create")]
        public async Task<IActionResult> Create(User model)
        {
            var result = await _userService.AddAsync(model);
            if (result.Success)
                return RedirectToAction("Index");
            return View(model);
        }

        // Cần quyền "Users.Edit"
        [PermissionAuthorize("Users.Edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            return View(user.Data);
        }

        [HttpPost]
        [PermissionAuthorize("Users.Edit")]
        public async Task<IActionResult> Edit(Guid id, User model)
        {
            var result = await _userService.UpdateAsync(id, model);
            if (result.Success)
                return RedirectToAction("Index");
            return View(model);
        }

        // Cần quyền "Users.Delete"
        [HttpPost]
        [PermissionAuthorize("Users.Delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _userService.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        // Cần CÓ ÍT NHẤT 1 trong 2 quyền
        [PermissionAuthorize("Users.Edit", "Users.Delete")]
        public async Task<IActionResult> Manage(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            return View(user.Data);
        }

        // Cần CÓ TẤT CẢ 2 quyền
        [PermissionAuthorize(true, "Users.Edit", "Users.Delete")]
        public async Task<IActionResult> FullManage(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            return View(user.Data);
        }
    }
}
```

## 📌 Ví dụ 2: Phân quyền theo cấp độ Role

### Setup Roles và Permissions

```
┌──────────────┬─────────────────────────────────────────┐
│ Role         │ Permissions                             │
├──────────────┼─────────────────────────────────────────┤
│ Admin        │ * (Tất cả - auto bypass)                │
├──────────────┼─────────────────────────────────────────┤
│ Manager      │ Users.Index                             │
│              │ Users.Edit                              │
│              │ Matching.FindMatches                    │
│              │ Matching.MutualMatches                  │
│              │ Messages.Index                          │
│              │ Messages.SendMessage                    │
├──────────────┼─────────────────────────────────────────┤
│ User         │ Matching.FindMatches                    │
│              │ Matching.MutualMatches                  │
│              │ Messages.Index                          │
│              │ Messages.SendMessage                    │
│              │ UserPreferences.Edit (own only)         │
└──────────────┴─────────────────────────────────────────┘
```

### Cách cấp quyền:

1. **Đăng nhập với tài khoản Admin**
2. **Truy cập**: `/Roles/Index`
3. **Chọn Role "Manager"** → Click "Quản lý quyền"
4. **Module Users**:
   - ✅ Check: Index, Edit
   - ❌ Uncheck: Create, Delete
5. **Module Matching**:
   - ✅ Check tất cả (Click "Chọn tất cả" ở header module)
6. **Module Messages**:
   - ✅ Check: Index, SendMessage
   - ❌ Uncheck: Delete
7. **Click "Lưu thay đổi"**

## 📌 Ví dụ 3: Controller chỉ dành cho Admin

```csharp
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Helper.Authorization;

namespace WebFindLove.Controllers
{
    // Cách 1: Dùng PermissionAuthorize với Admin.Manage
    [PermissionAuthorize("Admin.Manage")]
    public class AdminController : BaseController
    {
        // Tất cả actions trong controller này cần quyền "Admin.Manage"
        
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult SystemSettings()
        {
            return View();
        }
    }

    // Cách 2: Check Role trong code (traditional)
    public class AdminController : BaseController
    {
        public IActionResult Dashboard()
        {
            if (UserRole != "Admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            return View();
        }
    }

    // Cách 3: Kết hợp cả hai
    [PermissionAuthorize("Admin.Manage")] // Layer 1: Permission check
    public class AdminController : BaseController
    {
        public IActionResult Dashboard()
        {
            // Layer 2: Additional role check if needed
            if (UserRole != "Admin" && UserRole != "SuperAdmin")
            {
                Logger.LogWarning("Non-admin user accessed admin area");
            }
            return View();
        }
    }
}
```

## 📌 Ví dụ 4: API Controller với Permission

```csharp
using Microsoft.AspNetCore.Mvc;
using WebFindLove.Helper.Authorization;

namespace WebFindLove.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersApiController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersApiController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [PermissionAuthorize("Users.Index")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(new { success = true, data = users.Data });
        }

        [HttpGet("{id}")]
        [PermissionAuthorize("Users.Index")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (!user.Success)
                return NotFound(new { success = false, message = user.Message });
            
            return Ok(new { success = true, data = user.Data });
        }

        [HttpPost]
        [PermissionAuthorize("Users.Create")]
        public async Task<IActionResult> Create([FromBody] User model)
        {
            var result = await _userService.AddAsync(model);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });
            
            return Ok(new { success = true, data = result.Data });
        }

        [HttpPut("{id}")]
        [PermissionAuthorize("Users.Edit")]
        public async Task<IActionResult> Update(Guid id, [FromBody] User model)
        {
            var result = await _userService.UpdateAsync(id, model);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });
            
            return Ok(new { success = true, data = result.Data });
        }

        [HttpDelete("{id}")]
        [PermissionAuthorize("Users.Delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _userService.DeleteAsync(id);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });
            
            return Ok(new { success = true });
        }
    }
}
```

### AJAX Request với Permission Check

```javascript
// Frontend code
async function deleteUser(userId) {
    try {
        const response = await fetch(`/api/UsersApi/${userId}`, {
            method: 'DELETE',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        const data = await response.json();

        if (response.status === 403) {
            // Permission denied
            alert('Bạn không có quyền xóa user này!');
            console.error('Permission denied:', data.message);
            return;
        }

        if (data.success) {
            alert('Xóa user thành công!');
            location.reload();
        } else {
            alert('Lỗi: ' + data.message);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Có lỗi xảy ra!');
    }
}
```

## 📌 Ví dụ 5: Kiểm tra Permission trong View

### Cách 1: Dùng User Claims
```cshtml
@using System.Security.Claims

@{
    var userPermissions = User.Claims
        .Where(c => c.Type == "Permission")
        .Select(c => c.Value)
        .ToList();
        
    var canEditUsers = userPermissions.Contains("Users.Edit");
    var canDeleteUsers = userPermissions.Contains("Users.Delete");
    var isAdmin = User.IsInRole("Admin");
}

<div class="user-actions">
    @if (canEditUsers || isAdmin)
    {
        <a asp-action="Edit" asp-route-id="@Model.Id" class="btn btn-primary">
            <i class="fas fa-edit"></i> Chỉnh sửa
        </a>
    }

    @if (canDeleteUsers || isAdmin)
    {
        <button onclick="deleteUser('@Model.Id')" class="btn btn-danger">
            <i class="fas fa-trash"></i> Xóa
        </button>
    }
</div>
```

### Cách 2: Dùng Helper Method
```csharp
// Trong BaseController hoặc ViewComponent
public static class PermissionHelper
{
    public static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        // Admin có tất cả quyền
        if (user.IsInRole("Admin"))
            return true;

        // Kiểm tra permission claim
        return user.Claims
            .Any(c => c.Type == "Permission" && 
                     c.Value.Equals(permission, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasAnyPermission(ClaimsPrincipal user, params string[] permissions)
    {
        if (user.IsInRole("Admin"))
            return true;

        var userPermissions = user.Claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return permissions.Any(p => userPermissions.Contains(p));
    }
}
```

```cshtml
@using WebFindLove.Helper

@if (PermissionHelper.HasPermission(User, "Users.Edit"))
{
    <a asp-action="Edit" asp-route-id="@Model.Id">Edit</a>
}

@if (PermissionHelper.HasAnyPermission(User, "Users.Edit", "Users.Delete"))
{
    <a asp-action="Manage" asp-route-id="@Model.Id">Manage</a>
}
```

## 📌 Ví dụ 6: Programmatic Permission Check

```csharp
using WebFindLove.Models.Services.RolePermissionService;

public class MatchingController : BaseController
{
    private readonly IRolePermissionService _rolePermissionService;
    private readonly IMatchingService _matchingService;

    public MatchingController(
        IRolePermissionService rolePermissionService,
        IMatchingService matchingService)
    {
        _rolePermissionService = rolePermissionService;
        _matchingService = matchingService;
    }

    public async Task<IActionResult> FindMatches()
    {
        // Kiểm tra permission trong code
        var hasPermission = await _rolePermissionService.CheckUserPermissionAsync(
            UserId.Value, 
            "Matching.FindMatches");

        if (!hasPermission.Success || !hasPermission.Data)
        {
            TempData["Error"] = "Bạn không có quyền tìm kiếm người phù hợp";
            return RedirectToAction("Index", "Home");
        }

        // Logic tìm matches
        var matches = await _matchingService.FindMatchesAsync(UserId.Value);
        return View(matches.Data);
    }

    public async Task<IActionResult> PremiumFeature()
    {
        // Kiểm tra nhiều điều kiện
        var hasPermission = await _rolePermissionService.CheckUserPermissionAsync(
            UserId.Value, 
            "Matching.PremiumFeature");

        var isPremiumUser = User.Claims.Any(c => 
            c.Type == "IsPremium" && c.Value == "true");

        if (!hasPermission.Data && !isPremiumUser)
        {
            return View("UpgradeToPremium");
        }

        // Logic premium feature
        return View();
    }
}
```

## 📌 Ví dụ 7: Seed Initial Permissions cho Roles

```csharp
// Trong DataSeedService.cs
public async Task SeedRolePermissionsAsync()
{
    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
    var managerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Manager");
    var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");

    if (adminRole == null || managerRole == null || userRole == null)
        return;

    // Manager permissions
    var managerPermissions = await _context.Permissions
        .Where(p => 
            p.Name.StartsWith("Users.") && 
            (p.Name.EndsWith("Index") || p.Name.EndsWith("Edit")) ||
            p.Name.StartsWith("Matching.") ||
            p.Name.StartsWith("Messages.") && 
            (p.Name.EndsWith("Index") || p.Name.EndsWith("SendMessage")))
        .Select(p => p.Id)
        .ToListAsync();

    foreach (var permId in managerPermissions)
    {
        if (!await _context.RolePermissions.AnyAsync(rp => 
            rp.RoleId == managerRole.Id && rp.PermissionId == permId))
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = managerRole.Id,
                PermissionId = permId
            });
        }
    }

    // User permissions
    var userPermissions = await _context.Permissions
        .Where(p => 
            p.Name.StartsWith("Matching.") ||
            p.Name.StartsWith("Messages.") ||
            p.Name == "UserPreferences.Edit")
        .Select(p => p.Id)
        .ToListAsync();

    foreach (var permId in userPermissions)
    {
        if (!await _context.RolePermissions.AnyAsync(rp => 
            rp.RoleId == userRole.Id && rp.PermissionId == permId))
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = userRole.Id,
                PermissionId = permId
            });
        }
    }

    await _context.SaveChangesAsync();
    Console.WriteLine("✅ Seeded role permissions successfully");
}
```

## 🎯 Best Practices Summary

### ✅ DO:
- Sử dụng `[PermissionAuthorize]` cho các action cần phân quyền
- Đặt tên Controller/Action rõ ràng và có ý nghĩa
- Group permissions theo module
- Check permissions trong View để ẩn/hiện buttons
- Log access denied events
- Admin role tự động có tất cả quyền

### ❌ DON'T:
- Hard-code permission names ở nhiều nơi (dùng constants)
- Forget to logout/login lại sau khi cấp quyền mới
- Mix authorization logic giữa code và database
- Tạo quá nhiều permissions chi tiết (keep it simple)
- Bypass permission check "tạm thời" rồi quên remove

## 🚀 Quick Start Checklist

- [ ] Chạy ứng dụng → Permissions được auto-sync vào DB
- [ ] Truy cập `/Roles/Index` với tài khoản Admin
- [ ] Chọn Role → Click "Quản lý quyền"
- [ ] Cấp permissions phù hợp cho từng Role
- [ ] Apply `[PermissionAuthorize]` vào Controllers/Actions
- [ ] Test với user thuộc các role khác nhau
- [ ] Kiểm tra log để verify permission checks
- [ ] Done! 🎉

