# 🧪 Quick Test Guide - Online/Offline Status

## Cách Test Nhanh (5 phút)

### Setup:
1. ✅ Build & run application
2. ✅ Mở 2 browsers khác nhau (hoặc 2 incognito windows)
3. ✅ Login 2 users khác nhau (User A, User B)

---

## Test 1: Basic Online Status ⭐

### Steps:
1. **User A**: Login trên Chrome
2. **User B**: Login trên Firefox
3. **User B**: Vào `/Messages` → Click vào conversation với User A
4. **Kỳ vọng**: Thấy **green dot** và text **"Online"** bên cạnh tên User A

✅ Pass nếu: Hiển thị đúng status online

---

## Test 2: Real-time Offline Detection ⭐⭐

### Steps:
1. Tiếp tục từ Test 1 (User B đang xem conversation với User A)
2. **User A**: Đóng browser (hoặc logout)
3. **User B**: Quan sát màn hình (không cần refresh)
4. **Kỳ vọng**: 
   - Green dot → Gray dot
   - Text "Online" → "Offline"
   - Tự động update trong 1-2 giây

✅ Pass nếu: Status tự động chuyển sang offline

---

## Test 3: Chat Widget Status ⭐⭐

### Steps:
1. **User A**: Online trên Chrome
2. **User B**: Click vào **chat widget button** (góc dưới bên phải)
3. **User B**: Click vào conversation với User A trong popup
4. **Kỳ vọng**: Header popup hiển thị status online của User A
5. **User A**: Đóng browser
6. **Kỳ vọng**: Popup tự động update sang offline

✅ Pass nếu: Status trong chat widget hoạt động đúng

---

## Test 4: Multi-Device Support ⭐⭐⭐

### Steps:
1. **User A**: Login trên Chrome
2. **User A**: Login thêm trên Firefox (cùng account)
3. **User B**: Xem conversation với User A → **Online**
4. **User A**: Đóng Chrome (nhưng giữ Firefox)
5. **User B**: Quan sát
   - **Kỳ vọng**: User A vẫn **Online** (vì còn Firefox)
6. **User A**: Đóng Firefox
7. **User B**: Quan sát
   - **Kỳ vọng**: User A chuyển sang **Offline**

✅ Pass nếu: Multi-device tracking hoạt động

---

## Test 5: Initial Status Check ⭐

### Steps:
1. **User A**: Đang online
2. **User B**: Refresh page hoặc mở conversation mới với User A
3. **Kỳ vọng**: Status hiển thị đúng ngay từ đầu (không cần chờ)

✅ Pass nếu: Initial status đúng

---

## 🔍 Debug Tips

### Check Browser Console:

**User B Console** (khi xem conversation với User A):
```javascript
// Khi User A connect
"User status changed: { userId: 'xxx', isOnline: true }"
"User status updated: Online"

// Khi User A disconnect
"User status changed: { userId: 'xxx', isOnline: false }"
"User status updated: Offline"
```

### Check Server Logs:

```
[INFO] User {UserId} connected with ConnectionId {xxx}
[INFO] User {UserId} is now ONLINE
[INFO] User {UserId} disconnected
[INFO] User {UserId} is now OFFLINE
```

---

## ❌ Common Issues

### Issue 1: Status luôn hiển thị "Checking..."
**Nguyên nhân**: SignalR chưa connect
**Fix**: Kiểm tra console có log "SignalR Connected" không

### Issue 2: Status không update real-time
**Nguyên nhân**: Không nhận được event "UserStatusChanged"
**Fix**: 
- Check SignalR connection state
- Check browser console có log events không
- Restart cả 2 browsers

### Issue 3: Multi-device không work
**Nguyên nhân**: Service không track đúng connections
**Fix**: Check server logs để xem "Total connections" và "Remaining connections"

---

## ✅ Expected Results Summary

| Test | Visual Indicator | Update Speed |
|------|------------------|--------------|
| Online | 🟢 Green dot + "Online" | Immediate on load |
| Offline | ⚪ Gray dot + "Offline" | 1-2 seconds |
| Chat Widget | Same as above | Real-time |
| Multi-device | Stays online until all disconnect | Real-time |

---

## 🎯 Quick Verification Checklist

- [ ] Green dot khi user online
- [ ] Gray dot khi user offline
- [ ] Tự động update không cần refresh
- [ ] Chat widget hiển thị đúng
- [ ] Conversation page hiển thị đúng
- [ ] Multi-device support hoạt động
- [ ] Console logs hiển thị events
- [ ] Server logs có thông tin connections

---

**Tất cả tests pass** → ✅ Tính năng hoạt động hoàn hảo!

**Có issues** → Check Debug Tips và Common Issues ở trên

