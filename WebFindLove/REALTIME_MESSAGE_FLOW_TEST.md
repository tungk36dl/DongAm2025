# 🔄 Luồng Gửi Tin Nhắn Real-Time - Chi tiết & Test Guide

## 📋 Tổng quan kiểm tra

Tài liệu này mô tả chi tiết luồng gửi tin nhắn real-time và cách test để đảm bảo mọi thứ hoạt động đúng.

## 🎯 Luồng hoàn chỉnh (Step-by-step)

### **Bước 1: User A kết nối SignalR**
```
Browser A → /chatHub WebSocket connection
↓
ChatHub.OnConnectedAsync() triggered
↓
1. Get userId from Context.UserIdentifier (via CustomUserIdProvider)
2. Add connectionId to _userConnections[userId]
3. Log: "User connected - UserId: {userId}, ConnectionId: {connectionId}"
4. Broadcast to ALL other clients: "UserOnline" event
↓
Browser B receives "UserOnline" event
↓
Update UI: Green dot + "Online" text
```

**Test:**
- ✅ Mở browser A, đăng nhập
- ✅ Mở browser B với user khác, đăng nhập
- ✅ Check console log: "User connected"
- ✅ Verify green dot xuất hiện ở browser B

---

### **Bước 2: User A gõ tin nhắn (Typing Indicator)**
```
Browser A → User types in input field
↓
JavaScript: messageInput.addEventListener('input')
↓
connection.invoke("NotifyTyping", receiverId, true)
↓
ChatHub.NotifyTyping(receiverId, isTyping)
↓
1. Get senderId from Context
2. Store in _typingStatus[senderId_receiverId]
3. Find receiver's connections
4. Send "UserTyping" event to ALL receiver's connections
↓
Browser B receives "UserTyping" event
↓
Show typing indicator animation
↓
After 2 seconds no typing:
connection.invoke("NotifyTyping", receiverId, false)
↓
Hide typing indicator
```

**Test:**
- ✅ User A gõ trong message input
- ✅ Check console: "NotifyTyping invoked"
- ✅ Verify typing indicator xuất hiện ở browser B
- ✅ Stop typing → verify indicator biến mất sau 2s

---

### **Bước 3: User A gửi tin nhắn**

#### **3.1 Form Submit (Traditional POST)**
```
Browser A → Form submit
↓
POST /Messages/Send
{
  receiverId: "guid",
  content: "Hello!",
  __RequestVerificationToken: "..."
}
↓
MessagesController.Send() called
```

#### **3.2 Server-side Processing**
```
MessagesController.Send(receiverId, content)
↓
Step 1: Validate
  - Check content not empty
  - Check authentication
↓
Step 2: Save to Database
  var response = await _messageService.SendMessageAsync(UserId, receiverId, content);
  ↓
  MessageService creates Message entity
  ↓
  UnitOfWork saves to SQL Server
  ↓
  Returns DataResponse with messageId
↓
Step 3: Send SignalR Notification
  try {
    // Get sender's full info (including avatar)
    var senderInfo = await _userService.GetByIdAsync(UserId);
    
    // Send via HubContext
    await _hubContext.Clients
        .User(receiverId.ToString())
        .SendAsync("ReceivePrivateMessage", new {
            senderId = UserId.ToString(),
            senderName = senderInfo.Data?.UserName,
            senderAvatar = senderInfo.Data?.Avatar,
            message = content,
            timestamp = DateTime.UtcNow,
            messageId = response.Data?.Id
        });
    
    Log: "SignalR notification sent to user: {receiverId}"
  }
  catch (Exception ex) {
    Log: "Failed to send SignalR notification"
    // Don't fail - graceful degradation
  }
↓
Step 4: Redirect
  return RedirectToAction("Conversation", new { userId = receiverId });
```

#### **3.3 SignalR Hub Processing**
```
_hubContext.Clients.User(receiverId)
↓
SignalR Hub finds ALL connections for receiverId
  (supports multi-device: web + mobile + tablet)
↓
For each connection:
  Send "ReceivePrivateMessage" event with message data
```

#### **3.4 Client-side Real-time Update**
```
Browser B → SignalR client receives event
↓
connection.on("ReceivePrivateMessage", function(data) {
  console.log('Received message:', data);
  
  // Verify sender
  if (data.senderId === otherUserId) {
    
    // Append to UI
    appendMessage(
      data.message,
      false, // not sent by me
      data.timestamp,
      data.senderName,
      data.senderAvatar
    );
    
    // Scroll to bottom
    scrollToBottom();
    
    // Remove empty state if exists
    document.getElementById('emptyState')?.remove();
  }
});
```

#### **3.5 UI Rendering**
```
appendMessage() function
↓
1. Build HTML with:
   - Avatar (image or initials)
   - Message bubble
   - Timestamp
   - Alignment (left for received)
↓
2. Escape HTML to prevent XSS
↓
3. Insert into DOM:
   messagesList.insertAdjacentHTML('beforeend', messageHtml)
↓
4. Auto-scroll to bottom
```

