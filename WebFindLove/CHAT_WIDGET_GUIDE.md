# Chat Widget Popup - Hướng dẫn sử dụng

## Tổng quan

Chat Widget là một popup tin nhắn thời gian thực được hiển thị ở góc dưới bên phải màn hình. Widget này cho phép người dùng xem và gửi tin nhắn mà không cần rời khỏi trang hiện tại.

## Tính năng

### 1. **Floating Chat Button**
- Nút chat tròn nổi ở góc dưới bên phải màn hình
- Icon comment với gradient màu xanh-tím
- Badge thông báo đỏ hiển thị số tin nhắn chưa đọc
- Hiệu ứng hover và scale khi di chuột

### 2. **Chat Popup**
- Kích thước: 384px (width) x 600px (height)
- Vị trí: Góc dưới bên phải (bottom-6 right-6)
- Thiết kế hiện đại với shadow và border

### 3. **Header Bar**
- Gradient màu xanh-tím
- Hiển thị tên "Messages" và số tin nhắn chưa đọc
- Các nút:
  - **Refresh**: Làm mới danh sách cuộc trò chuyện
  - **Close**: Đóng popup

### 4. **Conversations List View**
- Hiển thị danh sách tất cả cuộc trò chuyện
- Mỗi conversation bao gồm:
  - Avatar người dùng (ảnh hoặc chữ cái đầu)
  - Tên người dùng
  - Tin nhắn cuối cùng
  - Thời gian
  - Badge số tin nhắn chưa đọc (nếu có)
- Cuộc trò chuyện chưa đọc có background màu xanh nhạt
- Click vào conversation để mở chi tiết

### 5. **Conversation Detail View**
- **Header**:
  - Nút Back để quay lại danh sách
  - Avatar và tên người dùng
  - Trạng thái online/offline
- **Messages Container**:
  - Hiển thị tất cả tin nhắn
  - Tin nhắn của bạn: Màu xanh, căn phải
  - Tin nhắn của người khác: Màu xám, căn trái
  - Hiển thị thời gian và trạng thái đã đọc (✓ hoặc ✓✓)
  - Typing indicator khi người khác đang gõ
- **Message Input**:
  - Ô nhập tin nhắn
  - Nút gửi với icon máy bay giấy

### 6. **Real-time Features (SignalR)**
- Nhận tin nhắn mới ngay lập tức
- Cập nhật trạng thái đã đọc real-time
- Hiển thị typing indicator
- Tự động cập nhật số tin nhắn chưa đọc

## Cấu trúc File

### 1. **View Components**
```
WebFindLove/Views/Shared/
  └── _ChatWidget.cshtml          # Widget chính
  └── _Layout.cshtml               # Layout đã include widget
```

### 2. **Controller & API Endpoints**
```
WebFindLove/Controllers/
  └── MessagesController.cs
```

**API Endpoints:**
- `GET /Messages/GetConversationsJson` - Lấy danh sách conversations
- `GET /Messages/GetMessagesJson?userId={guid}` - Lấy tin nhắn của một conversation
- `POST /Messages/SendMessageJson` - Gửi tin nhắn mới
- `GET /Messages/GetUnreadCount` - Lấy số tin nhắn chưa đọc

### 3. **SignalR Hub**
```
WebFindLove/Hubs/
  └── ChatHub.cs                   # Hub xử lý real-time messaging
```

## Cách hoạt động

### 1. **Khởi tạo**
```javascript
// Widget tự động khởi tạo khi trang load
document.addEventListener('DOMContentLoaded', function() {
    ChatWidget.init();
});
```

### 2. **Load Conversations**
```javascript
// Gọi API để lấy danh sách conversations
fetch('/Messages/GetConversationsJson')
  .then(response => response.json())
  .then(data => {
      // Hiển thị conversations
      // Cập nhật unread count
  });
```

### 3. **Open Conversation**
```javascript
// Khi click vào một conversation
ChatWidget.openConversation(userId, userName, userAvatar);
// - Chuyển sang conversation detail view
// - Load messages
// - Cập nhật UI
```

### 4. **Send Message**
```javascript
// Gửi tin nhắn qua API
fetch('/Messages/SendMessageJson', {
    method: 'POST',
    body: JSON.stringify({
        receiverId: userId,
        content: message
    })
});
// - Thêm message vào UI ngay lập tức
// - Server gửi real-time notification qua SignalR
```

### 5. **Receive Real-time Message**
```javascript
// SignalR connection nhận tin nhắn mới
connection.on("ReceiveMessage", function(data) {
    // Nếu đang xem conversation với người gửi
    if (currentConversationUserId === data.senderId) {
        // Thêm message vào UI
        addMessageToUI(data);
    } else {
        // Tăng unread count badge
        updateNotificationBadge(unreadCount + 1);
    }
});
```

## Responsive Design

### Desktop (>= 1024px)
- Widget hiển thị đầy đủ tính năng
- Popup width: 384px (24rem)
- Height: 600px

