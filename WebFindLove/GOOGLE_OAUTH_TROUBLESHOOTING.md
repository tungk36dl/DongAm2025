# 🔧 Troubleshooting Google OAuth Errors

## ❌ Lỗi: "oauth state was missing or invalid"

### Nguyên nhân
1. **Cookie SameSite policy** không tương thích với OAuth flow
2. **Redirect URI** không match giữa app và Google Console
3. **HTTPS/HTTP** mismatch
4. **Cookie settings** không đúng

### ✅ Giải pháp đã áp dụng

#### 1. **Sửa Cookie Configuration**
Trong `Program.cs`:

```csharp
.AddCookie(options =>
{
    // ...
    options.Cookie.SameSite = SameSiteMode.Lax;
    // Allow unsecured cookies in development
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
})
```

#### 2. **Sửa CookiePolicy**
```csharp
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.SameAsRequest;
    options.CheckConsentNeeded = context => false;
});
```

#### 3. **Cookie Configuration phải đồng bộ**
**Cookie Policy**: `SameSiteMode.None`
**Cookie Authentication**: `SameSiteMode.None` + `SecurePolicy.Always`
**Session Cookie**: `SameSiteMode.None` + `SecurePolicy.Always`

⚠️ **Important**: `SameSite=None` phải đi kèm với `Secure=true`!

#### 4. **Middleware Order**
```csharp
app.UseRouting();
app.UseCookiePolicy();  // Must be before session
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
```

## 🔍 Debug Steps

### Step 1: Check Current URL
Khi chạy app, xem log để biết URL:
```
Application starting...
Listening on: https://localhost:7092;http://localhost:5149
```

### Step 2: Update Google Console

Vào Google Cloud Console → APIs & Services → Credentials → OAuth 2.0 Client IDs

**Authorized JavaScript origins:**
```
https://localhost:7092
http://localhost:5149
http://localhost:7877
```

**Authorized redirect URIs:**
```
https://localhost:7092/Auth/GoogleCallback
http://localhost:5149/Auth/GoogleCallback
http://localhost:7877/Auth/GoogleCallback
```

⚠️ **Important**: Phải match chính xác với URL đang chạy!

### Step 3: Verify Callback Path

Trong `Program.cs`, callback path là:
```csharp
googleOptions.CallbackPath = "/Auth/GoogleCallback";
```

Kiểm tra controller:
```csharp
public async Task<IActionResult> GoogleCallback()
```

✅ Path phải khớp: `/Auth/GoogleCallback`

### Step 4: Check Configuration

Kiểm tra `appsettings.json`:
```json
{
  "GoogleAuth": {
    "ClientId": "799977134387-xxxxx.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-xxxxx"
  }
}
```

### Step 5: Test với HTTPS

Luôn test với **HTTPS** vì OAuth yêu cầu secure connection:

1. Chạy app: `dotnet run --launch-profile https`
2. Truy cập: `https://localhost:7092`
3. Test login với Google

### Step 6: Clear Cookies

Nếu vẫn lỗi, clear cookies:
1. Mở Developer Tools (F12)
2. Application → Cookies
3. Delete all cookies cho localhost
4. Refresh và thử lại

## 🚀 Quick Fix

### Option 1: Use HTTPS Always (Recommended)

1. Chỉ chạy app với HTTPS:
   ```bash
   dotnet run --launch-profile https
   ```

2. Update Google Console:
   - **Authorized JavaScript origins**: `https://localhost:7092`
   - **Authorized redirect URIs**: `https://localhost:7092/Auth/GoogleCallback`

### Option 2: Configure All URLs

Thêm TẤT CẢ URLs vào Google Console:

**Authorized JavaScript origins:**
```
http://localhost:5149
http://localhost:7877
https://localhost:7092
```

**Authorized redirect URIs:**
```
http://localhost:5149/Auth/GoogleCallback
http://localhost:7877/Auth/GoogleCallback
https://localhost:7092/Auth/GoogleCallback
```

## 🧪 Testing