**Full Test:**
- ✅ User A gửi message "Hello World"
- ✅ Check server log: "Message sent successfully"
- ✅ Check server log: "SignalR notification sent"
- ✅ Check browser B console: "Received message: Hello World"
- ✅ Verify message xuất hiện ngay ở browser B
- ✅ Verify avatar hiển thị đúng
- ✅ Verify timestamp đúng
- ✅ Verify auto-scroll to bottom

---

### **Bước 4: User B đọc tin nhắn (Optional - Mark as Read)**
```
Browser B → User views message
↓
connection.invoke("MarkAsRead", senderId)
↓
ChatHub.MarkAsRead(senderId)
↓
Send "MessagesRead" event to sender's connections
↓
Browser A receives "MessagesRead"
↓
Update UI: Single check → Double check (blue)
```

---

### **Bước 5: User A disconnect**
```
Browser A → Window closed / Network lost
↓
ChatHub.OnDisconnectedAsync() triggered
↓
1. Get userId and connectionId
2. Remove connectionId from _userConnections[userId]
3. If no more connections:
   - Remove userId from _userConnections
   - Broadcast "UserOffline" to all others
   - Log: "User disconnected and went offline"
4. Else:
   - Log: "User still has X active connections"
↓
Browser B receives "UserOffline"
↓
Update UI: Green dot → Gray dot + "Offline"
```

**Test:**
- ✅ Close browser A
- ✅ Check server log: "User disconnected"
- ✅ Verify gray dot ở browser B
- ✅ Verify status text: "Offline"

---

## 🔍 Debug Checklist

### **Server-side Logs (Serilog)**
```bash
# Check SignalR connection
[INFO] User connected - UserId: {guid}, ConnectionId: {connId}

# Check message send
[INFO] POST Send Message - From: {senderId}, To: {receiverId}
[INFO] Message sent successfully: {messageId}
[DEBUG] SignalR notification sent to user: {receiverId}

# Check typing
[DEBUG] NotifyTyping - From: {senderId}, To: {receiverId}, IsTyping: {bool}

# Check disconnect
[INFO] User disconnected and went offline - UserId: {guid}
```

### **Browser Console Logs (JavaScript)**
```javascript
// Connection
"SignalR Connected"

// Receive message
"Received message: { senderId, message, timestamp, ... }"

// Typing
"NotifyTyping invoked"

// Online status
"User online: { userId, timestamp }"
"User offline: { userId, timestamp }"

// Errors
"SignalR Connection Error: ..."
"Error checking online status: ..."
```

### **Network Tab (Browser DevTools)**
```
1. WebSocket connection to /chatHub
   Status: 101 Switching Protocols
   
2. SignalR negotiation:
   POST /chatHub/negotiate
   Response: { connectionId, availableTransports }
   
3. WebSocket frames:
   ⬆️ Send: {"type":1,"target":"NotifyTyping","arguments":[...]}
   ⬇️ Receive: {"type":1,"target":"ReceivePrivateMessage","arguments":[...]}
```

---

## ✅ Test Scenarios

### **Scenario 1: Basic Real-Time Messaging**
```
Setup:
- Browser A: User Alice (user1@test.com)
- Browser B: User Bob (user2@test.com)

Steps:
1. Alice đăng nhập → Check online status
2. Bob đăng nhập → Check Alice sees Bob online
3. Alice gõ message → Check Bob sees typing indicator
4. Alice gửi "Hello Bob" → Check Bob receives instantly
5. Bob reply "Hi Alice" → Check Alice receives instantly
6. Close Bob's browser → Check Alice sees Bob offline

Expected:
✅ All messages appear instantly (< 1 second)
✅ Typing indicators work both ways
✅ Online/offline status accurate
✅ Avatars display correctly
✅ No console errors
```

### **Scenario 2: Multi-Device Support**
```
Setup:
- Browser A: Alice on laptop
- Browser B: Alice on phone (same account)
- Browser C: Bob

Steps:
1. Alice opens both browsers → Check 2 connections in server log
2. Bob sends message → Check message appears on BOTH Alice's devices
3. Close Browser A → Check Browser B still works
4. Bob checks Alice's status → Should still show "Online"
5. Close Browser B → Now Alice should be "Offline"

Expected:
✅ Messages sync across devices
✅ Online until ALL devices disconnect
✅ Each device has independent connection
```

### **Scenario 3: Network Interruption**
```
Setup:
- Browser A: Alice
- Browser B: Bob

Steps:
1. Alice and Bob connected
2. Disable Alice's network for 5 seconds
3. Enable network → Check auto-reconnect
4. Bob sends message → Check Alice receives after reconnect

Expected:
✅ SignalR auto-reconnects
✅ Status shows "Reconnecting..." then "Online"
✅ Messages still work after reconnect
✅ No data loss
```

