# Tài Liệu Tính Năng Mới: Avatar và Quản Lý Hồ Sơ

## Tổng Quan

Dự án đã được cập nhật với các tính năng mới cho phép người dùng quản lý tài khoản và hồ sơ cá nhân của mình một cách riêng biệt, bao gồm khả năng upload avatar.

## Các Thay Đổi Chi Tiết

### 1. Cơ Sở Dữ Liệu

#### Thêm Trường Avatar vào Bảng User
- **File**: `WebFindLove/Models/Entities/User.cs`
- **Trường mới**: `Avatar` (string, max 500 ký tự)
- **Migration**: `20251025114037_AddAvatarToUser.cs`
- **Mô tả**: Lưu đường dẫn đến file avatar của người dùng

### 2. ViewModels

#### EditAccountVM
- **File**: `WebFindLove/Models/Services/UserService/ViewModels/EditAccountVM.cs`
- **Mục đích**: Quản lý thông tin tài khoản (username, email, password)
- **Các trường**:
  - `Id`: ID người dùng
  - `UserName`: Tên đăng nhập
  - `Email`: Email
  - `NewPassword`: Mật khẩu mới (tùy chọn)
  - `ConfirmPassword`: Xác nhận mật khẩu mới

#### EditProfileVM
- **File**: `WebFindLove/Models/Services/UserService/ViewModels/EditProfileVM.cs`
- **Mục đích**: Quản lý thông tin cá nhân
- **Các trường**:
  - Thông tin cơ bản: `FullName`, `PhoneNumber`, `Gender`, `DateOfBirth`, `Height`
  - Thông tin vị trí: `Location`, `Hometown`
  - Giới thiệu: `Bio`, `Interests`
  - Avatar: `Avatar` (đường dẫn hiện tại), `AvatarFile` (file upload)

### 3. Services

#### IUserService
- **File**: `WebFindLove/Models/Services/UserService/IUserService.cs`
- **Phương thức mới**:
  - `UpdateAccountAsync(EditAccountVM model)`: Cập nhật thông tin tài khoản
  - `UpdateProfileAsync(EditProfileVM model, string uploadsPath)`: Cập nhật thông tin hồ sơ và xử lý upload avatar

#### UserService
- **File**: `WebFindLove/Models/Services/UserService/UserService.cs`
- **Tính năng mới**:
  - Kiểm tra tính duy nhất của username và email khi cập nhật
  - Mã hóa mật khẩu mới nếu được cung cấp
  - Xử lý upload avatar với validation:
    - Chỉ chấp nhận file: JPG, JPEG, PNG, GIF
    - Tự động xóa avatar cũ khi upload avatar mới
    - Tạo tên file duy nhất sử dụng GUID

### 4. Controllers

#### UsersController
- **File**: `WebFindLove/Controllers/UsersController.cs`
- **Actions mới**:

##### GET `/Users/EditAccount`
- Hiển thị form chỉnh sửa thông tin tài khoản
- Chỉ người dùng đã đăng nhập mới có thể truy cập
- Tự động load thông tin hiện tại của người dùng

##### POST `/Users/EditAccount`
- Xử lý cập nhật thông tin tài khoản
- Validation đầy đủ
- Cập nhật session nếu username thay đổi
- Hiển thị thông báo thành công/lỗi

##### GET `/Users/EditProfile`
- Hiển thị form chỉnh sửa thông tin cá nhân
- Chỉ người dùng đã đăng nhập mới có thể truy cập
- Hiển thị avatar hiện tại (nếu có)

##### POST `/Users/EditProfile`
- Xử lý cập nhật thông tin hồ sơ
- Upload và lưu avatar mới
- Validation file upload
- Cập nhật session với thông tin mới
- Hiển thị thông báo thành công/lỗi

### 5. Views

#### EditAccount.cshtml
- **File**: `WebFindLove/Views/Users/EditAccount.cshtml`
- **Tính năng**:
  - Form chỉnh sửa username và email
  - Form đổi mật khẩu (tùy chọn)
  - Validation client-side và server-side
  - Link nhanh đến EditProfile
  - UI responsive với Bootstrap