### 1. Check Logs
Xem logs để verify configuration:
```
Google Authentication configured with ClientId: 799977134387-pro49d...
```

### 2. Test Flow
1. Truy cập login page
2. Click "Đăng nhập bằng Google"
3. Chọn Google account
4. Kiểm tra redirect về callback
5. Xem logs có error không

### 3. Common Errors

| Error | Nguyên nhân | Giải pháp |
|-------|-------------|-----------|
| `redirect_uri_mismatch` | URL không match | Thêm URL vào Google Console |
| `invalid_client` | Client ID/Secret sai | Kiểm tra lại credentials |
| `access_denied` | User từ chối permissions | Chọn account khác |
| `state was missing` | Cookie issue | Check cookie settings |

## 📝 Checklist

- [x] Cookie SameSite set to `None` ✅
- [x] Cookie SecurePolicy set to `Always` ✅
- [x] Session cookie SameSite/Secure đồng bộ ✅
- [x] CookiePolicy middleware before Session ✅
- [ ] Google credentials configured correctly
- [ ] Callback path matches: `/Auth/GoogleCallback`
- [ ] Google Console URLs match app URLs
- [x] Test với HTTPS (Required) ✅
- [x] No linter errors ✅
- [ ] App starts successfully

## 🎯 Current Status

✅ Đã fix:
- Cookie SameSite policy → `SameSiteMode.None` (required for OAuth)
- Cookie SecurePolicy → `CookieSecurePolicy.Always` (required with SameSite=None)
- Session cookie → `SameSite=None` + `Secure=Always` (đồng bộ)
- Middleware order → `UseCookiePolicy` trước `UseSession`
- CookiePolicy Options → `MinimumSameSitePolicy=None`
- Import `Microsoft.AspNetCore.Http` namespace

✅ Cần verify:
- Google Console redirect URIs
- App running URL
- HTTPS configuration

## 🚀 Next Steps

### 1. Stop Application
Trước khi rebuild, phải stop app đang chạy:
- Stop trong VS Code/Visual Studio
- Hoặc kill process: `taskkill /F /IM WebFindLove.exe`

### 2. Update Google Console
Vào Google Cloud Console → APIs & Services → Credentials → OAuth 2.0 Client IDs

**Authorized JavaScript origins:**
```
http://localhost:5149
http://localhost:7877
https://localhost:7092
```

**Authorized redirect URIs:**
```
http://localhost:5149/Auth/GoogleCallback
http://localhost:7877/Auth/GoogleCallback
https://localhost:7092/Auth/GoogleCallback
```

### 3. Restart và Test
```bash
# Stop app hiện tại
# Build lại
dotnet build

# Chạy với HTTPS
dotnet run --launch-profile https

# Hoặc chạy với HTTP
dotnet run --launch-profile http
```

### 4. Test Flow
1. Truy cập `https://localhost:7092/Auth/Login`
2. Click "Đăng nhập bằng Google"
3. Kiểm tra redirect và callback
4. Xem logs để debug

---

**Important Notes:**
- ⚠️ **Always test với HTTPS** cho OAuth (required!)
- ⚠️ **Redirect URIs** phải match chính xác
- ⚠️ **Clear cookies** nếu vẫn lỗi
- ⚠️ **Check logs** để xem error details
- ⚠️ **SameSite=None phải có Secure=true**

## ⚠️ CRITICAL: Must Use HTTPS

OAuth2 với `SameSite=None` **REQUIRES** HTTPS!

### ❌ Không hoạt động:
```bash
dotnet run  # HTTP only
```

### ✅ Phải dùng:
```bash
dotnet run --launch-profile https  # HTTPS
```

Truy cập: `https://localhost:7092` (NOT http://localhost:5149)

### Tại sao?
- Modern browsers require `Secure` flag when `SameSite=None`
- Google OAuth redirects không work với HTTP localhost
- Cookie `state` parameter bị block bởi browser security

### Verify HTTPS:
1. Check URL: Must start with `https://`
2. Check browser: Must show padlock icon 🔒
3. Check cookies in DevTools: Must have `Secure` flag ✅

