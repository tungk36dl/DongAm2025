# 🔔 Module Notification - Hướng dẫn đầy đủ

## 📋 Tổng quan

Module Notification cung cấp hệ thống thông báo realtime cho ứng dụng WebFindLove, bao gồm:
- Thông báo khi có tin nhắn mới
- Icon thông báo trên navbar với badge hiển thị số lượng chưa đọc
- Dropdown menu để xem thông báo gần đây
- Trang danh sách thông báo đầy đủ
- Toast notifications realtime
- Âm thanh thông báo
- Tích hợp SignalR cho realtime updates

## 🏗️ Kiến trúc

### 1. Entity & Database
**File**: `Models/Entities/Notification.cs`

```csharp
public class Notification : DomainEntity<Guid>
{
    public string Title { get; set; }                    // Tiêu đề thông báo
    public string Message { get; set; }                  // Nội dung
    public Guid? SenderId { get; set; }                  // Người gửi (có thể null - hệ thống)
    public Guid ReceiverId { get; set; }                 // Người nhận
    public string? Link { get; set; }                    // Link đến chi tiết
    public bool IsRead { get; set; }                     // Đã đọc chưa
    public string? Type { get; set; }                    // Loại: "Message", "System", etc.
    public DateTime CreatedAt { get; set; }              // Thời gian tạo
}
```

**Database Configuration**: `AppDbContext.cs`
- DbSet: `Notifications`
- Indexes trên: `ReceiverId`, `SenderId`, `IsRead`, `CreatedAt`
- Relationships với User (SenderId, ReceiverId)

### 2. Repository Layer
**Files**:
- `Models/Repositories/NotificationRepo/INotificationRepository.cs`
- `Models/Repositories/NotificationRepo/NotificationRepository.cs`

**Methods**:
- `GetUserNotificationsAsync()` - Lấy danh sách thông báo với pagination
- `GetUnreadCountAsync()` - Đếm số thông báo chưa đọc
- `MarkAsReadAsync()` - Đánh dấu một thông báo đã đọc
- `MarkAllAsReadAsync()` - Đánh dấu tất cả đã đọc
- `DeleteOldNotificationsAsync()` - Xóa thông báo cũ

### 3. Service Layer
**Files**:
- `Models/Services/NotificationService/INotificationService.cs`
- `Models/Services/NotificationService/NotificationService.cs`
- `Models/Services/NotificationService/Dto/NotificationDto.cs`
- `Models/Services/NotificationService/Dto/NotificationCreateDto.cs`

**Features**:
- Business logic xử lý thông báo
- Tính toán "time ago" (1 phút trước, 2 giờ trước, etc.)
- Validation dữ liệu
- Error handling

### 4. Controller
**File**: `Controllers/NotificationController.cs`

**Endpoints**:
- `GET /Notification/Index` - Trang danh sách thông báo
- `GET /Notification/GetUnreadCount` - API lấy số lượng chưa đọc
- `GET /Notification/GetRecent` - API lấy thông báo gần đây cho dropdown
- `POST /Notification/MarkAsRead/{id}` - Đánh dấu đã đọc
- `POST /Notification/MarkAllAsRead` - Đánh dấu tất cả đã đọc
- `POST /Notification/Delete/{id}` - Xóa thông báo

### 5. Views
**Files**:
- `Views/Notification/Index.cshtml` - Trang danh sách thông báo
- `Views/Shared/_NotificationDropdown.cshtml` - Dropdown trên navbar

### 6. SignalR Integration
**File**: `Hubs/ChatHub.cs`

**SignalR Methods**:
- `SendNotificationToUser()` - Gửi thông báo realtime đến user
- `NotifyNewMessage()` - Tạo và gửi thông báo khi có tin nhắn mới

**Client Events**:
- `ReceiveNotification` - Event nhận thông báo từ server

## 🚀 Cách sử dụng

### 1. Tạo migration và update database

```bash
dotnet ef migrations add AddNotificationModule
dotnet ef database update
```

### 2. Gửi thông báo từ server

#### Cách 1: Sử dụng NotificationService (Recommended)

```csharp
public class YourController : Controller
{
    private readonly INotificationService _notificationService;
    
    public YourController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    public async Task<IActionResult> SomeAction()
    {
        // Tạo thông báo
        var notificationDto = new NotificationCreateDto
        {
            Title = "Tiêu đề thông báo",
            Message = "Nội dung thông báo",
            SenderId = currentUserId,      // hoặc null nếu là hệ thống
            ReceiverId = targetUserId,
            Link = "/SomeController/SomeAction",
            Type = "System"  // hoặc "Message", "Match", etc.
        };
        
        var result = await _notificationService.CreateNotificationAsync(notificationDto);
        
        if (result.Success)
        {
            // Notification đã được tạo và lưu vào DB
            // Bạn có thể gửi realtime notification qua SignalR
        }
        
        return View();
    }
}
```

#### Cách 2: Gửi thông báo realtime qua SignalR

```csharp
public class MessagesController : Controller
{
    private readonly IHubContext<ChatHub> _hubContext;
    
    [HttpPost]
    public async Task<IActionResult> SendMessage(MessageCreateVM model)
    {
        // ... save message to database ...
        
        // Gửi thông báo tin nhắn mới
        await _hubContext.Clients.User(receiverId.ToString())
            .SendAsync("ReceiveNotification", new {
                title = "Tin nhắn mới",
                message = $"{senderName} đã gửi cho bạn: {messageContent}",
                senderName = senderName,
                senderAvatar = senderAvatar,
                link = "/Messages/Index",
                type = "Message"
            });
        
        return Ok();
    }
}
```

