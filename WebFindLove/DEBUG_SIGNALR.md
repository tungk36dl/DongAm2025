# 🐛 Debug SignalR Real-Time Issues

## Vấn đề: Messages không hiện real-time

### Root Cause Analysis:

1. **SignalR WebSocket không authenticate được**
   - Cookie authentication không tự động work với WebSocket
   - Cần configure để pass cookies

2. **User ID mapping có thể sai**
   - CustomUserIdProvider cần return đúng format
   - Controller gửi với format khác

3. **JavaScript comparison có thể sai**
   - Type mismatch: Guid vs String

## Solutions đang implement...

