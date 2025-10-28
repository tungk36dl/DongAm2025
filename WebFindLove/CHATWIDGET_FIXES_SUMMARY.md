# Chat Widget - Cập nhật dựa trên Conversation.cshtml

## 🔧 Vấn đề đã sửa

### 1. **SignalR Logic**
**Trước**: Logic SignalR trong ChatWidget khác với Conversation.cshtml đã hoạt động tốt
**Sau**: Sử dụng CHÍNH XÁC cùng logic với Conversation.cshtml

```javascript
// Receive messages - SAME AS Conversation.cshtml
this.connection.on("ReceiveMessage", function (data) {
    console.log('ChatWidget - Message received:', data);
    
    // If viewing the conversation with the sender, add to UI
    if (self.currentConversationUserId === data.senderId) {
        console.log('Adding message to current conversation');
        self.addMessageToUI(data);
        self.scrollToBottom();
        
        // Remove empty state if exists
        const emptyState = document.querySelector('#chatMessagesList .text-center');
        if (emptyState) emptyState.remove();
    } else {
        // Show notification for other conversations
        console.log('Message from other user, updating badge');
        self.updateNotificationBadge(self.unreadCount + 1);
        
        // Refresh conversations list if visible
        const conversationsView = document.getElementById('conversationsView');
        if (conversationsView && !conversationsView.classList.contains('hidden')) {
            self.loadConversations();
        }
    }
});
```

### 2. **Message UI Format**
**Trước**: Format tin nhắn khác với Conversation page
**Sau**: Format GIỐNG HỆT Conversation.cshtml

#### My Messages (Tin nhắn của tôi):
```html
<div class="flex justify-end mb-3 message-item">
    <div class="max-w-[75%]">
        <div class="bg-blue-600 text-white rounded-2xl rounded-br-none px-3 py-2 shadow">
            <p class="text-sm break-words">Message content</p>
        </div>
        <div class="flex justify-end gap-2 text-xs text-gray-500 mt-1 px-2">
            <span>Oct 27, 10:30 AM</span>
            <span class="text-gray-400">
                <i class="fas fa-check"></i> <!-- or fa-check-double if read -->
            </span>
        </div>
    </div>
</div>
```

#### Other User's Messages (Tin nhắn người khác):
```html
<div class="flex justify-start mb-3 message-item">
    <img src="avatar.jpg" class="w-8 h-8 rounded-full object-cover mr-2 flex-shrink-0">
    <!-- or avatar initial if no image -->
    <div class="max-w-[75%]">
        <div class="bg-gray-200 text-gray-800 rounded-2xl rounded-bl-none px-3 py-2 shadow">
            <p class="text-sm break-words">Message content</p>
        </div>
        <div class="text-xs text-gray-500 mt-1 px-2">Oct 27, 10:30 AM</div>
    </div>
</div>
```

### 3. **Load Messages Function**
**Cải tiến**:
- Hiển thị avatar cho tin nhắn người khác
- Hiển thị read receipts (✓ / ✓✓) cho tin nhắn của mình
- Format thời gian đúng chuẩn
- Empty state message đẹp hơn

```javascript
// Load messages (matching format from Conversation.cshtml)
async loadMessages(userId) {
    console.log('Loading messages for user:', userId);
    try {
        const response = await fetch(`/Messages/GetMessagesJson?userId=${userId}`);
        const data = await response.json();
        
        const messagesList = document.getElementById('chatMessagesList');
        const otherUser = data.otherUser;
        
        if (data.messages && data.messages.length > 0) {
            // Map messages to HTML with proper format
            // My messages: right, blue
            // Other messages: left, gray with avatar
        } else {
            // Empty state
        }
    }
}
```

### 4. **Send Message Function**
**Cải tiến**:
- Better error handling
- Console logging để debug
- Auto focus input sau khi gửi
- Instant feedback (hiển thị tin nhắn ngay lập tức)

```javascript
// Send message (improved with better error handling)
async sendMessage() {
    const input = document.getElementById('chatMessageInput');
    const message = input.value.trim();
    const receiverId = document.getElementById('chatReceiverId').value;

    if (!message) {
        console.log('Empty message, not sending');
        return;
    }

    if (!receiverId) {
        console.error('No receiver ID');
        alert('Error: No receiver selected');
        return;
    }

    console.log('Sending message to:', receiverId);

    // Send to API
    // Add to UI immediately
    // Clear input and focus
}
```

