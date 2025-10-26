# 🐛 SignalR Debug Guide - Tin nhắn không hiện real-time

## ⚠️ Vấn đề: Tin nhắn chỉ hiện khi reload

### Đã thêm Debug Logging

Giờ bạn sẽ thấy rất nhiều logs để debug. Hãy làm theo:

---

## 📝 Bước 1: Kiểm tra Server Logs

### Khởi động app:
```bash
cd WebFindLove
dotnet run
```

### Quan sát logs khi User kết nối:

**Khi User A mở trang Conversation, bạn sẽ thấy:**
```
[INFO] 🔌 NEW CONNECTION - ConnectionId: xxxxx
[INFO] 👤 User Identifier: {guid của User A}
[INFO] 🔐 Is Authenticated: True
[INFO] 📛 Username: admin@example.com
[INFO] ✅ User connected - UserId: {guid}, ConnectionId: xxxxx, TotalConnections: 1
[INFO] 📊 Current tracked users: 1
```

**❌ Nếu thấy:**
```
[INFO] 👤 User Identifier: {connection-id thay vì guid}
[INFO] 🔐 Is Authenticated: False
```
→ **PROBLEM: SignalR không authenticate được!**

---

## 📝 Bước 2: Kiểm tra Browser Console

### Mở Browser Console (F12):

**Khi trang load, bạn sẽ thấy:**
```
✅ SignalR Connected Successfully!
Connection ID: xxxxxxxx
Connection State: Connected
Current User ID: {guid của user hiện tại}
Other User ID: {guid của user đang chat}
```

**❌ Nếu thấy lỗi:**
```
❌ SignalR Connection Error: ...
Error details: ...
```
→ Copy error message để debug

---

## 📝 Bước 3: Test Gửi Message

### User A gửi message cho User B:

**Server logs sẽ show:**
```
[INFO] POST Send Message - From: {User A ID}, To: {User B ID}
[INFO] Message sent successfully: {message-id}
[INFO] 🔄 Attempting to send SignalR notification...
[INFO] 📤 Sending to User: {User B ID}
[INFO] 📦 Message data: SenderId={User A ID}, Message=Hello
[INFO] ✅ SignalR notification sent successfully to user: {User B ID}
```

**Browser B console sẽ show:**
```
✅ Received message: { senderId: "...", message: "Hello", ... }
Current otherUserId: {User A ID}
Data senderId: {User A ID}
Match? true
✅ Displaying message in UI
```

---

## 🔍 Các Scenarios & Solutions

### **Scenario 1: Authentication Failed**

**Symptoms:**
```
Server: Is Authenticated: False
Server: User Identifier: {connection-id}
```

**Root Cause:** Cookie authentication không work với WebSocket

**Solution:** Đã cấu hình fallback to LongPolling:
```javascript
.withUrl("/chatHub", {
    transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
})
```

**If still fails, thử thêm vào Program.cs:**
```csharp
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax
});
```

---

### **Scenario 2: Message Received but Not Displayed**

**Symptoms:**
```
Browser: ✅ Received message: {...}
Browser: ⚠️ Message sender does not match current conversation
```

**Root Cause:** senderId không match otherUserId (type mismatch)

**Solution:** Đã fix comparison:
```javascript
if (data.senderId.toString() === otherUserId.toString())
```

**Check trong console:**
```
Current otherUserId: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Data senderId: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Match? true/false
```

---

### **Scenario 3: SignalR Sends but No User Connected**

**Symptoms:**
```
Server: ✅ SignalR notification sent successfully
Browser: (không nhận được gì)
```

**Root Cause:** User B không có active SignalR connection

**Debug:**
```
Server logs should show:
📊 Current tracked users: 0  ← PROBLEM!

Should be:
📊 Current tracked users: 2  ← User A & B
```

**Solution:** Đảm bảo cả 2 users đều mở Conversation page và SignalR connected

---

### **Scenario 4: Wrong User ID Format**

**Symptoms:**
```
Server: Sending to User: {guid}
Browser: Message for different user
```

**Debug trong ChatHub logs:**
```
[INFO] ✅ User connected - UserId: {xxx}
```

So sánh với Controller:
```
[INFO] 📤 Sending to User: {yyy}
```

Nếu `xxx !== yyy` → **PROBLEM: User ID mismatch!**

**Solution:** Check CustomUserIdProvider returns correct claim

---

## 🧪 Test Step-by-Step

### **Test 1: Basic Connection**

1. User A đăng nhập → Vào Conversation
2. Check server log:
   ```
   ✅ User connected - UserId: {guid-A}
   Is Authenticated: True
   ```
