# SignalR Real-Time Messaging Implementation

## Tổng quan
Đã triển khai hệ thống nhắn tin real-time sử dụng Microsoft.AspNetCore.SignalR với đầy đủ các tính năng:
- Gửi/nhận tin nhắn tức thời
- Hiển thị trạng thái online/offline
- Typing indicator (đang gõ)
- Hỗ trợ multi-device (nhiều thiết bị cùng lúc)
- Tích hợp tìm kiếm người dùng từ trang chủ

## Các thành phần đã triển khai

### 1. ChatHub (`Core/SignalR/Hubs/ChatHub.cs`)
Hub chính xử lý tất cả các sự kiện real-time:

**Chức năng chính:**
- `SendPrivateMessage()` - Gửi tin nhắn riêng giữa 2 users
- `NotifyTyping()` - Thông báo khi user đang gõ
- `MarkAsRead()` - Đánh dấu tin nhắn đã đọc
- `JoinConversation()` / `LeaveConversation()` - Quản lý conversation rooms
- `GetOnlineUsers()` - Lấy danh sách users đang online
- `CheckUserOnlineStatus()` - Kiểm tra trạng thái online của một user cụ thể

**Tính năng nâng cao:**
- Quản lý connections theo userId (hỗ trợ multi-device)
- Tự động thông báo online/offline khi user connect/disconnect
- ConcurrentDictionary để quản lý state thread-safe

### 2. CustomUserIdProvider (`Core/SignalR/CustomUserIdProvider.cs`)
Provider để map user authentication claims với SignalR connections:
- Lấy userId từ `ClaimTypes.NameIdentifier`
- Fallback sang "sub" claim nếu cần (JWT support)

### 3. Program.cs Updates
Đã cấu hình SignalR trong application pipeline:

```csharp
// SignalR Configuration
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Hub mapping
app.MapHub<ChatHub>("/chatHub");
```

### 4. HomeController Updates
Thêm chức năng tìm kiếm người dùng:

**Tính năng:**
- Tìm kiếm users theo tên, email
- Lọc ra current user khỏi kết quả
- Chỉ hiển thị active users
- Phân trang (PageSize: 20)

### 5. Home/Index.cshtml Updates
Giao diện tìm kiếm và hiển thị kết quả:

**Components:**
- Form tìm kiếm với search input
- Grid layout hiển thị users (responsive: 1/2/3 columns)
- User cards với avatar, bio, location
- Nút "Xem" (Details) và "Nhắn tin" (Start conversation)
- Empty state khi không có kết quả

### 6. MessagesController Updates
Tích hợp SignalR notifications:

**Chức năng:**
- Inject `IHubContext<ChatHub>`
- Gửi SignalR notification khi send message thành công
- Graceful error handling (không fail request nếu SignalR lỗi)
- Log đầy đủ để debug

### 7. Messages/Conversation.cshtml Updates
Giao diện chat real-time hoàn chỉnh:

**UI Features:**
- Online/offline status indicator với dot màu
- Avatar hiển thị (nếu có)
- Typing indicator với animation
- Dark mode support
- Responsive design

**SignalR Client Features:**
- Auto-connect với retry logic
- Receive messages real-time
- Send typing notifications
- Display online/offline status
- Auto-scroll to new messages
- XSS protection với escapeHtml()
- Cleanup on page unload

## Luồng hoạt động

### 1. User Search Flow
```
Trang chủ → Nhập từ khóa tìm kiếm → Submit form
→ HomeController.Index(searchQuery)
→ UserService.GetAllAsync(search)
→ Lọc kết quả, loại bỏ current user
→ Hiển thị grid với user cards
→ Click "Nhắn tin" → Chuyển đến Messages/Conversation
```

### 2. Real-Time Messaging Flow
```
User A gửi tin nhắn:
1. Submit form → MessagesController.Send()
2. Lưu message vào database
3. Gửi SignalR notification qua HubContext
4. SignalR Hub broadcast đến User B
5. User B nhận message real-time qua JavaScript
6. Message append vào chat UI
7. Auto scroll to bottom
```

### 3. Typing Indicator Flow
```
User A gõ tin nhắn:
1. Input event trigger → JavaScript debounce
2. Invoke Hub.NotifyTyping(receiverId, true)
3. Hub gửi "UserTyping" event đến User B
4. User B hiển thị typing indicator
5. Sau 2s không gõ → NotifyTyping(receiverId, false)
6. User B ẩn typing indicator
```

### 4. Online Status Flow
```
User A connect:
1. ChatHub.OnConnectedAsync()
2. Add connection to _userConnections
3. Broadcast "UserOnline" event
4. All other users nhận notification

User A disconnect:
1. ChatHub.OnDisconnectedAsync()
2. Remove connection from _userConnections
3. Nếu là connection cuối → Broadcast "UserOffline"
4. All other users nhận notification
```

