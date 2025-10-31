# 🚀 Google OAuth Quick Fix Guide

## 🎯 Vấn Đề: "oauth state was missing or invalid"

**Root Cause**: Cookie state không được lưu do thiết lập cookie không tương thích với OAuth.

## ✅ Giải Pháp Cuối Cùng

### Configuration đã fix:

```csharp
// Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.SameAsRequest;
    options.CheckConsentNeeded = context => false;
});

// Cookie Authentication
options.Cookie.SameSite = SameSiteMode.None;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

// Session Cookie
options.Cookie.SameSite = SameSiteMode.None;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
```

## ⚠️ **QUAN TRỌNG NHẤT: PHẢI DÙNG HTTPS**

### ❌ Không chạy với:
```bash
dotnet run                    # HTTP only → FAIL
http://localhost:5149        # HTTP → FAIL  
http://localhost:7877        # HTTP → FAIL
```

### ✅ **PHẢI CHẠY VỚI HTTPS**:
```bash
dotnet run --launch-profile https  # HTTPS → SUCCESS
https://localhost:7092              # HTTPS → SUCCESS
```

## 🔧 Setup Steps

### 1. **Stop App hiện tại**
```bash
# Kill process nếu đang chạy
taskkill /F /IM WebFindLove.exe
```

### 2. **Update Google Console**

Vào: https://console.cloud.google.com/apis/credentials

Tìm OAuth 2.0 Client ID của bạn → **Edit**

**Authorized JavaScript origins:**
```
https://localhost:7092
```

**Authorized redirect URIs:**
```
https://localhost:7092/Auth/GoogleCallback
```

Click **Save**

### 3. **Run với HTTPS**
```bash
dotnet run --launch-profile https
```

**VERIFY**: Log phải show:
```
Listening on: https://localhost:7092
```

### 4. **Test Flow**

1. Mở browser: `https://localhost:7092/Auth/Login`
2. ✅ **Check**: URL phải có `https://` và padlock 🔒
3. Click **"Đăng nhập bằng Google"**
4. Chọn Google account
5. Cho phép permissions
6. ✅ Success: Redirect về app và đăng nhập thành công

## 🧪 Debug Tips

### Check Browser Cookies
1. Open DevTools (F12)
2. Application → Cookies → https://localhost:7092
3. Tìm cookies `.AspNetCore.Correlation.Google` và `.AspNetCore.Cookies`
4. Verify:
   - ✅ `Secure` = true
   - ✅ `SameSite` = None
   - ✅ `Path` = /

### Check Logs
Look for these messages:
```
Google Authentication configured with ClientId: 799977134387-pro49d...
Initiating Google OAuth login
Google OAuth callback received
Google user authenticated - Email: xxx@xxx.com
```

## ❌ Common Mistakes

| Mistake | Result | Fix |
|---------|--------|-----|
| Run với HTTP | OAuth fails | Use HTTPS only |
| Google Console URL sai | redirect_uri_mismatch | Match exact URL |
| Cookie policy conflict | state missing | All cookies must use SameSite=None |
| Old cookies cached | Still fails | Clear all cookies |
| Credentials wrong | invalid_client | Check appsettings.json |

## ✅ Success Checklist

- [x] All cookies: `SameSite=None` + `Secure=Always`
- [x] Run app với HTTPS profile
- [x] Google Console: `https://localhost:7092` added
- [x] Browser: Padlock icon visible
- [x] No old cookies in browser
- [x] Test: Click Google login button

## 📝 Final Configuration Summary

### Program.cs
```csharp
// ✅ CookiePolicy: SameSite=None
// ✅ Cookie Auth: SameSite=None + Secure=Always  
// ✅ Session Cookie: SameSite=None + Secure=Always
// ✅ Google: ClientId + ClientSecret configured
// ✅ Callback: /Auth/GoogleCallback
```

### Google Console
```
✅ ClientId: 799977134387-xxxxx
✅ Redirect URIs: https://localhost:7092/Auth/GoogleCallback
```

### Running
```
✅ Command: dotnet run --launch-profile https
✅ URL: https://localhost:7092
✅ Browser: Shows padlock 🔒
```

---

**🔑 Key Takeaway**: 
```
OAuth2 + SameSite=None = REQUIRES HTTPS
```

**No exceptions. No workarounds. Use HTTPS!**

