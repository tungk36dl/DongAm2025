# 🔔 Hướng Dẫn Sử Dụng Notification Toast

## 📌 Tổng Quan

Hệ thống thông báo popup (toast) được tích hợp sẵn vào `_Layout.cshtml` và tự động hiển thị thông báo từ TempData.

## 🎯 Các Loại Thông Báo

### 1. ✅ Thông Báo Thành Công (Success)
```csharp
TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
```

### 2. ❌ Thông Báo Lỗi (Error)
```csharp
TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xử lý!";
```

### 3. ℹ️ Thông Báo Thông Tin (Info)
```csharp
TempData["InfoMessage"] = "Vui lòng kiểm tra email để xác nhận tài khoản!";
```

### 4. ⚠️ Thông Báo Cảnh Báo (Warning)
```csharp
TempData["WarningMessage"] = "Bạn cần hoàn thiện hồ sơ trước khi tiếp tục!";
```

## 💡 Ví Dụ Sử Dụng Trong Controller

### Ví dụ 1: Sau khi tạo mới thành công
```csharp
[HttpPost]
public async Task<IActionResult> Create(UserViewModel model)
{
    if (!ModelState.IsValid)
    {
        TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin!";
        return View(model);
    }

    var result = await _userService.CreateUserAsync(model);
    
    if (result.Success)
    {
        TempData["SuccessMessage"] = "Tạo người dùng thành công!";
        return RedirectToAction(nameof(Index));
    }
    
    TempData["ErrorMessage"] = result.ErrorMessage;
    return View(model);
}
```

### Ví dụ 2: Sau khi cập nhật
```csharp
[HttpPost]
public async Task<IActionResult> Edit(int id, EditProfileVM model)
{
    if (!ModelState.IsValid)
    {
        TempData["ErrorMessage"] = "Dữ liệu không hợp lệ!";
        return View(model);
    }

    var result = await _userService.UpdateProfileAsync(id, model);
    
    if (result.Success)
    {
        TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
        return RedirectToAction("Profile", new { id });
    }
    
    TempData["ErrorMessage"] = "Không thể cập nhật hồ sơ. Vui lòng thử lại!";
    return View(model);
}
```

### Ví dụ 3: Sau khi xóa
```csharp
[HttpPost]
public async Task<IActionResult> Delete(int id)
{
    var result = await _userService.DeleteAsync(id);
    
    if (result.Success)
    {
        TempData["SuccessMessage"] = "Xóa thành công!";
    }
    else
    {
        TempData["ErrorMessage"] = "Không thể xóa. Vui lòng thử lại!";
    }
    
    return RedirectToAction(nameof(Index));
}
```

### Ví dụ 4: Thông báo cảnh báo
```csharp
public async Task<IActionResult> Profile()
{
    var user = await _userService.GetCurrentUserAsync();
    
    if (user.ProfileCompleteness < 50)
    {
        TempData["WarningMessage"] = "Hồ sơ của bạn chưa đầy đủ. Vui lòng cập nhật để tăng cơ hội ghép đôi!";
    }
    
    return View(user);
}
```

### Ví dụ 5: Thông báo thông tin
```csharp
[HttpPost]
public async Task<IActionResult> SendVerificationEmail()
{
    await _emailService.SendVerificationEmailAsync();
    
    TempData["InfoMessage"] = "Email xác nhận đã được gửi. Vui lòng kiểm tra hộp thư!";
    
    return RedirectToAction("Account");
}
```

## 🎨 Đặc Điểm

### ✨ Tự Động
- Popup tự động hiển thị khi có TempData
- Tự động ẩn sau 5 giây
- Animation mượt mà (slide in/out)

### 🎯 Responsive
- Tự động điều chỉnh vị trí trên mobile
- Hiển thị đẹp trên mọi kích thước màn hình

### 🌙 Dark Mode
- Tự động chuyển màu theo theme
- Màu sắc tương thích với dark/light mode

### 📊 Progress Bar
- Thanh tiến trình hiển thị thời gian còn lại
- Màu sắc theo loại thông báo

## 🛠️ Sử Dụng JavaScript (Optional)

Nếu muốn hiển thị thông báo từ JavaScript (không qua TempData):

```javascript
// Thông báo thành công
showNotification("Thao tác thành công!", "success");

// Thông báo lỗi
showNotification("Đã có lỗi xảy ra!", "error");

// Thông báo thông tin
showNotification("Đây là thông tin quan trọng!", "info");

// Thông báo cảnh báo
showNotification("Hãy cẩn thận!", "warning");
```

### Ví dụ sử dụng trong View với AJAX:

```javascript
$.ajax({
    url: '/api/users/update',
    method: 'POST',
    data: formData,
    success: function(response) {
        showNotification("Cập nhật thành công!", "success");
    },
    error: function(xhr) {
        showNotification("Có lỗi xảy ra: " + xhr.responseText, "error");
    }
});
```

## 📍 Vị Trí File

- **Partial View**: `Views/Shared/_NotificationToast.cshtml`
- **Layout**: `Views/Shared/_Layout.cshtml`

## 🔧 Tùy Chỉnh

### Thay đổi thời gian tự động ẩn:
Trong `_NotificationToast.cshtml`, tìm và thay đổi:
```javascript
setTimeout(() => {
    closeNotification(toast.querySelector('button'));
}, 5000); // Đổi 5000 thành số mili giây mong muốn
```

### Thay đổi vị trí hiển thị:
Trong `_NotificationToast.cshtml`, tìm:
```html
<div id="notification-container" class="fixed top-4 right-4 z-50 ...">
```
Đổi `top-4 right-4` thành vị trí khác:
- `top-4 left-4` - Góc trên trái
- `bottom-4 right-4` - Góc dưới phải
- `bottom-4 left-4` - Góc dưới trái

## ✅ Best Practices

1. **Ngắn gọn**: Giữ message ngắn gọn, dễ hiểu
   ```csharp
   ✅ TempData["SuccessMessage"] = "Lưu thành công!";
   ❌ TempData["SuccessMessage"] = "Chúng tôi đã lưu tất cả các thông tin bạn vừa nhập vào hệ thống thành công...";
   ```

2. **Cụ thể**: Nói rõ điều gì đã xảy ra
   ```csharp
   ✅ TempData["ErrorMessage"] = "Email đã tồn tại!";
   ❌ TempData["ErrorMessage"] = "Lỗi!";
   ```

3. **Hướng dẫn**: Nếu có lỗi, gợi ý cách khắc phục
   ```csharp
   ✅ TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 8 ký tự!";
   ❌ TempData["ErrorMessage"] = "Mật khẩu không hợp lệ!";
   ```

4. **Phù hợp**: Chọn đúng loại thông báo
   - `Success`: Khi thao tác hoàn thành thành công
   - `Error`: Khi có lỗi xảy ra
   - `Info`: Thông tin bổ sung, không quan trọng lắm
   - `Warning`: Cảnh báo người dùng về điều gì đó

## 🚀 Quick Start

Chỉ cần thêm TempData vào Controller, hệ thống sẽ tự động hiển thị popup!

```csharp
TempData["SuccessMessage"] = "Thành công!";
return RedirectToAction("Index");
```

Vậy là xong! 🎉