3. Check browser console:
   ```
   ✅ SignalR Connected Successfully!
   ```

**✅ PASS:** Logs như trên
**❌ FAIL:** Authentication False → See Scenario 1

---

### **Test 2: Two Users Connected**

1. User A connected (đã test ở Test 1)
2. User B đăng nhập → Vào Conversation với A
3. Check server log:
   ```
   📊 Current tracked users: 2
   ```
4. Check cả 2 browsers console:
   ```
   ✅ SignalR Connected Successfully!
   ```

**✅ PASS:** 2 users connected
**❌ FAIL:** < 2 users → Connection issue

---

### **Test 3: Send & Receive**

1. User A và B đã connected
2. User A gửi message "Test 123"
3. Check server log (User A request):
   ```
   [INFO] POST Send Message - From: {A}, To: {B}
   [INFO] Message sent successfully
   [INFO] 🔄 Attempting to send SignalR notification...
   [INFO] 📤 Sending to User: {B}
   [INFO] ✅ SignalR notification sent successfully
   ```
4. Check User B console:
   ```
   ✅ Received message: {...message: "Test 123"...}
   Match? true
   ✅ Displaying message in UI
   ```
5. Check User B UI:
   Message "Test 123" hiện lên NGAY

**✅ PASS:** Message hiện ngay < 1 giây
**❌ FAIL:** Không hiện → Check which step failed

---

## 🔧 Quick Fixes

### Fix 1: Clear Cookies & Restart
```bash
# Browser: Clear all cookies for localhost
# Then refresh
```

### Fix 2: Restart App
```bash
# Stop app (Ctrl+C)
dotnet clean
dotnet build
dotnet run
```

### Fix 3: Check Cookie Settings
```csharp
// In Program.cs, add before app.UseAuthentication():
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.SameAsRequest
});
```

### Fix 4: Force LongPolling (if WebSocket fails)
```javascript
// In Conversation.cshtml, change to:
.withUrl("/chatHub", {
    skipNegotiation: false,
    transport: signalR.HttpTransportType.LongPolling  // Only LongPolling
})
```

---

## 📊 Expected Full Flow Logs

### When User A sends "Hello" to User B:

**User A Browser:**
```
(Form submits normally - no special logs needed)
```

**Server:**
```
[INFO] POST Send Message - From: {A-guid}, To: {B-guid}
[INFO] Message sent successfully: {msg-id}
[INFO] 🔄 Attempting to send SignalR notification...
[INFO] 📤 Sending to User: {B-guid}
[INFO] 📦 Message data: SenderId={A-guid}, Message=Hello
[INFO] ✅ SignalR notification sent successfully to user: {B-guid}
```

**User B Browser:**
```
✅ Received message: {senderId: "{A-guid}", message: "Hello", ...}
Current otherUserId: {A-guid}
Data senderId: {A-guid}
Match? true
✅ Displaying message in UI
```

**User B UI:**
→ Message bubble "Hello" appears instantly!

---

## 🎯 Action Items

### Immediately After Reading This:

1. **Restart app** với clean build
2. **Open 2 browsers** (Chrome Private + Firefox)
3. **Login 2 different users**
4. **Open Developer Console** (F12) ở cả 2
5. **Watch server console** logs
6. **Navigate to Conversation** ở cả 2
7. **Verify trong logs:**
   - ✅ Both users authenticated
   - ✅ Both users connected
   - ✅ 📊 Current tracked users: 2
8. **Send a test message**
9. **Watch all 3 consoles:**
   - Server: SignalR notification sent
   - Sender browser: (no special logs)
   - Receiver browser: ✅ Received message → ✅ Displaying
10. **Verify in UI:** Message appears < 1 second

---

## 🚨 If Still Not Working

### Copy và gửi cho tôi:

1. **Server logs** (toàn bộ output khi send message)
2. **User A browser console** logs
3. **User B browser console** logs
4. **Screenshot** của cả 3 consoles

Với đầy đủ logs này, tôi sẽ tìm ra vấn đề chính xác!

---

## ✅ Success Criteria

Khi mọi thứ hoạt động đúng, bạn sẽ thấy:

- ✅ Server: Is Authenticated: True
- ✅ Server: 📊 Current tracked users: 2 (hoặc hơn)
- ✅ Browser: SignalR Connected Successfully
- ✅ Browser: Received message
- ✅ Browser: Match? true
- ✅ Browser: Displaying message in UI
- ✅ UI: Message appears instantly

**🎉 Nếu tất cả check marks ✅ → Real-time messaging works!**

