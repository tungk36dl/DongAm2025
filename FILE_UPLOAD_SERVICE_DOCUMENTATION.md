# FileUploadService Documentation

## Tổng Quan

`FileUploadService` là một service chuyên dụng để xử lý việc upload, xóa và quản lý file trong ứng dụng WebFindLove. Service này được thiết kế theo các nguyên tắc SOLID và Clean Architecture, giúp tái sử dụng code và dễ dàng bảo trì.

## Cấu Trúc Thư Mục

```
WebFindLove/Models/Services/FileUploadService/
├── Dto/
│   ├── FileUploadOptions.cs     # Cấu hình cho upload
│   └── FileUploadResult.cs      # Kết quả upload
├── IFileUploadService.cs         # Interface
└── FileUploadService.cs          # Implementation
```

## DTOs (Data Transfer Objects)

### FileUploadOptions

Cấu hình cho việc upload file:

```csharp
public class FileUploadOptions
{
    // Thư mục con trong wwwroot/uploads/ (vd: "avatars", "documents")
    public string SubDirectory { get; set; } = "files";
    
    // Các extension được phép (vd: [".jpg", ".png"])
    public string[]? AllowedExtensions { get; set; }
    
    // Kích thước file tối đa (bytes) - mặc định: 5MB
    public long MaxFileSize { get; set; } = 5 * 1024 * 1024;
    
    // Tạo tên file unique bằng GUID
    public bool GenerateUniqueFileName { get; set; } = true;
    
    // Giữ nguyên tên file gốc
    public bool PreserveOriginalFileName { get; set; } = false;
    
    // Ghi đè file trùng tên
    public bool OverwriteExisting { get; set; } = false;
}
```

### FileUploadResult

Kết quả sau khi upload:

```csharp
public class FileUploadResult
{
    public bool Success { get; set; }              // Upload có thành công?
    public string? FilePath { get; set; }          // Đường dẫn tương đối
    public string? FileName { get; set; }          // Tên file
    public string? FullPath { get; set; }          // Đường dẫn đầy đủ trên server
    public string? ErrorMessage { get; set; }      // Thông báo lỗi
    public long FileSize { get; set; }             // Kích thước file (bytes)
    public string? ContentType { get; set; }       // MIME type
}
```

## Interface: IFileUploadService

### Các Phương Thức

#### 1. UploadFileAsync
Upload file lên server với các options cấu hình.

```csharp
Task<FileUploadResult> UploadFileAsync(IFormFile file, FileUploadOptions options);
```

**Tính năng**:
- Validation file (extension, size)
- Tạo thư mục nếu chưa tồn tại
- Tạo tên file unique hoặc sanitize tên gốc
- Kiểm tra file trùng lặp
- Logging chi tiết

**Ví dụ**:
```csharp
var options = new FileUploadOptions
{
    SubDirectory = "avatars",
    AllowedExtensions = new[] { ".jpg", ".png", ".gif" },
    MaxFileSize = 5 * 1024 * 1024, // 5MB
    GenerateUniqueFileName = true
};

var result = await _fileUploadService.UploadFileAsync(avatarFile, options);
if (result.Success)
{
    Console.WriteLine($"File uploaded: {result.FilePath}");
}
```

#### 2. DeleteFileAsync
Xóa file khỏi server.

```csharp
Task<bool> DeleteFileAsync(string filePath);
```

**Ví dụ**:
```csharp
var deleted = await _fileUploadService.DeleteFileAsync("avatars/old-avatar.jpg");
if (deleted)
{
    Console.WriteLine("File deleted successfully");
}
```

#### 3. FileExists
Kiểm tra file có tồn tại hay không.

```csharp
bool FileExists(string filePath);
```

**Ví dụ**:
```csharp
if (_fileUploadService.FileExists("avatars/user-avatar.jpg"))
{
    Console.WriteLine("File exists");
}
```

#### 4. GetFullPath
Lấy đường dẫn đầy đủ từ đường dẫn tương đối.

```csharp
string GetFullPath(string relativePath);
```

**Ví dụ**:
```csharp
var fullPath = _fileUploadService.GetFullPath("avatars/image.jpg");
// Output: D:/MyApp/wwwroot/uploads/avatars/image.jpg
```

#### 5. ValidateFile
Validate file trước khi upload.

```csharp
string? ValidateFile(IFormFile file, FileUploadOptions options);
```