#### Cách 3: Sử dụng ChatHub.NotifyNewMessage (Integrated)

Trong ChatWidget hoặc messaging system, gọi:

```javascript
// Khi gửi tin nhắn
await connection.invoke("NotifyNewMessage", 
    senderId,           // GUID của người gửi
    receiverId,         // GUID của người nhận
    senderName,         // Tên người gửi
    messagePreview      // Preview của tin nhắn (50-100 ký tự)
);
```

Method này sẽ:
1. Tạo notification trong database
2. Gửi realtime notification qua SignalR
3. Cập nhật badge count
4. Hiển thị toast notification

### 3. Client-side: Nhận thông báo realtime

Thông báo được tự động xử lý trong `_Layout.cshtml`. Khi user nhận được thông báo:

1. **Badge count** trên icon bell được cập nhật
2. **Toast notification** hiển thị ở góc phải màn hình
3. **Âm thanh thông báo** phát ra (có thể tắt)
4. **Dropdown menu** được cập nhật

### 4. Tùy chỉnh

#### Tắt âm thanh thông báo
Trong `_Layout.cshtml`, comment dòng:
```javascript
// playNotificationSound();
```

#### Thay đổi thời gian hiển thị toast
Trong `_Layout.cshtml`, tìm:
```javascript
setTimeout(() => {
    removeToast(toast);
}, 5000);  // Đổi 5000 thành số milliseconds khác
```

#### Thay đổi số lượng thông báo trong dropdown
Trong `_NotificationDropdown.cshtml`, tìm:
```javascript
const response = await fetch('/Notification/GetRecent?count=5');
// Đổi count=5 thành số khác
```

## 🎨 UI Components

### Notification Bell Icon
- Vị trí: Navbar, giữa "Tin nhắn" và "Admin Menu"
- Badge: Hiển thị số thông báo chưa đọc
- Dropdown: Click để xem thông báo gần đây

### Notification Dropdown
- Hiển thị 5 thông báo gần nhất
- Avatar người gửi
- Tiêu đề và nội dung
- Time ago
- Link "Xem tất cả"

### Notification Page
- Danh sách đầy đủ thông báo
- Phân biệt đã đọc/chưa đọc (màu nền xanh nhạt)
- Các actions: Đánh dấu đã đọc, Xóa
- Pagination
- Empty state khi không có thông báo

### Toast Notification
- Vị trí: Góc phải màn hình, dưới navbar
- Tự động biến mất sau 5 giây
- Có nút đóng manual
- Responsive, đẹp cả desktop và mobile

## 📊 Database Schema

```sql
CREATE TABLE Notifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    SenderId UNIQUEIDENTIFIER NULL,
    ReceiverId UNIQUEIDENTIFIER NOT NULL,
    Link NVARCHAR(255) NULL,
    IsRead BIT DEFAULT 0,
    Type NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL,
    
    FOREIGN KEY (SenderId) REFERENCES Users(Id) ON DELETE RESTRICT,
    FOREIGN KEY (ReceiverId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Notifications_ReceiverId ON Notifications(ReceiverId);
CREATE INDEX IX_Notifications_SenderId ON Notifications(SenderId);
CREATE INDEX IX_Notifications_IsRead ON Notifications(IsRead);
CREATE INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt);
```

## 🔧 Cấu hình

### Dependency Injection
Đã được tự động đăng ký trong:
- `Models/Repositories/RepositoryRegistration.cs`
- `Models/Services/ServiceRegistration.cs`

### SignalR
- Hub: `/chathub`
- Connection: Tự động kết nối khi user đăng nhập
- Reconnection: Tự động với exponential backoff

## 📝 Best Practices

### 1. Khi tạo thông báo
- Luôn set `ReceiverId`
- `SenderId` có thể null cho system notifications
- `Link` nên là relative path (VD: `/Messages/Index`)
- `Type` nên consistent: "Message", "System", "Match", etc.
- `Message` nên ngắn gọn (< 200 ký tự)

### 2. Xóa thông báo cũ
Nên chạy background job để xóa thông báo cũ hơn 30 ngày:

```csharp
// Trong background job hoặc scheduled task
await notificationRepository.DeleteOldNotificationsAsync(
    DateTime.UtcNow.AddDays(-30)
);
```

### 3. Performance
- Notification list được cache 30 giây
- Badge count được update mỗi 30 giây
- Sử dụng pagination cho danh sách lớn

## 🐛 Troubleshooting

### Không nhận được thông báo realtime?
1. Kiểm tra SignalR connection trong browser console
2. Đảm bảo user đã đăng nhập
3. Check UserId mapping trong `UserIdProvider`
4. Kiểm tra firewall/proxy settings

### Badge count không cập nhật?
1. Check API endpoint `/Notification/GetUnreadCount`
2. Kiểm tra function `updateNotificationCount()` trong console
3. Refresh trang

### Toast không hiển thị?
1. Kiểm tra z-index (phải > navbar)
2. Check console cho errors
3. Đảm bảo Tailwind CSS được load

## 🎯 Future Enhancements

- [ ] Push notifications (Browser API)
- [ ] Email notifications
- [ ] Notification preferences (user settings)
- [ ] Group notifications
- [ ] Rich notifications (images, actions)
- [ ] Notification history export

## 📞 Support

Nếu có vấn đề, kiểm tra:
1. Logs trong `Logs/app-log-{date}.txt`
2. Browser console
3. Network tab (API calls)
4. SignalR connection status

---

**Version**: 1.0  
**Last Updated**: October 28, 2025  
**Author**: WebFindLove Development Team

