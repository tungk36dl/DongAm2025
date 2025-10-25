# So sánh Luồng Cookie: Authentication vs Language

## 🔐 Authentication Cookie Flow (AuthController)

### 1. **Khi User Login/Register**

```csharp
// AuthController.cs - Lines 69-85 & 231-247
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
    new Claim(ClaimTypes.Role, user.Role?.Name ?? "User")
};

var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
var authProperties = new AuthenticationProperties
{
    IsPersistent = true,           // ✅ Cookie tồn tại sau khi đóng browser
    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)  // ✅ Tồn tại 7 ngày
};

await HttpContext.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme, 
    new ClaimsPrincipal(claimsIdentity), 
    authProperties
);
```

### 2. **Cookie Được Tạo**
- **Cookie Name**: `.AspNetCore.Cookies` (default)
- **Giá trị**: Encrypted claims (UserId, Username, Email, Role)
- **Options**:
  - `IsPersistent`: true
  - `ExpiresUtc`: 7 days
  - `Secure`: true (HTTPS only)
  - `HttpOnly`: true (không thể đọc bằng JavaScript)
  - `SameSite`: Lax

### 3. **Khi User Truy Cập Trang**
- Middleware `UseAuthentication()` tự động đọc cookie
- Tạo `User.Identity` với các claims
- Controller có thể kiểm tra: `User.Identity.IsAuthenticated`, `User.IsInRole("Admin")`

### 4. **Khi User Logout**
```csharp
// AuthController.cs - Line 312
await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
```
- Cookie được xóa hoàn toàn

---

## 🌐 Language Cookie Flow (LanguageController) - ĐÃ CẢI TIẾN

### 1. **Khi User Chọn Ngôn Ngữ**

```csharp
// LanguageController.cs - Lines 28-40
Response.Cookies.Append(
    CookieRequestCultureProvider.DefaultCookieName,  // ".AspNetCore.Culture"
    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
    new CookieOptions 
    { 
        Expires = DateTimeOffset.UtcNow.AddYears(1),  // ✅ Tồn tại 1 năm
        IsEssential = true,        // ✅ Cookie bắt buộc (GDPR compliant)
        Path = "/",                // ✅ Áp dụng toàn bộ website
        HttpOnly = false,          // ✅ Có thể đọc bằng JavaScript nếu cần
        Secure = true,             // ✅ HTTPS only (giống Auth cookie)
        SameSite = SameSiteMode.Lax  // ✅ Security (giống Auth cookie)
    }
);
```

### 2. **Cookie Được Tạo**
- **Cookie Name**: `.AspNetCore.Culture`
- **Giá trị**: `c=vi-VN|uic=vi-VN` hoặc `c=en-US|uic=en-US`
  - `c`: Culture (format ngày, số, tiền tệ)
  - `uic`: UI Culture (ngôn ngữ hiển thị text)
- **Options**:
  - `Expires`: 1 year (lâu hơn Auth cookie)
  - `Secure`: true (HTTPS only)
  - `HttpOnly`: false (có thể đọc JS nếu cần)
  - `SameSite`: Lax

### 3. **Khi User Truy Cập Trang**
- Middleware `UseRequestLocalization()` tự động đọc cookie
- Set `CultureInfo.CurrentCulture` và `CultureInfo.CurrentUICulture`
- Views có thể sử dụng `@Localizer["Key"]` để hiển thị text đúng ngôn ngữ

### 4. **Khi User Đổi Ngôn Ngữ**
- Cookie cũ bị ghi đè bằng giá trị mới
- Trang reload với ngôn ngữ mới

---

## 📊 So Sánh Chi Tiết

| Tiêu chí | Authentication Cookie | Language Cookie |
|----------|---------------------|-----------------|
| **Cookie Name** | `.AspNetCore.Cookies` | `.AspNetCore.Culture` |
| **Thời gian tồn tại** | 7 ngày | 1 năm |
| **Secure (HTTPS)** | ✅ Yes | ✅ Yes |
| **HttpOnly** | ✅ Yes (không đọc được JS) | ❌ No (có thể đọc JS) |
| **SameSite** | Lax | Lax |
| **IsEssential** | Auto (via Auth middleware) | ✅ Yes |
| **Encrypted** | ✅ Yes (ASP.NET Core auto) | ❌ No (plain text) |
| **Middleware Đọc** | `UseAuthentication()` | `UseRequestLocalization()` |
| **Thứ tự Middleware** | Sau `UseRouting()` | Sau `UseRouting()` |

---

## 🔄 Luồng Xử Lý Hoàn Chỉnh

### **Request Flow**