**Ví dụ**:
```csharp
var error = _fileUploadService.ValidateFile(file, options);
if (error != null)
{
    Console.WriteLine($"Validation error: {error}");
}
```

## Implementation: FileUploadService

### Constructor

```csharp
public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
```

Service tự động:
- Lấy đường dẫn `wwwroot`
- Tạo thư mục `uploads` nếu chưa có
- Khởi tạo logger

### Tính Năng Chính

#### 1. Validation
- **File Size**: Kiểm tra kích thước file
- **File Extension**: Kiểm tra extension được phép
- **File Existence**: Kiểm tra file đã tồn tại

#### 2. Filename Generation
- **Unique GUID**: `{guid}.{extension}` (vd: `a1b2c3d4-...-xyz.jpg`)
- **Original**: Giữ nguyên tên gốc
- **Sanitized**: `{sanitized_name}_{timestamp}.{extension}`

#### 3. File Management
- Tự động tạo thư mục
- Xóa file cũ khi upload file mới
- Ghi log chi tiết mọi thao tác

#### 4. Error Handling
- Try-catch toàn bộ operations
- Trả về error message chi tiết
- Logging errors với context

## Đăng Ký Service

Service đã được đăng ký trong `ServiceRegistration.cs`:

```csharp
services.AddScoped<IFileUploadService, FileUploadService>();
```

## Cách Sử Dụng

### 1. Trong Controller

```csharp
public class MyController : Controller
{
    private readonly IFileUploadService _fileUploadService;
    
    public MyController(IFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }
    
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var options = new FileUploadOptions
        {
            SubDirectory = "documents",
            AllowedExtensions = new[] { ".pdf", ".docx" },
            MaxFileSize = 10 * 1024 * 1024 // 10MB
        };
        
        var result = await _fileUploadService.UploadFileAsync(file, options);
        
        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage);
        }
        
        return Ok(new { path = result.FilePath });
    }
}
```

### 2. Trong Service Layer

**UserService Example** (Upload Avatar):

```csharp
public class UserService : IUserService
{
    private readonly IFileUploadService _fileUploadService;
    
    public UserService(IFileUploadService fileUploadService, ...)
    {
        _fileUploadService = fileUploadService;
    }
    
    public async Task<DataResponse<User>> UpdateProfileAsync(EditProfileVM model)
    {
        // ... update user info ...
        
        if (model.AvatarFile != null)
        {
            var uploadOptions = new FileUploadOptions
            {
                SubDirectory = "avatars",
                AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
                MaxFileSize = 5 * 1024 * 1024
            };
            
            var result = await _fileUploadService.UploadFileAsync(
                model.AvatarFile, 
                uploadOptions
            );
            
            if (!result.Success)
            {
                return new DataResponse<User> 
                { 
                    Success = false, 
                    Message = result.ErrorMessage 
                };
            }
            
            // Delete old avatar
            if (!string.IsNullOrEmpty(user.Avatar))
            {
                await _fileUploadService.DeleteFileAsync($"avatars/{user.Avatar}");
            }
            
            user.Avatar = result.FileName;
        }
        
        // ... save to database ...
    }
}
```

## Upload Options Presets

### Avatar Images
```csharp
var avatarOptions = new FileUploadOptions
{
    SubDirectory = "avatars",
    AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
    MaxFileSize = 5 * 1024 * 1024, // 5MB
    GenerateUniqueFileName = true
};
```

### Documents
```csharp
var documentOptions = new FileUploadOptions
{
    SubDirectory = "documents",
    AllowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" },
    MaxFileSize = 10 * 1024 * 1024, // 10MB
    GenerateUniqueFileName = true
};
```

### Photos
```csharp
var photoOptions = new FileUploadOptions
{
    SubDirectory = "photos",
    AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" },
    MaxFileSize = 10 * 1024 * 1024, // 10MB
    GenerateUniqueFileName = true
};
```

### User Uploads (Preserve Original Name)
```csharp
var userFileOptions = new FileUploadOptions
{
    SubDirectory = "user-files",
    MaxFileSize = 20 * 1024 * 1024, // 20MB
    GenerateUniqueFileName = false,
    PreserveOriginalFileName = false // Will sanitize + add timestamp
};
```

## Logging

Service ghi log chi tiết cho mọi thao tác:

