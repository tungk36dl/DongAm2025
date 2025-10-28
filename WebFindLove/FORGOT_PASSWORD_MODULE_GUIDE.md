# 🔐 Module Quên Mật Khẩu - Hướng Dẫn Sử Dụng

## 📋 Tổng Quan

Module quên mật khẩu cho phép người dùng đặt lại mật khẩu thông qua email. Hệ thống sẽ gửi mã xác nhận 6 chữ số về email của người dùng, mã có hiệu lực trong 15 phút.

## 🏗️ Kiến Trúc Module

### 1. Database Layer
- **Entity**: `PasswordResetToken`
  - `Id`: Guid - Khóa chính
  - `Email`: string - Email người dùng
  - `Token`: string - Mã xác nhận 6 chữ số
  - `ExpiredAt`: DateTime - Thời gian hết hạn
  - `IsUsed`: bool - Đã sử dụng hay chưa
  - `CreatedAt`: DateTime - Thời gian tạo

### 2. Repository Layer
- **Interface**: `IPasswordResetTokenRepository`
- **Implementation**: `PasswordResetTokenRepository`
- **Location**: `Models/Repositories/PasswordResetTokenRepo/`

**Methods**:
```csharp
Task<PasswordResetToken?> GetByTokenAsync(string token)
Task<PasswordResetToken?> GetByEmailAsync(string email)
Task<List<PasswordResetToken>> GetValidTokensByEmailAsync(string email)
Task AddAsync(PasswordResetToken token)
Task UpdateAsync(PasswordResetToken token)
Task DeleteExpiredTokensAsync()
Task InvalidateTokensByEmailAsync(string email)
```

### 3. Service Layer
- **Interface**: `IPasswordResetService`
- **Implementation**: `PasswordResetService`
- **Location**: `Models/Services/PasswordResetService/`

**Methods**:
```csharp
Task<DataResponse<string>> GenerateResetTokenAsync(string email)
Task<DataResponse<bool>> ValidateResetTokenAsync(string token)
Task<DataResponse<bool>> ResetPasswordAsync(string token, string newPassword)
Task CleanupExpiredTokensAsync()
```

### 4. Email Service
- **Interface**: `IEmailService`
- **Implementation**: `EmailService`
- **Location**: `Helper/HelperServices/`

**Configuration**: `appsettings.json`
```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "Port": 587,
  "SenderName": "Support - MyApp",
  "SenderEmail": "yourapp@gmail.com",
  "Password": "your-app-password"
}
```

### 5. Email Template
- **Location**: `Core/Email/Templete/PasswordResetTemplate.html`
- **Placeholders**:
  - `{{FullName}}`: Tên người dùng
  - `{{Token}}`: Mã xác nhận 6 chữ số
  - `{{Year}}`: Năm hiện tại

### 6. Controller Layer
**AuthController** - Endpoints:

#### GET /Auth/ForgotPassword
Hiển thị form nhập email

#### POST /Auth/ForgotPassword
Xử lý yêu cầu gửi mã xác nhận
- **Input**: email
- **Output**: JSON hoặc redirect
- **Process**:
  1. Validate email
  2. Tạo mã 6 chữ số ngẫu nhiên
  3. Lưu token vào database
  4. Gửi email
  5. Redirect đến trang ResetPassword

#### GET /Auth/ResetPassword
Hiển thị form nhập mã và mật khẩu mới

#### POST /Auth/ResetPassword
Xử lý đặt lại mật khẩu
- **Input**: token, newPassword, confirmPassword
- **Output**: JSON hoặc redirect
- **Process**:
  1. Validate input
  2. Kiểm tra mã xác nhận
  3. Hash mật khẩu mới
  4. Cập nhật mật khẩu
  5. Đánh dấu token đã sử dụng
  6. Redirect đến trang Login

### 7. View Layer
- **ForgotPassword.cshtml**: Form nhập email
- **ResetPassword.cshtml**: Form nhập mã và mật khẩu mới
- **Style**: Tailwind CSS + Font Awesome
- **JavaScript**: AJAX form submission

## 🔄 Luồng Hoạt Động

```
1. User clicks "Quên mật khẩu?" trên trang Login
   ↓
2. Hiển thị form nhập email (ForgotPassword.cshtml)
   ↓
3. User nhập email và submit
   ↓
4. Controller gọi PasswordResetService.GenerateResetTokenAsync()
   ↓
5. Service:
   - Kiểm tra email tồn tại
   - Vô hiệu hóa các token cũ
   - Tạo mã 6 chữ số ngẫu nhiên
   - Lưu token vào database (có hiệu lực 15 phút)
   - Gửi email với template đẹp
   ↓
6. Redirect đến ResetPassword.cshtml
   ↓
7. User nhập mã 6 chữ số + mật khẩu mới + xác nhận
   ↓
8. Controller gọi PasswordResetService.ResetPasswordAsync()
   ↓
9. Service:
   - Validate token (chưa hết hạn, chưa dùng)
   - Hash mật khẩu mới
   - Cập nhật password trong database
   - Đánh dấu token đã sử dụng
   ↓
10. Redirect đến Login với thông báo thành công
```

