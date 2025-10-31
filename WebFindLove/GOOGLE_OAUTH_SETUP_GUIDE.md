# 📚 Hướng Dẫn Setup Google OAuth Credentials

## 🎯 Tổng Quan

Hướng dẫn chi tiết cách lấy Google OAuth 2.0 Client ID và Client Secret để sử dụng tính năng đăng nhập bằng Google.

## 📋 Bước 1: Tạo Google Cloud Project

1. Truy cập **Google Cloud Console**:
   - URL: https://console.cloud.google.com/
   - Đăng nhập bằng tài khoản Google của bạn

2. Tạo Project mới:
   - Click **Select a project** ở thanh top
   - Click **New Project**
   - Điền thông tin:
     - **Project name**: `WebFindLove`
     - **Organization**: (Optional)
     - **Location**: (Optional)
   - Click **Create**
   - Chờ project được tạo (30-60 giây)

3. Chọn Project vừa tạo:
   - Click **Select a project** lại
   - Chọn project `WebFindLove` vừa tạo

## 🔑 Bước 2: Bật Google+ API

1. Vào **APIs & Services**:
   - Click menu **☰** (hamburger) ở góc trái
   - Chọn **APIs & Services** → **Library**

2. Tìm và bật Google+ API:
   - Search: `Google+ API`
   - Click vào **Google+ API**
   - Click **Enable**
   - Chờ vài giây cho API được enable

## 🔐 Bước 3: Tạo OAuth Consent Screen

1. Vào **OAuth consent screen**:
   - Click menu **☰**
   - Chọn **APIs & Services** → **OAuth consent screen**

2. Chọn User Type:
   - **External** (recommended cho testing)
   - Click **Create**

3. Điền thông tin App:
   - **App name**: `WebFindLove`
   - **User support email**: Email của bạn
   - **App logo**: (Optional)
   - **App domain**: `localhost` (cho development)
   - **Developer contact information**: Email của bạn

4. Scopes:
   - Click **Add or Remove Scopes**
   - Thêm scopes:
     - `.../auth/userinfo.email`
     - `.../auth/userinfo.profile`
   - Click **Update**
   - Click **Save and Continue**

5. Test users (nếu chọn External):
   - Click **Add Users**
   - Thêm email của bạn
   - Click **Save and Continue**

6. Summary:
   - Review lại thông tin
   - Click **Back to Dashboard**

## 🔑 Bước 4: Tạo OAuth 2.0 Credentials

1. Vào **Credentials**:
   - Click menu **☰**
   - Chọn **APIs & Services** → **Credentials**

2. Tạo OAuth Client ID:
   - Click **+ CREATE CREDENTIALS**
   - Chọn **OAuth client ID**

3. Nếu chưa config OAuth consent screen:
   - Sẽ yêu cầu config
   - Làm theo Bước 3

4. Cấu hình OAuth Client:
   - **Application type**: `Web application`
   - **Name**: `WebFindLove Web Client`
   - **Authorized JavaScript origins**: 
     ```
     http://localhost:5149
     http://localhost:7877
     https://localhost:7092
     https://yourdomain.com
     ```
   - **Authorized redirect URIs**:
     ```
     http://localhost:5149/Auth/GoogleCallback
     http://localhost:7877/Auth/GoogleCallback
     https://localhost:7092/Auth/GoogleCallback
     https://yourdomain.com/Auth/GoogleCallback
     ```

5. Click **Create**

6. Copy Credentials:
   - Một popup sẽ hiện ra với **Client ID** và **Client Secret**
   - **Client ID**: Dạng `xxx.apps.googleusercontent.com`
   - **Client Secret**: Dạng `GOCSPX-xxxxxxxxxxxxx`
   - ⚠️ **Lưu lại ngay** - Client Secret chỉ hiển thị 1 lần!

## 📝 Bước 5: Cập Nhật Configuration