## Các events SignalR

### Server → Client Events
- `ReceivePrivateMessage` - Nhận tin nhắn mới
- `MessageSent` - Xác nhận tin nhắn đã gửi (multi-device sync)
- `UserTyping` - User đang gõ tin nhắn
- `UserOnline` - User vừa online
- `UserOffline` - User vừa offline
- `UserOnlineStatus` - Response cho check status request
- `MessagesRead` - Tin nhắn đã được đọc
- `OnlineUsers` - Danh sách users đang online

### Client → Server Methods
- `SendPrivateMessage(receiverId, message, senderName, senderAvatar)`
- `NotifyTyping(receiverId, isTyping)`
- `MarkAsRead(senderId)`
- `JoinConversation(conversationId)`
- `LeaveConversation(conversationId)`
- `GetOnlineUsers()`
- `CheckUserOnlineStatus(userId)`

## Security & Performance

### Security
- ✅ [Authorize] attribute trên MessagesController
- ✅ AntiForgeryToken validation
- ✅ XSS protection với escapeHtml()
- ✅ User validation (không thể message chính mình)
- ✅ SignalR authentication với CustomUserIdProvider

### Performance
- ✅ ConcurrentDictionary cho thread-safe state management
- ✅ Connection pooling (multi-device support)
- ✅ Auto-reconnect với exponential backoff
- ✅ KeepAlive để detect disconnections nhanh
- ✅ Debouncing cho typing indicator

## Testing & Debugging

### Test Scenarios
1. **User search:**
   - Đăng nhập → Tìm kiếm users → Verify kết quả
   - Test search với không có kết quả
   - Test với nhiều users

2. **Real-time messaging:**
   - Mở 2 browsers với 2 accounts khác nhau
   - Gửi message từ browser 1
   - Verify message xuất hiện ngay lập tức ở browser 2

3. **Typing indicator:**
   - Gõ message ở browser 1
   - Verify typing indicator hiện ở browser 2
   - Stop typing → Verify indicator biến mất sau 2s

4. **Online status:**
   - Browser 1 connect → Verify "Online" ở browser 2
   - Browser 1 disconnect → Verify "Offline" ở browser 2

### Debug Tools
- Browser Console logs (SignalR client)
- Server logs (Serilog)
- SignalR browser extension
- Network tab để xem WebSocket connections

## Dependencies
- Microsoft.AspNetCore.SignalR (included in ASP.NET Core 8.0)
- @microsoft/signalr@7.0.0 (CDN)
- Font Awesome icons
- Tailwind CSS

## Các file đã thay đổi

### New Files
- `Core/SignalR/Hubs/ChatHub.cs` - SignalR Hub
- `Core/SignalR/CustomUserIdProvider.cs` - User ID mapping

### Modified Files
- `Program.cs` - SignalR configuration
- `Controllers/HomeController.cs` - Search functionality
- `Controllers/MessagesController.cs` - SignalR integration
- `Views/Home/Index.cshtml` - Search UI
- `Views/Messages/Conversation.cshtml` - Real-time chat UI

## Hướng dẫn sử dụng

### Cho Developer
1. Build project: `dotnet build`
2. Run: `dotnet run`
3. Mở browser, đăng nhập
4. Sử dụng form tìm kiếm ở trang chủ
5. Click "Nhắn tin" để bắt đầu conversation
6. Mở browser thứ 2 với account khác để test real-time

### Cho End User
1. Đăng nhập vào hệ thống
2. Tìm kiếm người dùng từ trang chủ
3. Click "Nhắn tin" để bắt đầu chat
4. Gõ tin nhắn và gửi
5. Tin nhắn sẽ được gửi real-time
6. Xem trạng thái online/typing của người nhận

## Future Enhancements
- [ ] Message reactions (emoji)
- [ ] File/image sharing
- [ ] Voice/video call
- [ ] Group chat
- [ ] Message search
- [ ] Message editing/deletion
- [ ] Read receipts
- [ ] Push notifications
- [ ] Message encryption

## Troubleshooting

### SignalR connection fails
- Check browser console for errors
- Verify `/chatHub` endpoint is accessible
- Check authentication cookies
- Verify firewall/proxy settings

### Messages not appearing real-time
- Check SignalR connection status
- Verify userId in CustomUserIdProvider
- Check server logs for errors
- Test with 2 different browsers

### Typing indicator not working
- Verify NotifyTyping method is called
- Check debounce timeout
- Verify receiver's connection is active

## Kết luận
Hệ thống messaging đã được triển khai đầy đủ với tất cả các tính năng real-time hiện đại. Code được tổ chức tốt, có logging đầy đủ, xử lý errors gracefully, và ready cho production sau khi testing kỹ lưỡng.