## 📝 Cấu Hình Email Settings

### Gmail Setup
1. Bật 2-Step Verification cho tài khoản Gmail
2. Tạo App Password:
   - Vào Google Account → Security → 2-Step Verification → App passwords
   - Chọn "Mail" và "Other (Custom name)"
   - Copy password được tạo

3. Cập nhật `appsettings.json`:
```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "Port": 587,
  "SenderName": "WebFindLove Support",
  "SenderEmail": "your-email@gmail.com",
  "Password": "your-app-password-here"
}
```

### Outlook/Office365 Setup
```json
"EmailSettings": {
  "SmtpServer": "smtp.office365.com",
  "Port": 587,
  "SenderName": "WebFindLove Support",
  "SenderEmail": "your-email@outlook.com",
  "Password": "your-password"
}
```

## 🧪 Kiểm Tra Module

### 1. Kiểm tra Email Service
```bash
# Đảm bảo có cấu hình email đúng trong appsettings.json
# Chạy ứng dụng và test gửi email
```

### 2. Kiểm tra Forgot Password Flow
1. Truy cập `/Auth/Login`
2. Click "Quên mật khẩu?"
3. Nhập email đã đăng ký
4. Kiểm tra email nhận được mã 6 chữ số
5. Nhập mã và mật khẩu mới
6. Đăng nhập với mật khẩu mới

### 3. Kiểm tra Validation
- Token hết hạn sau 15 phút
- Token chỉ dùng được 1 lần
- Email không tồn tại vẫn hiển thị thông báo thành công (bảo mật)
- Mật khẩu phải >= 6 ký tự
- Mật khẩu xác nhận phải khớp

### 4. Kiểm tra Security
- Token cũ bị vô hiệu hóa khi tạo token mới
- Token được lưu dưới dạng plain text (6 chữ số random)
- Password được hash bằng PasswordHasher
- Email không tiết lộ thông tin tồn tại hay không

## 🔧 Maintenance

### Cleanup Expired Tokens
Nên tạo background job để xóa token hết hạn định kỳ:

```csharp
// Trong Program.cs hoặc tạo Background Service
using (var scope = app.Services.CreateScope())
{
    var passwordResetService = scope.ServiceProvider
        .GetRequiredService<IPasswordResetService>();
    await passwordResetService.CleanupExpiredTokensAsync();
}
```

### Monitoring
- Log tất cả yêu cầu reset password
- Theo dõi số lượng token được tạo
- Cảnh báo nếu có quá nhiều yêu cầu từ 1 email (rate limiting)

## 🎨 Customize

### Thay đổi thời gian hết hạn
`PasswordResetService.cs` - line 66:
```csharp
ExpiredAt = DateTime.UtcNow.AddMinutes(15), // Thay đổi số phút ở đây
```

### Thay đổi độ dài mã
`PasswordResetService.cs` - line 63:
```csharp
var tokenCode = random.Next(100000, 999999).ToString(); // 6 chữ số
// Thay bằng:
var tokenCode = random.Next(1000, 9999).ToString(); // 4 chữ số
```

### Customize Email Template
Chỉnh sửa file: `Core/Email/Templete/PasswordResetTemplate.html`
- Thay đổi màu sắc gradient
- Thay đổi icon
- Thêm logo công ty
- Thay đổi nội dung text

## ⚠️ Lưu Ý Quan Trọng

1. **Bảo mật Email Settings**: 
   - Không commit `appsettings.json` với password thật
   - Sử dụng User Secrets cho development
   - Sử dụng Environment Variables cho production

2. **Rate Limiting**: 
   - Nên giới hạn số lần gửi email cho 1 địa chỉ (VD: 3 lần/giờ)
   - Prevent brute force attack

3. **Email Deliverability**:
   - Cấu hình SPF, DKIM cho domain
   - Tránh spam folder

4. **Token Security**:
   - 6 chữ số random đủ an toàn với expiry 15 phút
   - Có thể tăng lên 8 chữ số nếu muốn bảo mật hơn

## 📚 Dependencies

```xml
<PackageReference Include="MailKit" Version="4.x.x" />
<PackageReference Include="MimeKit" Version="4.x.x" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.x.x" />
```

## ✅ Checklist Hoàn Thành

- [x] Tạo Entity PasswordResetToken
- [x] Tạo Repository và Service
- [x] Tạo Email Template đẹp
- [x] Tạo Controller endpoints
- [x] Tạo Views với Tailwind CSS
- [x] Cập nhật Login view với link Forgot Password
- [x] Đăng ký services trong DI Container
- [x] Tạo và apply migration
- [x] Viết tài liệu hướng dẫn

## 🚀 Next Steps

1. **Test module với email thật**
2. **Thêm rate limiting**
3. **Tạo background job cleanup expired tokens**
4. **Thêm analytics/monitoring**
5. **Thêm multi-language support** (nếu cần)

---

**Created by**: AI Assistant  
**Date**: October 28, 2025  
**Version**: 1.0  

