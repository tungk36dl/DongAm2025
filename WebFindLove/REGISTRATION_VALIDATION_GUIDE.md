# 📝 Hướng dẫn Validation Đăng ký - Registration Validation Guide

## ✅ Tóm tắt

Hệ thống đăng ký đã được tích hợp đầy đủ validation cho **Username** và **Email trùng lặp**, với thông báo lỗi bằng **tiếng Việt** hiển thị chính xác trên giao diện.

---

## 🔍 Các tầng Validation

### 1️⃣ **Client-side Validation (Register.cshtml)**

#### **A. HTML5 Pattern Validation**
```html
<!-- UserName: Chỉ cho phép chữ cái, số và dấu gạch dưới -->
<input type="text" 
       id="UserName" 
       name="UserName"
       pattern="^[a-zA-Z0-9_]+$"
       title="Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới"
       required />
```

#### **B. JavaScript Validation**
```javascript
// Validate UserName format
const userNamePattern = /^[a-zA-Z0-9_]+$/;
if (!userNamePattern.test(userName)) {
    userNameError.textContent = 'Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới (_)';
    document.getElementById('UserName').classList.add('border-red-500');
}

// Validate password match
if (password !== confirmPassword) {
    confirmError.textContent = 'Mật khẩu không khớp';
}

// Validate password length
if (password.length < 6) {
    passwordError.textContent = 'Mật khẩu phải có ít nhất 6 ký tự';
}
```

#### **C. Hiển thị lỗi theo field**
```html
<!-- Lỗi cho từng trường -->
<span asp-validation-for="UserName" class="text-red-500 text-sm"></span>
<span asp-validation-for="Email" class="text-red-500 text-sm"></span>
<span asp-validation-for="FullName" class="text-red-500 text-sm"></span>

<!-- Validation Summary cho lỗi chung -->
<div asp-validation-summary="ModelOnly" class="text-sm"></div>
```

---

### 2️⃣ **Server-side Validation (UserService.cs)**

#### **A. Kiểm tra UserName trùng lặp**
```csharp
if (!string.IsNullOrWhiteSpace(user.UserName))
{
    var existsUserName = await _userRepository.AnyAsync(u => u.UserName == user.UserName);
    if (existsUserName)
    {
        fieldErrors[nameof(user.UserName)].Add("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
    }
}
```

#### **B. Kiểm tra Email trùng lặp**
```csharp
if (!string.IsNullOrWhiteSpace(user.Email))
{
    var existsEmail = await _userRepository.AnyAsync(u => u.Email == user.Email);
    if (existsEmail)
    {
        fieldErrors[nameof(user.Email)].Add("Email đã được sử dụng. Vui lòng sử dụng email khác.");
    }
}
```

#### **C. Trả về lỗi**
```csharp
if (fieldErrors.Any())
{
    return new DataResponse<User>
    {
        Success = false,
        Message = "Có lỗi xảy ra khi đăng ký. Vui lòng kiểm tra lại thông tin.",
        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
    };
}
```

---

### 3️⃣ **Controller Processing (AuthController.cs)**

#### **A. Parse lỗi từ Service**
```csharp
if (!string.IsNullOrEmpty(op.ErrorDetails))
{
    try
    {
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(op.ErrorDetails!);
        if (dict != null)
        {
            foreach (var kv in dict)
            {
                foreach (var err in kv.Value)
                {
                    ModelState.AddModelError(kv.Key, err);
                }
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to parse error details, using raw message");
        ModelState.AddModelError(string.Empty, op.ErrorDetails);
    }
}
```

#### **B. Thêm lỗi vào ModelState**
- Lỗi được thêm với **key là tên field** (UserName, Email)
- ASP.NET Core tự động bind lỗi với `asp-validation-for`
- Lỗi chung được thêm với empty string key

---

## 🎨 Giao diện hiển thị lỗi

### **Trường hợp 1: Lỗi format UserName (Client-side)**
```
╔════════════════════════════════════════════╗
║ [👤] Tên đăng nhập                        ║
║ ┌──────────────────────────────────────┐  ║
║ │ admin@123                            │  ║
║ └──────────────────────────────────────┘  ║
║ ⚠️ Tên đăng nhập chỉ được chứa chữ cái,   ║
║    số và dấu gạch dưới (_)                ║
╚════════════════════════════════════════════╝
```

### **Trường hợp 2: UserName đã tồn tại (Server-side)**
```
╔════════════════════════════════════════════╗
║ ⚠️ Có lỗi xảy ra:                          ║
║ • Có lỗi xảy ra khi đăng ký. Vui lòng     ║
║   kiểm tra lại thông tin.                 ║
╠════════════════════════════════════════════╣
║ [👤] Tên đăng nhập                        ║
║ ┌──────────────────────────────────────┐  ║
║ │ admin                                │  ║
║ └──────────────────────────────────────┘  ║
║ ⚠️ Tên đăng nhập đã tồn tại. Vui lòng     ║
║    chọn tên khác.                         ║
╚════════════════════════════════════════════╝
```

### **Trường hợp 3: Email đã được sử dụng (Server-side)**
```
╔════════════════════════════════════════════╗
║ ⚠️ Có lỗi xảy ra:                          ║
║ • Có lỗi xảy ra khi đăng ký. Vui lòng     ║
║   kiểm tra lại thông tin.                 ║
╠════════════════════════════════════════════╣
║ [📧] Email                                 ║
║ ┌──────────────────────────────────────┐  ║
║ │ admin@example.com                    │  ║
║ └──────────────────────────────────────┘  ║
║ ⚠️ Email đã được sử dụng. Vui lòng sử     ║
║    dụng email khác.                       ║
╚════════════════════════════════════════════╝
```