```
1. Browser gửi request
   ↓
2. HttpsRedirection middleware
   ↓
3. StaticFiles middleware
   ↓
4. Routing middleware (UseRouting)
   ↓
5. RequestLocalization middleware 
   - Đọc cookie .AspNetCore.Culture
   - Set CultureInfo.CurrentCulture
   - Set CultureInfo.CurrentUICulture
   ↓
6. Authentication middleware
   - Đọc cookie .AspNetCore.Cookies
   - Tạo User.Identity với Claims
   ↓
7. Authorization middleware
   - Kiểm tra User.IsInRole()
   ↓
8. Controller Action
   - Có thể dùng @Localizer["Key"]
   - Có thể kiểm tra User.Identity.IsAuthenticated
   ↓
9. View Rendering
   - Hiển thị text theo ngôn ngữ
   - Hiển thị UI theo role
```

---

## 🐛 Debug Tips

### **Kiểm tra Authentication Cookie**

```csharp
// Trong Controller
if (User.Identity?.IsAuthenticated == true)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var userName = User.FindFirst(ClaimTypes.Name)?.Value;
    var role = User.FindFirst(ClaimTypes.Role)?.Value;
    
    _logger.LogInformation("Auth Cookie - UserId: {UserId}, Username: {Username}, Role: {Role}", 
        userId, userName, role);
}
```

### **Kiểm tra Language Cookie**

```csharp
// Trong Controller
var currentCulture = System.Globalization.CultureInfo.CurrentCulture.Name;
var currentUICulture = System.Globalization.CultureInfo.CurrentUICulture.Name;

_logger.LogInformation("Language Cookie - Culture: {Culture}, UICulture: {UICulture}", 
    currentCulture, currentUICulture);
```

### **Kiểm tra Cookie trong Browser**

1. Mở **Developer Tools** (F12)
2. Vào tab **Application** > **Cookies**
3. Tìm:
   - `.AspNetCore.Cookies` - Authentication
   - `.AspNetCore.Culture` - Language
4. Xem giá trị và thời gian hết hạn

---

## ✅ Cải Tiến Đã Thực Hiện

### 1. **LanguageController Improvements**

```csharp
✅ Thêm Logging (giống AuthController)
✅ Validate culture input (chỉ cho phép vi-VN và en-US)
✅ Set Secure = true (HTTPS only)
✅ Preserve query string khi redirect
✅ Fallback to Referer nếu returnUrl trống
✅ Thêm GetCurrentLanguage() API endpoint
```

### 2. **View Improvements**

```cshtml
✅ Preserve query string: @(Context.Request.Path + Context.Request.QueryString)
✅ Highlight ngôn ngữ đang chọn với checkmark icon
✅ Thêm background color cho item đang active
```

### 3. **Middleware Order Fix**

```csharp
✅ UseRouting() → UseRequestLocalization() → UseAuthentication()
   (đúng thứ tự theo Microsoft best practices)
```

---

## 🎯 Testing Checklist

### **Test Authentication Cookie**
- [ ] Login thành công → Cookie được tạo
- [ ] Đóng browser → Mở lại → Vẫn đăng nhập (IsPersistent = true)
- [ ] Sau 7 ngày → Cookie hết hạn → Phải login lại
- [ ] Logout → Cookie bị xóa → Không còn đăng nhập

### **Test Language Cookie**
- [ ] Chọn Tiếng Việt → Trang reload → Hiển thị tiếng Việt
- [ ] Chọn English → Trang reload → Hiển thị English
- [ ] Đóng browser → Mở lại → Ngôn ngữ vẫn được giữ
- [ ] Check browser cookies → Thấy `.AspNetCore.Culture`
- [ ] Checkmark hiển thị đúng ngôn ngữ đang chọn

### **Test Cookie Security**
- [ ] HTTP → HTTPS redirect hoạt động
- [ ] Cookie chỉ hoạt động trên HTTPS
- [ ] Cookie có SameSite = Lax (chống CSRF)

---

## 📝 Notes

1. **Authentication Cookie** dùng ASP.NET Core Identity infrastructure (mã hóa, secure)
2. **Language Cookie** dùng plain text (không cần mã hóa, chỉ là preference)
3. Cả hai đều tuân thủ security best practices
4. Cả hai đều có logging đầy đủ để debug
5. Middleware order rất quan trọng - đã được sắp xếp đúng

---

**Kết luận**: Luồng cookie cho Language đã được cải tiến để tương đồng với Authentication cookie về:
- ✅ Security (Secure flag)
- ✅ Logging pattern
- ✅ Error handling
- ✅ Cookie options configuration
- ✅ User experience (checkmark, highlight)