### Mobile (< 1024px)
- Widget button vẫn hiển thị
- Popup tự động điều chỉnh kích thước
- Khuyến nghị người dùng vào trang Messages đầy đủ

## Styling

### Colors
- **Primary Gradient**: `from-blue-600 to-purple-600`
- **Sent Message**: `bg-blue-600 text-white`
- **Received Message**: `bg-gray-200 text-gray-800`
- **Unread Badge**: `bg-red-500 text-white`
- **Hover**: `hover:bg-gray-50`

### Animations
- **Button hover**: `transform hover:scale-110`
- **Typing indicator**: `animate-bounce` với delay khác nhau
- **Transitions**: `transition-all duration-300`

## Tùy chỉnh

### 1. **Thay đổi vị trí**
```css
/* Trong _ChatWidget.cshtml, tìm: */
<div id="chatWidget" class="fixed bottom-6 right-6 z-50">

/* Đổi thành góc trái: */
<div id="chatWidget" class="fixed bottom-6 left-6 z-50">
```

### 2. **Thay đổi kích thước**
```css
/* Trong _ChatWidget.cshtml, tìm: */
<div ... class="... w-96 ..." style="height: 600px;">

/* Đổi thành: */
<div ... class="... w-[500px] ..." style="height: 700px;">
```

### 3. **Thay đổi màu sắc**
```css
/* Gradient button */
class="bg-gradient-to-br from-blue-600 to-purple-600"

/* Đổi thành: */
class="bg-gradient-to-br from-pink-600 to-red-600"
```

## Xử lý lỗi

### 1. **Widget không hiển thị**
- Kiểm tra user đã đăng nhập chưa
- Widget chỉ hiển thị khi `User.Identity.IsAuthenticated == true`

### 2. **Không load được conversations**
- Kiểm tra API endpoint `/Messages/GetConversationsJson`
- Xem console log để biết chi tiết lỗi
- Kiểm tra IConversationService và IMessageService

### 3. **Không gửi được tin nhắn**
- Kiểm tra CSRF token
- Kiểm tra API endpoint `/Messages/SendMessageJson`
- Kiểm tra SignalR connection

### 4. **Không nhận được tin nhắn real-time**
- Kiểm tra SignalR connection: `connection.state === signalR.HubConnectionState.Connected`
- Kiểm tra ChatHub configuration trong `Program.cs`
- Kiểm tra UserIdProvider

## Debug

### 1. **Enable Console Logging**
```javascript
// Widget đã có sẵn console.log
// Mở Developer Tools (F12) để xem logs
```

### 2. **Kiểm tra SignalR Connection**
```javascript
// Trong console
ChatWidget.connection.state
// 0 = Disconnected, 1 = Connected
```

### 3. **Kiểm tra API Response**
```javascript
// Test API trong console
fetch('/Messages/GetConversationsJson')
  .then(r => r.json())
  .then(d => console.log(d));
```

## Tích hợp với các trang khác

Widget đã được tích hợp sẵn trong `_Layout.cshtml`, do đó sẽ tự động hiển thị trên tất cả các trang sử dụng layout này.

### Ẩn widget trên một trang cụ thể

Nếu muốn ẩn widget trên một trang cụ thể (ví dụ: trang Messages đầy đủ):

```cshtml
@{
    ViewData["HideChatWidget"] = true;
}
```

Sau đó trong `_ChatWidget.cshtml`, thêm điều kiện:
```cshtml
@if (ViewData["HideChatWidget"] as bool? != true)
{
    <!-- Widget content -->
}
```

## Performance

### 1. **Lazy Loading**
- Widget chỉ load conversations khi người dùng click vào button
- Messages chỉ load khi người dùng mở conversation

### 2. **Caching**
- Unread count được cache trong badge
- Conversations được cache cho đến khi refresh

### 3. **SignalR Optimization**
- Sử dụng automatic reconnect
- Chỉ gửi/nhận messages cho conversation đang active

## Testing

### 1. **Test Conversations List**
- Click vào chat button
- Kiểm tra danh sách conversations hiển thị
- Kiểm tra unread count badge
- Kiểm tra avatar và thông tin user

### 2. **Test Messaging**
- Mở một conversation
- Gửi tin nhắn
- Kiểm tra tin nhắn hiển thị ngay lập tức
- Kiểm tra scroll to bottom tự động

### 3. **Test Real-time**
- Mở 2 browser với 2 user khác nhau
- Gửi tin nhắn từ user A
- Kiểm tra user B nhận được tin nhắn real-time
- Kiểm tra unread count cập nhật

### 4. **Test Responsive**
- Test trên desktop
- Test trên tablet
- Test trên mobile
- Kiểm tra popup không bị overflow

## Kết luận

Chat Widget Popup là một giải pháp chat real-time hoàn chỉnh với:
- ✅ UI/UX hiện đại và thân thiện
- ✅ Real-time messaging với SignalR
- ✅ Notification badges
- ✅ Responsive design
- ✅ Easy to customize
- ✅ Good performance

Widget sẵn sàng sử dụng và có thể tùy chỉnh theo nhu cầu của dự án.