---

## 🧪 Test Cases

### **Test 1: Đăng ký với UserName có ký tự đặc biệt**
```
Input: UserName = "admin@123"
Expected: ❌ "Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới (_)"
Type: Client-side validation
```

### **Test 2: Đăng ký với UserName đã tồn tại**
```
Input: UserName = "admin" (đã có trong DB)
Expected: ❌ "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác."
Type: Server-side validation
```

### **Test 3: Đăng ký với Email đã tồn tại**
```
Input: Email = "admin@example.com" (đã có trong DB)
Expected: ❌ "Email đã được sử dụng. Vui lòng sử dụng email khác."
Type: Server-side validation
```

### **Test 4: Mật khẩu không khớp**
```
Input: Password = "123456", ConfirmPassword = "654321"
Expected: ❌ "Mật khẩu không khớp"
Type: Client-side validation
```

### **Test 5: Mật khẩu quá ngắn**
```
Input: Password = "123"
Expected: ❌ "Mật khẩu phải có ít nhất 6 ký tự"
Type: Client-side validation
```

### **Test 6: Đăng ký thành công**
```
Input: 
  - UserName = "newuser123" (chưa tồn tại)
  - Email = "newuser@example.com" (chưa tồn tại)
  - Password = "123456"
  - ConfirmPassword = "123456"
Expected: ✅ Redirect to Home page với user đã đăng nhập
```

---

## 🔄 Luồng xử lý (Flow)

```
┌─────────────────────────────────────────┐
│ User nhập thông tin và submit form      │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│ 1. HTML5 Pattern Validation             │
│    - Check UserName format               │
│    - Check required fields               │
└────────────────┬────────────────────────┘
                 │ ✅ Valid
                 ▼
┌─────────────────────────────────────────┐
│ 2. JavaScript Validation (on submit)    │
│    - Check UserName format (regex)       │
│    - Check password match                │
│    - Check password length               │
└────────────────┬────────────────────────┘
                 │ ✅ Valid
                 ▼
┌─────────────────────────────────────────┐
│ 3. POST to AuthController.Register      │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│ 4. UserService.AddAsync                 │
│    - Validate data annotations           │
│    - Check UserName exists in DB         │
│    - Check Email exists in DB            │
└────────────────┬────────────────────────┘
                 │
        ┌────────┴────────┐
        │                 │
        ▼                 ▼
    ❌ Fail           ✅ Success
        │                 │
        ▼                 ▼
┌──────────────┐   ┌──────────────┐
│ Return       │   │ Save to DB   │
│ ErrorDetails │   │ & Sign In    │
│ (JSON)       │   │ user         │
└──────┬───────┘   └──────┬───────┘
       │                  │
       ▼                  ▼
┌──────────────┐   ┌──────────────┐
│ Parse errors │   │ Redirect to  │
│ to ModelState│   │ Home page    │
└──────┬───────┘   └──────────────┘
       │
       ▼
┌──────────────┐
│ Render view  │
│ with errors  │
└──────────────┘
```

---

## 📊 Thống kê Validation

| Loại Validation | Số lượng | Vị trí |
|----------------|----------|--------|
| HTML5 Pattern | 1 | Register.cshtml (UserName) |
| JavaScript | 3 | Register.cshtml (UserName format, password match, password length) |
| Server-side | 2 | UserService.cs (UserName unique, Email unique) |
| **Tổng** | **6** | |

---

## 🎯 Kết luận

### ✅ **Đã hoàn thành:**
1. ✅ Validation UserName format (chỉ chữ cái, số, dấu gạch dưới)
2. ✅ Validation UserName trùng lặp trong database
3. ✅ Validation Email trùng lặp trong database
4. ✅ Validation password match
5. ✅ Validation password length (min 6 ký tự)
6. ✅ Hiển thị lỗi bằng tiếng Việt cho tất cả validation
7. ✅ Hiển thị lỗi theo từng field (asp-validation-for)
8. ✅ Hiển thị lỗi chung (validation summary)
9. ✅ Support dark mode cho thông báo lỗi
10. ✅ UX tốt: Xóa lỗi khi user bắt đầu sửa

### 🎨 **UX Features:**
- 🔴 Border đỏ highlight field có lỗi
- ⚠️ Icon warning cho thông báo lỗi
- 🌙 Dark mode support
- ⚡ Real-time error clearing khi user input
- 📱 Responsive design

### 🔒 **Security:**
- ✅ Anti-forgery token
- ✅ Server-side validation (không chỉ rely client-side)
- ✅ Password hashing
- ✅ Input sanitization

---

## 📝 Ghi chú thêm

### **Đối với UpdateAsync (cập nhật user):**
- ✅ Đã có validation tương tự
- ✅ Exclude current user khi check uniqueness
- ✅ Thông báo lỗi bằng tiếng Việt

### **Các validation khác có thể thêm (Optional):**
- 📧 Validate email format (đang dùng type="email" của HTML5)
- 🔤 Validate UserName length (min/max)
- 💪 Validate password strength (uppercase, lowercase, number, special char)
- 🚫 Validate username không chứa từ cấm (blacklist)

---

**Ngày cập nhật:** 26/10/2025  
**Trạng thái:** ✅ **HOÀN THÀNH - Đã test và hoạt động đúng**

