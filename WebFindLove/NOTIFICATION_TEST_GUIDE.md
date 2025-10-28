# 🧪 Hướng Dẫn Test Notification Toast

## 🚀 Quick Test

Sau khi chạy ứng dụng, bạn có thể test ngay bằng các URL sau:

### 1. Test Thông Báo Thành Công ✅
```
https://localhost:PORT/Home/TestNotifications?type=success
```

### 2. Test Thông Báo Lỗi ❌
```
https://localhost:PORT/Home/TestNotifications?type=error
```

### 3. Test Thông Báo Thông Tin ℹ️
```
https://localhost:PORT/Home/TestNotifications?type=info
```

### 4. Test Thông Báo Cảnh Báo ⚠️
```
https://localhost:PORT/Home/TestNotifications?type=warning
```

### 5. Test Nhiều Thông Báo Cùng Lúc 📢
```
https://localhost:PORT/Home/TestNotifications?type=multiple
```

## 📋 Checklist Test

- [ ] Popup hiển thị ở góc phải trên màn hình
- [ ] Animation trượt vào mượt mà
- [ ] Icon hiển thị đúng theo loại thông báo
- [ ] Màu sắc phù hợp với loại thông báo
- [ ] Progress bar chạy từ 100% về 0%
- [ ] Tự động ẩn sau 5 giây
- [ ] Có thể đóng bằng nút X
- [ ] Animation trượt ra khi đóng
- [ ] Responsive trên mobile
- [ ] Dark mode hoạt động tốt

## 🎨 Kiểm Tra Dark Mode

1. Click vào icon 🌙/☀️ để chuyển dark mode
2. Kiểm tra popup hiển thị đúng màu sắc
3. Chuyển qua lại giữa light/dark mode

## 📱 Kiểm Tra Mobile

1. Mở Developer Tools (F12)
2. Chuyển sang chế độ mobile (Ctrl + Shift + M)
3. Test lại các loại thông báo
4. Kiểm tra popup có căn giữa và responsive không

## ✅ Expected Behavior

### Success Notification
- Border trái: Xanh lá
- Icon: ✓ (check circle)
- Background icon: Xanh lá nhạt
- Progress bar: Xanh lá

### Error Notification
- Border trái: Đỏ
- Icon: ⚠ (exclamation circle)
- Background icon: Đỏ nhạt
- Progress bar: Đỏ

### Info Notification
- Border trái: Xanh dương
- Icon: ℹ (info circle)
- Background icon: Xanh dương nhạt
- Progress bar: Xanh dương

### Warning Notification
- Border trái: Vàng
- Icon: ⚠ (exclamation triangle)
- Background icon: Vàng nhạt
- Progress bar: Vàng

## 🐛 Debug

Nếu popup không hiển thị:

1. Kiểm tra Console (F12) xem có lỗi JavaScript không
2. Kiểm tra TempData có giá trị không
3. Kiểm tra `_NotificationToast.cshtml` đã được include trong `_Layout.cshtml` chưa
4. Xóa cache trình duyệt (Ctrl + Shift + Delete)

## 🔧 Test Trong Code

Thêm vào bất kỳ Controller action nào:

```csharp
TempData["SuccessMessage"] = "Test thành công!";
return RedirectToAction("Index");
```

## 📝 Test Report Template

```
✅ Test Date: [Ngày/Tháng/Năm]
✅ Tester: [Tên người test]

| Test Case | Status | Note |
|-----------|--------|------|
| Success Notification | ✅/❌ | |
| Error Notification | ✅/❌ | |
| Info Notification | ✅/❌ | |
| Warning Notification | ✅/❌ | |
| Multiple Notifications | ✅/❌ | |
| Auto Hide (5s) | ✅/❌ | |
| Manual Close | ✅/❌ | |
| Dark Mode | ✅/❌ | |
| Mobile Responsive | ✅/❌ | |
| Animation Smooth | ✅/❌ | |
```

## 🎯 Performance Test

- [ ] Popup load nhanh (< 100ms)
- [ ] Animation không lag
- [ ] Không ảnh hưởng đến trang chính
- [ ] Memory không leak khi đóng/mở nhiều lần

Chúc bạn test thành công! 🎉