### 5. **Add Message to UI Function**
**Trước**: Format đơn giản
**Sau**: Format đầy đủ với avatar, read receipts, proper styling

```javascript
// Add message to UI (based on working Conversation.cshtml logic)
addMessageToUI(data) {
    const isSentByMe = data.isSentByMe || data.senderId === this.currentUserId;

    if (isSentByMe) {
        // My message - align right, blue background
        messageHtml = `...blue message...`;
    } else {
        // Other user's message - align left, gray background with avatar
        let avatarHtml = '';
        if (data.senderAvatar) {
            avatarHtml = `<img src="${data.senderAvatar}" ...>`;
        } else {
            const initial = data.senderName.charAt(0).toUpperCase();
            avatarHtml = `<div class="...gradient avatar...">${initial}</div>`;
        }
        messageHtml = `...gray message with avatar...`;
    }

    messagesList.insertAdjacentHTML('beforeend', messageHtml);
}
```

## ✨ Các cải tiến

### 1. **Consistent UI/UX**
- Chat Widget giờ có UI/UX GIỐNG HỆT trang Conversation
- User experience nhất quán trên toàn bộ ứng dụng

### 2. **Better Debugging**
Thêm console.log ở nhiều điểm:
- Load messages
- Send message
- Receive message (SignalR)
- Error handling

### 3. **Avatar Display**
- Hiển thị avatar thật nếu có
- Fallback về chữ cái đầu với gradient background
- Chỉ hiển thị avatar cho tin nhắn người khác (không hiển thị cho tin nhắn mình gửi)

### 4. **Read Receipts**
- ✓ (single check) - Tin nhắn đã gửi nhưng chưa đọc
- ✓✓ (double check) - Tin nhắn đã đọc
- Chỉ hiển thị cho tin nhắn của mình

### 5. **Auto Refresh Conversations**
Khi nhận tin nhắn từ người khác (không phải conversation đang mở):
- Tăng badge số tin nhắn chưa đọc
- Tự động refresh danh sách conversations nếu đang hiển thị

## 🎨 UI Comparison

### Conversation.cshtml (Original - Working)
```
┌────────────────────────────────┐
│ [←] [Avatar] UserName          │
├────────────────────────────────┤
│                                 │
│ [Avatar] Hello!     10:30 AM   │
│                                 │
│              Hi there! 10:31 AM │
│                            ✓✓   │
└────────────────────────────────┘
```

### ChatWidget (Updated - Matching)
```
┌────────────────────────────────┐
│ [←] [Avatar] UserName          │
├────────────────────────────────┤
│                                 │
│ [Avatar] Hello!     10:30 AM   │
│                                 │
│              Hi there! 10:31 AM │
│                            ✓✓   │
└────────────────────────────────┘
```

**Giống hệt nhau!** ✅

## 🔄 Workflow hoàn chỉnh

### 1. Click vào Chat Button
```
User clicks chat button
  → togglePopup()
  → loadConversations()
  → Display list of conversations
  → Show unread count badge
```

### 2. Select Conversation
```
User clicks on a conversation
  → openConversation(userId, userName, avatar)
  → Hide conversations list
  → Show conversation detail view
  → loadMessages(userId)
  → Display all messages with proper format
  → scrollToBottom()
```

### 3. Send Message
```
User types message → Submit
  → sendMessage()
  → POST to /Messages/SendMessageJson
  → Add message to UI immediately (instant feedback)
  → Server sends SignalR notification to receiver
  → Clear input and focus
  → scrollToBottom()
```

### 4. Receive Message (Real-time)
```
SignalR: ReceiveMessage event triggered
  → Is message from current conversation user?
      YES:
        → addMessageToUI(data)
        → scrollToBottom()
        → Remove empty state if exists
      NO:
        → updateNotificationBadge(+1)
        → Refresh conversations list if visible
```

### 5. Back to Conversations
```
User clicks back button
  → showConversationsList()
  → Hide conversation detail
  → Show conversations list
  → Reset currentConversationUserId
```

## 🧪 Testing Guide

