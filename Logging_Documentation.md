# Logging Documentation - WebFindLove

## Tổng quan

Dự án đã được bổ sung đầy đủ logging với Serilog để hỗ trợ debug và monitoring. Tất cả các controllers đã được tích hợp logging chi tiết tại các điểm quan trọng.

## Cấu hình Serilog

### Program.cs

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "WebFindLove")
    .WriteTo.Console(...)
    .WriteTo.File("Logs/app-log-.txt", rollingInterval: RollingInterval.Day, ...)
    .CreateLogger();
```

### Log Levels được sử dụng:
- **Information**: Các sự kiện quan trọng (login, CRUD operations thành công)
- **Warning**: Validation failures, unauthorized access attempts, not found
- **Error**: Service failures, exceptions
- **Debug**: Chi tiết kỹ thuật (password hashing, claims creation, data loading)
- **Fatal**: Application crashes (trong Program.cs)

## Controllers đã được thêm Logging

### 1. AuthController
**ILogger được inject**: ✅

#### Các điểm logging:

**Register (GET)**
- Log Information: Truy cập trang register

**Register (POST)**
- Log Information: Attempt đăng ký với username/email
- Log Warning: Validation failures, password empty
- Log Debug: User ID generation, password hashing
- Log Information: Đăng ký thành công (AJAX & non-AJAX)
- Log Error: Đăng ký thất bại với message và error details
- Log Debug: Error details parsing

**Login (GET)**
- Log Information: Truy cập trang login

**Login (POST)**
- Log Information: Login attempt với username/email
- Log Warning: Empty credentials, user not found, invalid password
- Log Debug: User fetching, user details, password verification, role routing
- Log Information: Login successful với username, role, userId
- Log Error: Exception during login

**ValidateSession (GET)**
- Log Debug: Session validation request
- Log Information: Session valid
- Log Warning: Session invalid

**Logout**
- Log Information: User logout và logout successful

### 2. UsersController
**ILogger được inject**: ✅

#### Các điểm logging:

**Index (GET)**
- Log Information: Request với search params, user count
- Log Error: Failed to retrieve users

**Details (GET)**
- Log Information: Request với userId
- Log Warning: User not found
- Log Debug: User details retrieved

**Create (GET)**
- Log Information: Truy cập create page

**Create (POST)**
- Log Information: Create attempt với username, email, role
- Log Warning: Validation failures
- Log Debug: User ID generation, password hashing
- Log Error: Create failed
- Log Information: Create successful với userId

**Edit (GET)**
- Log Information: Request với userId
- Log Warning: User not found
- Log Debug: User loaded for edit

**Edit (POST)**
- Log Information: Edit attempt với userId, username
- Log Warning: ID mismatch, validation failures
- Log Error: Update failed
- Log Information: Update successful

**Delete (GET)**
- Log Information: Delete confirmation request
- Log Warning: User not found
- Log Debug: User loaded for deletion

**DeleteConfirmed (POST)**
- Log Information: Delete confirmed, delete successful
- Log Error: Delete failed

**LoadRolesAsync (Private)**
- Log Debug: Loading roles, role count, failed to load roles

### 3. RolesController
**ILogger được inject**: ✅ (đã có sẵn)

#### Các điểm logging:

**Index (GET)**
- Log Information: Request với search params, role count
- Log Error: Failed to retrieve roles

**Details (GET)**
- Log Information: Request với roleId
- Log Warning: Role not found
- Log Debug: Role details retrieved

**Create (GET)**
- Log Information: Truy cập create page

**Create (POST)**
- Log Information: Create attempt với roleName
- Log Warning: Validation failures
- Log Error: Create failed
- Log Information: Create successful

**Edit (GET)**
- Log Information: Request với roleId
- Log Warning: Role not found
- Log Debug: Role loaded for edit

**Edit (POST)**
- Log Information: Edit attempt với roleId, roleName
- Log Warning: ID mismatch, validation failures
- Log Error: Update failed
- Log Information: Update successful

**Delete (GET)**
- Log Information: Delete confirmation request
- Log Warning: Role not found
- Log Debug: Role loaded with user count

**DeleteConfirmed (POST)**
- Log Information: Delete confirmed, delete successful
- Log Error: Delete failed

**CheckNameExists (GET)**
- Log Debug: Name check request, result
- Log Warning: Check failed

**GetRolesWithUserCount (GET)**
- Log Debug: Request
- Log Information: Successful retrieval
- Log Error: Failed

### 4. HomeController
**ILogger được inject**: ✅ (đã có sẵn)

#### Các điểm logging:

**Index (GET)**
- Log Information: Request với auth status, username, role

**Privacy (GET)**
- Log Information: Request với username

**Error**
- Log Error: Error page displayed với requestId

### 5. AdminController
**ILogger được inject**: ✅

#### Các điểm logging:

**Index (GET)**
- Log Information: Admin dashboard access với username, userId
- Log Warning: Unauthenticated access, non-admin access
- Log Debug: Successful access

### 6. BaseController
**Protected Logger property thêm vào**: ✅

#### Các điểm logging:

**HandleServiceResponse**
- Log Debug: Service response successful
- Log Warning: Service response failed

**HandleApiResponse**
- Log Debug: API response successful
- Log Warning: API response failed

## Structured Logging

Tất cả log statements sử dụng structured logging với named parameters:

```csharp
_logger.LogInformation("User logged in - Username: {Username}, Role: {Role}, UserId: {UserId}", 
    user.UserName, userRole, user.Id);