```
[Information] FileUploadService initialized with base path: D:/MyApp/wwwroot/uploads
[Information] Starting file upload - FileName: avatar.jpg, Size: 123456 bytes, SubDirectory: avatars
[Debug] Upload directory ensured: D:/MyApp/wwwroot/uploads/avatars
[Debug] Generated unique filename: a1b2c3d4-5678-90ab-cdef-1234567890ab.jpg
[Information] File uploaded successfully - Path: D:/MyApp/.../avatar.jpg, Size: 123456 bytes
[Information] File deleted successfully: D:/MyApp/wwwroot/uploads/avatars/old-avatar.jpg
```

## Error Handling

### Validation Errors
```csharp
// File quá lớn
"File size exceeds maximum allowed size of 5.00 MB."

// Extension không hợp lệ
"File type not allowed. Allowed types: .jpg, .png, .gif"

// Không có file
"No file provided or file is empty."

// Không có extension
"File must have an extension."
```

### Upload Errors
```csharp
// File đã tồn tại
"File already exists."

// Lỗi hệ thống
"Error uploading file: {exception message}"
```

## Best Practices

### 1. Luôn Kiểm Tra Kết Quả
```csharp
var result = await _fileUploadService.UploadFileAsync(file, options);
if (!result.Success)
{
    // Handle error
    return BadRequest(result.ErrorMessage);
}
```

### 2. Xóa File Cũ Trước Khi Upload Mới
```csharp
if (!string.IsNullOrEmpty(user.Avatar))
{
    await _fileUploadService.DeleteFileAsync($"avatars/{user.Avatar}");
}
```

### 3. Sử Dụng Options Phù Hợp
```csharp
// Tùy chỉnh theo từng use case
var options = new FileUploadOptions { ... };
```

### 4. Validate Trước Nếu Cần
```csharp
var error = _fileUploadService.ValidateFile(file, options);
if (error != null)
{
    ModelState.AddModelError("file", error);
    return View(model);
}
```

## Security Considerations

1. **File Size Limit**: Luôn set `MaxFileSize` để tránh DoS
2. **Allowed Extensions**: Chỉ cho phép các extension cần thiết
3. **Filename Sanitization**: Service tự động sanitize filename
4. **Path Traversal**: Service tự động xử lý path traversal attacks
5. **Content Type Validation**: Nên validate content type ngoài extension

## Performance Tips

1. **Async Operations**: Tất cả operations đều async
2. **Stream Processing**: Sử dụng stream để xử lý file lớn
3. **Directory Caching**: Directory được tạo một lần và cache

## Testing

### Unit Test Example
```csharp
[Fact]
public async Task UploadFileAsync_WithValidFile_ReturnsSuccess()
{
    // Arrange
    var mockFile = CreateMockFormFile("test.jpg", 1024);
    var options = new FileUploadOptions
    {
        SubDirectory = "test",
        AllowedExtensions = new[] { ".jpg" }
    };
    
    // Act
    var result = await _fileUploadService.UploadFileAsync(mockFile, options);
    
    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.FileName);
}
```

## Migration từ Code Cũ

### Trước
```csharp
// UserService.cs
var fileName = $"{Guid.NewGuid()}{extension}";
var filePath = Path.Combine(uploadsPath, fileName);
Directory.CreateDirectory(uploadsPath);
using (var stream = new FileStream(filePath, FileMode.Create))
{
    await file.CopyToAsync(stream);
}
user.Avatar = fileName;
```

### Sau
```csharp
// UserService.cs
var options = new FileUploadOptions { SubDirectory = "avatars", ... };
var result = await _fileUploadService.UploadFileAsync(file, options);
if (result.Success)
{
    user.Avatar = result.FileName;
}
```

## Tương Lai / Improvements

Các tính năng có thể thêm vào:

1. **Image Processing**: Resize, crop, optimize images
2. **Cloud Storage**: Support Azure Blob, AWS S3, etc.
3. **Virus Scanning**: Integrate antivirus scanning
4. **Thumbnail Generation**: Auto generate thumbnails
5. **Watermarking**: Add watermark to images
6. **CDN Integration**: Upload to CDN automatically
7. **Compression**: Auto compress files before saving
8. **Metadata Extraction**: Extract and store file metadata

## Liên Hệ & Support

Nếu có vấn đề hoặc câu hỏi về FileUploadService, vui lòng tạo issue hoặc liên hệ với team phát triển.

---

**Version**: 1.0.0  
**Last Updated**: 2025-10-25  
**Author**: WebFindLove Development Team