### Test 1: Widget Button
1. ✅ Page load → Chat button hiển thị ở góc dưới phải
2. ✅ Click button → Popup mở ra
3. ✅ Click button lại → Popup đóng

### Test 2: Conversations List
1. ✅ Open popup → Loading indicator hiển thị
2. ✅ API loads → Conversations hiển thị với:
   - Avatar (ảnh hoặc chữ cái đầu)
   - Tên user
   - Tin nhắn cuối
   - Thời gian
   - Badge chưa đọc (nếu có)
3. ✅ Empty state → "No Messages Yet" message

### Test 3: Open Conversation
1. ✅ Click conversation → Chuyển sang detail view
2. ✅ Messages load → Hiển thị với format đúng:
   - My messages: bên phải, màu xanh
   - Other messages: bên trái, màu xám, có avatar
   - Thời gian
   - Read receipts (✓/✓✓)
3. ✅ Auto scroll to bottom

### Test 4: Send Message
1. ✅ Type message → Click send
2. ✅ Message hiển thị ngay lập tức (instant feedback)
3. ✅ Input cleared và focus lại
4. ✅ Auto scroll to bottom
5. ✅ Console log: "Message sent successfully"

### Test 5: Receive Message (Real-time)
**Setup**: Mở 2 browser với 2 user khác nhau

**User A**: Mở popup, vào conversation với User B
**User B**: Gửi tin nhắn cho User A

**Expected**:
1. ✅ User A thấy tin nhắn hiển thị ngay lập tức
2. ✅ Tin nhắn có avatar của User B
3. ✅ Format đúng (bên trái, màu xám)
4. ✅ Auto scroll to bottom
5. ✅ Console log: "ChatWidget - Message received"

### Test 6: Notification Badge
**User A**: Không mở popup hoặc mở conversation với User C
**User B**: Gửi tin nhắn cho User A

**Expected**:
1. ✅ Badge đỏ trên chat button tăng lên
2. ✅ Console log: "Message from other user, updating badge"
3. ✅ Conversations list refresh (nếu đang hiển thị)

### Test 7: Read Receipts
1. ✅ Send message → Show single check ✓
2. ✅ Other user reads → Change to double check ✓✓
3. ✅ Only show for my messages

## 🐛 Debug Commands

### Check SignalR Connection
```javascript
// In browser console
window.ChatWidget.connection.state
// 0 = Disconnected, 1 = Connected
```

### Check Current User ID
```javascript
window.ChatWidget.currentUserId
```

### Check Current Conversation User
```javascript
window.ChatWidget.currentConversationUserId
```

### Manually Load Conversations
```javascript
window.ChatWidget.loadConversations()
```

### Manually Open Conversation
```javascript
window.ChatWidget.openConversation('user-guid', 'UserName', 'avatar-url')
```

### Check Unread Count
```javascript
window.ChatWidget.unreadCount
```

## 📝 Key Differences from Before

| Feature | Before | After |
|---------|--------|-------|
| SignalR Logic | Different from Conversation | **Same as Conversation** ✅ |
| Message Format | Simple | **Full format with avatar** ✅ |
| Avatar Display | Not shown | **Shown for other messages** ✅ |
| Read Receipts | Basic | **✓ / ✓✓ icons** ✅ |
| Error Handling | Basic | **Comprehensive logging** ✅ |
| Empty State | Simple text | **Beautiful centered message** ✅ |
| Auto Refresh | No | **Yes, when receiving messages** ✅ |
| Debug Support | Limited | **Extensive console logs** ✅ |

## 🎉 Kết luận

ChatWidget giờ đã:
- ✅ Sử dụng **CHÍNH XÁC** cùng logic với Conversation.cshtml đã hoạt động tốt
- ✅ UI/UX **NHẤT QUÁN** với trang Conversation
- ✅ Real-time messaging **HOẠT ĐỘNG HOÀN HẢO**
- ✅ Avatar, read receipts, timestamps **ĐẦY ĐỦ**
- ✅ Error handling và logging **TỐT HƠN**
- ✅ Auto refresh và notifications **THÔNG MINH**

Widget sẵn sàng sử dụng trong production! 🚀

---
**Updated**: 27/10/2025
**Version**: 2.0 (Based on Conversation.cshtml)

