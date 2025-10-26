# ⚡ Quick Test Guide - Real-Time Messaging

## 🚀 Quick Start (5 phút)

### **Bước 1: Khởi động**
```bash
cd WebFindLove
dotnet build
dotnet run
```

### **Bước 2: Mở 2 browsers**
- **Browser A**: Chrome (Private)
- **Browser B**: Firefox (hoặc Edge)

### **Bước 3: Đăng nhập 2 accounts**
**Browser A:**
```
Email: admin@example.com
Password: Admin@123
```

**Browser B:**
```
Email: user@example.com
Password: User@123
```

*Nếu chưa có accounts, đăng ký mới từ trang Register*

---

## ✅ Test Scenarios (3 phút mỗi scenario)

### **Test 1: Tìm kiếm & Nhắn tin cơ bản**

**Browser A (Admin):**
1. ✅ Vào trang chủ
2. ✅ Nhập "user" vào search box
3. ✅ Click "Tìm kiếm"
4. ✅ Verify: User xuất hiện trong kết quả
5. ✅ Verify: Avatar hoặc initials hiển thị
6. ✅ Click nút "Nhắn tin"
7. ✅ Gõ message: "Hello from Admin!"
8. ✅ Click "Send"

**Browser B (User):**
9. ✅ Vào Messages → Conversation
10. ✅ Verify: Message "Hello from Admin!" xuất hiện NGAY LẬP TỨC
11. ✅ Verify: Avatar của Admin hiển thị
12. ✅ Verify: Timestamp đúng

**Expected Result:**
- ✅ Message xuất hiện < 1 giây
- ✅ Avatar hiển thị đúng
- ✅ No errors in console

---

### **Test 2: Online Status**

**Browser A & B:** (đang ở conversation)

1. ✅ Check header của conversation
2. ✅ Verify: Green dot + "Online" text
3. ✅ Close Browser A
4. ✅ Check Browser B
5. ✅ Verify: Gray dot + "Offline" text (sau 3-5 giây)

**Expected Result:**
- ✅ Status update tự động
- ✅ Không cần refresh

---

### **Test 3: Typing Indicator**

**Browser A & B:** (đang ở conversation)

**Browser A:**
1. ✅ Click vào message input
2. ✅ Gõ (KHÔNG gửi)

**Browser B:**
3. ✅ Verify: "... is typing" xuất hiện
4. ✅ Verify: Animation bubbles

**Browser A:**
5. ✅ Stop typing (không gõ gì trong 2 giây)

**Browser B:**
6. ✅ Verify: Typing indicator biến mất

**Expected Result:**
- ✅ Indicator xuất hiện ngay khi gõ
- ✅ Biến mất sau 2s không gõ

---

### **Test 4: Avatar Display**

**Preparation:**
1. User A upload avatar trong Edit Profile
2. Verify avatar lưu thành công

**Test:**
**Browser A:** (User có avatar)
1. ✅ Gửi message: "Testing avatar"

**Browser B:**
2. ✅ Verify: Avatar IMAGE hiển thị (không phải initials)
3. ✅ Verify: Avatar URL correct
4. ✅ Reload page
5. ✅ Verify: Avatar vẫn hiển thị cho messages cũ

**Expected Result:**
- ✅ Avatar hiển thị cho cả real-time và old messages
- ✅ Fallback to initials nếu không có avatar

---

### **Test 5: Multi-Message**

**Browser A & B:** Rapid fire test

**Browser A:**
1. ✅ Gửi 5 messages liên tiếp:
   - "Message 1"
   - "Message 2"
   - "Message 3"
   - "Message 4"
   - "Message 5"

**Browser B:**
2. ✅ Verify: Tất cả 5 messages xuất hiện
3. ✅ Verify: Đúng thứ tự (1,2,3,4,5)
4. ✅ Verify: Không bị duplicate
5. ✅ Verify: Auto-scroll to bottom

**Expected Result:**
- ✅ All messages delivered
- ✅ Correct order
- ✅ Smooth scrolling

---

## 🐛 Debug: Nếu có vấn đề

### **Problem: Messages không real-time**

**Check:**
```
1. Browser Console (F12)
   → Look for: "SignalR Connected"
   → If error: Check network tab for /chatHub

2. Server Console
   → Look for: "User connected"
   → If not: Check authentication

3. Fix:
   → Hard refresh (Ctrl+F5)
   → Clear cookies
   → Restart server
```

### **Problem: Avatar không hiển thị**

**Check:**
```
1. Browser Console
   → Look for 404 errors on avatar URL

2. Check avatar upload successful:
   → Go to Edit Profile
   → Verify avatar image shows

3. Check database:
   → Query: SELECT Avatar FROM Users WHERE Id = '...'
   → Should have valid path/URL

4. Fix:
   → Re-upload avatar
   → Check file permissions in wwwroot/uploads
```

### **Problem: Online status stuck**

**Check:**
```
1. Browser Console
   → Look for "User online" / "User offline" events

2. Server Console
   → Look for "User connected" / "User disconnected"

3. Fix:
   → Close ALL browser tabs
   → Reopen and login again
   → Check after 10 seconds
```

---

## 📊 Success Criteria

### **All Tests Pass:**
- ✅ Real-time messaging works
- ✅ Online status accurate
- ✅ Typing indicator smooth
- ✅ Avatar displays correctly
- ✅ No console errors
- ✅ Messages in correct order

### **If ANY test fails:**
1. Check detailed guide: `REALTIME_MESSAGE_FLOW_TEST.md`
2. Check server logs in `/Logs/app-log-*.txt`
3. Check browser console for errors
4. Follow debug steps above

---

## 🎉 Expected Output

### **Server Console:**
```
[INFO] Starting WebFindLove application
[INFO] SignalR configured
[INFO] Application starting...
[INFO] User connected - UserId: {...}, ConnectionId: {...}
[INFO] POST Send Message - From: {...}, To: {...}
[INFO] Message sent successfully: {...}
[DEBUG] SignalR notification sent to user: {...}
```

### **Browser Console:**
```
SignalR Connected
User online: { userId: "...", timestamp: "..." }
Received message: { senderId: "...", message: "...", senderAvatar: "..." }
```

### **UI:**
```
✅ Messages appear instantly
✅ Avatars show correctly
✅ Smooth animations
✅ Professional look
✅ No lag or freeze
```

---

## 🚀 Production Checklist

Trước khi deploy lên production:

- [ ] Tất cả 5 test scenarios pass
- [ ] No errors in server logs
- [ ] No errors in browser console
- [ ] Avatar upload works
- [ ] Multi-device tested
- [ ] Network interruption handled
- [ ] Load test với 10+ concurrent users
- [ ] Security review (XSS, authentication)

---

## 📚 More Resources

- **Chi tiết test:** `REALTIME_MESSAGE_FLOW_TEST.md`
- **Cải tiến:** `SIGNALR_IMPROVEMENTS_SUMMARY.md`
- **Full docs:** `SIGNALR_MESSAGING_IMPLEMENTATION.md`

---

## 💡 Tips

### **Fastest Test:**
```
1. Đăng nhập 2 browsers (1 phút)
2. Run Test 1 (messaging) (1 phút)
3. Check avatar displays (30 giây)
→ Total: 2.5 phút để verify core functionality
```

### **Comprehensive Test:**
```
Run all 5 scenarios: ~15 phút
→ Covers all features
→ Production-ready confidence
```

---

## ✨ Done!

Nếu tất cả tests pass → **🎉 Module hoạt động hoàn hảo!**

Nếu có vấn đề → Check debug section hoặc xem detailed docs.

**Happy Testing! 🚀**