```

### Các parameters thường dùng:
- `{Username}` - Tên người dùng
- `{UserId}` - ID người dùng
- `{Email}` - Email
- `{Role}` - Vai trò
- `{RoleId}` - ID vai trò
- `{RoleName}` - Tên vai trò
- `{Message}` - Thông báo lỗi
- `{ErrorDetails}` - Chi tiết lỗi
- `{Errors}` - Validation errors
- `{IsAuthenticated}` - Trạng thái xác thực
- `{CurrentUser}` - Người dùng hiện tại thực hiện action

## Log File Location

Logs được lưu tại: `Logs/app-log-YYYYMMDD.txt`

Format:
```
[2025-10-25 14:30:45.123 +07:00 INF] User logged in - Username: "admin", Role: "Admin", UserId: "12345-..."
```

## Console Output

Console logs có format ngắn gọn hơn:
```
[14:30:45 INF] User logged in - Username: "admin", Role: "Admin", UserId: "12345-..."
```

## Lợi ích của Logging

### 1. Debug Support
- Track user actions và flow
- Identify validation failures
- Monitor authentication attempts
- Trace data operations

### 2. Security Monitoring
- Failed login attempts
- Unauthorized access attempts
- Admin dashboard access tracking
- Role-based access violations

### 3. Performance Monitoring
- Service response times
- Database operation tracking
- API call monitoring

### 4. Error Tracking
- Exception details with stack traces
- Service failures
- Data validation errors
- Business logic errors

## Best Practices được áp dụng

1. ✅ **Structured Logging**: Sử dụng named parameters thay vì string interpolation
2. ✅ **Appropriate Log Levels**: Information cho events quan trọng, Warning cho issues, Error cho failures
3. ✅ **Context Information**: Luôn log username/userId khi có
4. ✅ **Sensitive Data Protection**: Không log passwords, chỉ log "password hashed"
5. ✅ **Consistent Naming**: Sử dụng naming conventions nhất quán
6. ✅ **Action Tracking**: Log cả GET và POST requests
7. ✅ **Error Details**: Log đầy đủ message và error details khi có lỗi

## Debugging với Logs

### Ví dụ 1: Track Login Flow
```
[14:30:40 INF] GET Login page accessed
[14:30:45 INF] POST Login attempt for username/email: "admin"
[14:30:45 DBG] Fetching users from database to find matching user
[14:30:45 DBG] Found 5 users in database
[14:30:45 DBG] User found - Username: "admin", Email: "admin@test.com", IsActive: True, Role: "Admin"
[14:30:45 DBG] Password verified successfully for user: "admin"
[14:30:45 INF] User logged in successfully - Username: "admin", Role: "Admin", UserId: "12345..."
[14:30:45 DBG] Redirecting admin user to Admin dashboard
```

### Ví dụ 2: Track User Creation
```
[14:35:10 INF] GET Create User page - Requested by: "admin"
[14:35:10 DBG] Loading roles for dropdown
[14:35:10 DBG] Loaded 3 active roles
[14:35:20 INF] POST Create User - Username: "newuser", Email: "new@test.com", Role: "User", Requested by: "admin"
[14:35:20 DBG] Generated new user ID: "67890-..." for username: "newuser"
[14:35:20 DBG] Password hashed successfully for user: "newuser"
[14:35:20 INF] User created successfully - Username: "newuser", UserId: "67890...", CreatedBy: "admin"
```

### Ví dụ 3: Track Failed Login
```
[14:40:10 INF] POST Login attempt for username/email: "hacker"
[14:40:10 DBG] Fetching users from database to find matching user
[14:40:10 DBG] Found 5 users in database
[14:40:10 WRN] Login failed: User not found for username/email: "hacker"
```

## Monitoring và Alerts

Có thể setup alerts dựa trên logs:

1. **Security Alerts**:
   - Multiple failed login attempts từ cùng IP
   - Unauthorized access attempts
   - Admin access from unusual locations

2. **Performance Alerts**:
   - Slow response times
   - Database connection issues
   - High error rates

3. **Business Alerts**:
   - User registration patterns
   - CRUD operation volumes
   - System usage statistics

## Tích hợp với Monitoring Tools

Serilog có thể tích hợp với:
- **Seq**: Structured log viewer
- **Application Insights**: Azure monitoring
- **Elasticsearch + Kibana**: Log aggregation
- **Splunk**: Enterprise logging
- **Datadog**: APM và monitoring

## Next Steps

1. ✅ Setup log rotation policies
2. ✅ Configure log retention
3. 📝 Setup monitoring dashboards
4. 📝 Configure alerts
5. 📝 Implement log analysis
6. 📝 Setup centralized logging (nếu có multiple instances)

## Tóm tắt

- ✅ 6 Controllers đã được thêm logging đầy đủ
- ✅ Program.cs đã được cấu hình với Serilog enhanced
- ✅ Structured logging với named parameters
- ✅ Appropriate log levels (Information, Warning, Error, Debug)
- ✅ Context information (username, userId, role) được log consistently
- ✅ Security-conscious (không log sensitive data)
- ✅ Ready for production monitoring
- ✅ Zero linter errors

Tất cả logging đã sẵn sàng để hỗ trợ debug và monitoring trong production! 🎉