### appsettings.json
```json
{
  "GoogleAuth": {
    "ClientId": "your-client-id.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-your-client-secret"
  }
}
```

### appsettings.Development.json
```json
{
  "GoogleAuth": {
    "ClientId": "your-client-id.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-your-client-secret"
  }
}
```

### appsettings.Production.json
```json
{
  "GoogleAuth": {
    "ClientId": "your-production-client-id.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-your-production-client-secret"
  }
}
```

⚠️ **Lưu ý**: Không commit credentials vào Git! Sử dụng User Secrets hoặc Environment Variables.

## 🧪 Bước 6: Test Setup

1. Chạy ứng dụng:
   ```bash
   dotnet run
   ```

2. Truy cập Login page:
   - URL: `http://localhost:5000/Auth/Login`

3. Click "Đăng nhập bằng Google":
   - Sẽ redirect đến Google OAuth page
   - Chọn tài khoản Google
   - Cho phép quyền truy cập

4. Kiểm tra:
   - Redirect về `/Auth/GoogleCallback`
   - Tạo hoặc đăng nhập tài khoản
   - Redirect về Home/Admin page
   - Đăng nhập thành công!

## 🔍 Troubleshooting

### Lỗi: "redirect_uri_mismatch"
**Nguyên nhân**: Redirect URI không match với authorized redirect URIs
**Giải pháp**: Thêm URI chính xác vào Google Console

### Lỗi: "access_denied"
**Nguyên nhân**: User từ chối permissions hoặc chưa được add vào test users
**Giải pháp**: 
- Thêm user vào test users (nếu OAuth screen là External)
- Hoặc publish OAuth screen (để mọi người dùng được)

### Lỗi: "invalid_client"
**Nguyên nhân**: Client ID hoặc Client Secret sai
**Giải pháp**: Kiểm tra lại credentials trong appsettings.json

### Lỗi: "redirect_uri_mismatch" khi deploy
**Nguyên nhân**: Production URL khác với development URL
**Giải pháp**: 
- Tạo OAuth client mới cho production
- Hoặc thêm production URL vào authorized redirect URIs

## 🌐 Production Setup

1. **Tạo OAuth Client mới cho Production**:
   - Vào Google Console
   - Tạo OAuth client ID mới
   - Set redirect URI là production URL

2. **Update Configuration**:
   - Thêm production credentials vào `appsettings.Production.json`
   - Deploy lên server

3. **Verify**:
   - Test login flow trên production
   - Kiểm tra logging

## 📊 Monitoring

### Google Cloud Console
- Vào **APIs & Services** → **OAuth consent screen** → **Metrics**
- Xem số lượng users, errors, etc.

### Application Logs
- Monitor logs trong `Logs/app-log-*.txt`
- Look for "Google Authentication configured" message
- Check for any authentication errors

## 🔒 Security Best Practices

1. **Never commit credentials**:
   ```bash
   # .gitignore
   appsettings.json
   appsettings.*.json
   ```

2. **Use User Secrets** (Development):
   ```bash
   dotnet user-secrets set "GoogleAuth:ClientId" "your-client-id"
   dotnet user-secrets set "GoogleAuth:ClientSecret" "your-client-secret"
   ```

3. **Use Environment Variables** (Production):
   ```bash
   export GoogleAuth__ClientId="your-client-id"
   export GoogleAuth__ClientSecret="your-client-secret"
   ```

4. **Rotate credentials regularly**: 
   - Change Client Secret mỗi 90 ngày
   - Revoke old credentials

## 📖 Resources

- [Google OAuth 2.0 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [ASP.NET Core Google Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins)
- [Google Cloud Console](https://console.cloud.google.com/)

---

**Next Steps:**
1. ✅ Hoàn tất setup Google OAuth credentials
2. ✅ Test đăng nhập trên development
3. ✅ Deploy lên production và cấu hình lại
4. ✅ Monitor usage và errors