#### EditProfile.cshtml
- **File**: `WebFindLove/Views/Users/EditProfile.cshtml`
- **Tính năng**:
  - Hiển thị và upload avatar
  - Preview avatar trước khi upload
  - Form đầy đủ cho tất cả thông tin cá nhân
  - Dropdown cho giới tính
  - Date picker cho ngày sinh
  - Textarea cho biography
  - Link nhanh đến EditAccount
  - UI responsive với Bootstrap và custom JavaScript

### 6. Navigation

#### _Layout.cshtml
- **File**: `WebFindLove/Views/Shared/_Layout.cshtml`
- **Cập nhật**:
  - Thay thế link "Hồ sơ của tôi" bằng 2 link riêng biệt:
    - "Tài khoản" → `/Users/EditAccount`
    - "Hồ sơ cá nhân" → `/Users/EditProfile`
  - Cập nhật cả menu desktop và mobile

### 7. File Storage

#### Thư mục Avatars
- **Đường dẫn**: `wwwroot/uploads/avatars/`
- **Mục đích**: Lưu trữ file avatar của người dùng
- **File naming**: Sử dụng GUID để đảm bảo tính duy nhất
- **Gitkeep**: File `.gitkeep` đảm bảo thư mục được track bởi git

## Cách Sử Dụng

### Đối với Người Dùng

1. **Chỉnh sửa Tài khoản**:
   - Đăng nhập vào hệ thống
   - Click vào tên người dùng ở góc trên bên phải
   - Chọn "Tài khoản" từ dropdown menu
   - Cập nhật username, email hoặc password
   - Click "Save Changes"

2. **Chỉnh sửa Hồ sơ Cá nhân**:
   - Đăng nhập vào hệ thống
   - Click vào tên người dùng ở góc trên bên phải
   - Chọn "Hồ sơ cá nhân" từ dropdown menu
   - Upload avatar mới (tùy chọn)
   - Điền/cập nhật thông tin cá nhân
   - Click "Save Profile"

### Đối với Developer

#### Test Upload Avatar
```csharp
// Prepare test file
IFormFile avatarFile = ...; // Your test file
var model = new EditProfileVM 
{
    Id = userId,
    AvatarFile = avatarFile,
    // ... other fields
};

// Call service
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
var result = await _userService.UpdateProfileAsync(model, uploadsPath);
```

#### Hiển thị Avatar trong View
```html
@if (!string.IsNullOrEmpty(Model.Avatar))
{
    <img src="~/uploads/avatars/@Model.Avatar" alt="User Avatar" class="avatar" />
}
else
{
    <img src="~/images/default-avatar.png" alt="Default Avatar" class="avatar" />
}
```

## Bảo Mật

1. **Authorization**: Chỉ người dùng đã đăng nhập mới có thể chỉnh sửa thông tin
2. **Ownership Check**: Người dùng chỉ có thể chỉnh sửa thông tin của chính họ
3. **File Validation**: Chỉ chấp nhận file ảnh với định dạng hợp lệ
4. **Password Hashing**: Mật khẩu được mã hóa trước khi lưu vào database
5. **Session Update**: Session được cập nhật tự động sau khi thay đổi thông tin

## Lưu Ý Kỹ Thuật

1. **File Size Limit**: Nên thêm giới hạn kích thước file trong production (ví dụ: 5MB)
2. **Image Processing**: Có thể thêm tính năng resize/crop ảnh để tối ưu storage
3. **CDN**: Trong production, nên sử dụng CDN để phục vụ static files
4. **Validation**: Tất cả validation đều được thực hiện ở cả client-side và server-side

## Testing Checklist

- [ ] Tạo tài khoản mới và đăng nhập
- [ ] Cập nhật username và kiểm tra session update
- [ ] Đổi password và đăng nhập lại với password mới
- [ ] Upload avatar và kiểm tra hiển thị
- [ ] Upload avatar mới và kiểm tra avatar cũ đã bị xóa
- [ ] Cập nhật tất cả thông tin profile
- [ ] Test validation (username trùng, email không hợp lệ, v.v.)
- [ ] Test với file không phải ảnh
- [ ] Test responsive UI trên mobile
- [ ] Test dark mode

## Tài Liệu Liên Quan

- [Clean Architecture Documentation](Clean_Architecture_Documentation.md)
- [Views Documentation](Views_Documentation.md)
- [Logging Documentation](Logging_Documentation.md)