### **Scenario 4: Concurrent Messaging**
```
Setup:
- Browser A: Alice
- Browser B: Bob

Steps:
1. Alice and Bob both typing at same time
2. Both send messages rapidly (5 messages each)
3. Check both sides receive all messages in order

Expected:
✅ All 10 messages delivered
✅ Correct order maintained
✅ No duplicates
✅ No missing messages
```

### **Scenario 5: Error Handling**
```
Setup:
- Browser A: Alice
- Stop SignalR hub on server (simulate server crash)

Steps:
1. Alice tries to send message
2. Check message still saves to database
3. Check graceful error handling
4. Start SignalR hub again
5. Check reconnection

Expected:
✅ Message saves even if SignalR fails
✅ No UI freeze/crash
✅ Error logged but user can continue
✅ Auto-reconnect when server back
```

---

## 🐛 Common Issues & Solutions

### **Issue 1: Messages not appearing real-time**
```
Symptoms:
- Message saves but doesn't appear on receiver's screen
- Need to refresh to see messages

Debug:
1. Check browser console for SignalR errors
2. Check server log for "SignalR notification sent"
3. Verify CustomUserIdProvider returns correct userId
4. Check WebSocket connection in Network tab

Solutions:
- Verify userId in claims matches database
- Check firewall/proxy allowing WebSocket
- Verify receiver is actually connected
- Check _hubContext injection in controller
```

### **Issue 2: Connection keeps dropping**
```
Symptoms:
- "Reconnecting..." appears frequently
- Messages delayed or lost

Debug:
1. Check KeepAliveInterval and ClientTimeoutInterval in Program.cs
2. Check server resources (CPU, memory)
3. Check network stability

Solutions:
- Increase timeout: ClientTimeoutInterval = TimeSpan.FromSeconds(60)
- Check for aggressive proxy/load balancer
- Verify server not overloaded
```

### **Issue 3: Typing indicator stuck**
```
Symptoms:
- "is typing..." never disappears
- Appears when user not actually typing

Debug:
1. Check JavaScript timeout logic (2 seconds)
2. Check NotifyTyping(false) being called

Solutions:
- Verify clearTimeout() in JavaScript
- Add cleanup on message send
- Check for JavaScript errors preventing timeout
```

### **Issue 4: Online status incorrect**
```
Symptoms:
- Shows "Offline" when user is online
- Shows "Online" when user is offline

Debug:
1. Check OnConnectedAsync/OnDisconnectedAsync logs
2. Verify _userConnections dictionary
3. Check CheckUserOnlineStatus method

Solutions:
- Ensure CustomUserIdProvider returns consistent userId
- Check for race conditions in connection management
- Verify multi-device logic (only offline when ALL connections gone)
```

### **Issue 5: Avatar not showing**
```
Symptoms:
- Avatar shows blank or default icon
- Works for some users but not others

Debug:
1. Check senderInfo.Data?.Avatar in controller
2. Verify avatar URL is correct and accessible
3. Check browser console for 404 errors

Solutions:
- Ensure avatar uploaded and saved correctly
- Check file path/URL format
- Verify image permissions
- Add fallback to initials (already implemented)
```

---

## 🔧 Performance Optimization

### **Current Implementation**
```csharp
// Already optimized:
✅ ConcurrentDictionary for thread-safety
✅ Multi-device support (don't duplicate queries)
✅ Graceful error handling (doesn't fail request if SignalR fails)
✅ Connection pooling
✅ Auto-reconnect with exponential backoff
```

### **Future Improvements**
```csharp
// Consider for scale:
- Redis backplane for multi-server scenarios
- Message batching for high-frequency senders
- Rate limiting per user
- Message queue for offline users
- WebSocket compression
```

---

## 📊 Monitoring Metrics

### **Key Metrics to Track**
```
1. Connection metrics:
   - Active connections count
   - Connection duration
   - Reconnection rate
   - Failed connections

2. Message metrics:
   - Messages sent/received per second
   - Message delivery latency
   - Failed message notifications
   - Message size distribution

3. Performance metrics:
   - Hub method execution time
   - Database query time
   - SignalR broadcast time
   - Memory usage of _userConnections
```

---

## ✨ Kết luận

Luồng gửi tin nhắn real-time đã được kiểm tra và tối ưu với:

✅ **Reliability**: Graceful error handling, auto-reconnect
✅ **Performance**: Thread-safe, connection pooling, minimal latency
✅ **Security**: XSS protection, authentication required
✅ **User Experience**: Typing indicator, online status, avatars
✅ **Scalability**: Multi-device support, concurrent messaging

Test theo các scenarios trên để đảm bảo mọi thứ hoạt động đúng trong môi trường của bạn!

