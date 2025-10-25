# FileUploadService - Quick Start

## Sử Dụng Cơ Bản

### 1. Inject Service
```csharp
public class MyService
{
    private readonly IFileUploadService _fileUploadService;
    
    public MyService(IFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }
}
```

### 2. Upload File
```csharp
var options = new FileUploadOptions
{
    SubDirectory = "avatars",
    AllowedExtensions = new[] { ".jpg", ".png" },
    MaxFileSize = 5 * 1024 * 1024 // 5MB
};

var result = await _fileUploadService.UploadFileAsync(file, options);

if (result.Success)
{
    // Lưu result.FileName vào database
    user.Avatar = result.FileName;
}
else
{
    // Xử lý lỗi
    Console.WriteLine(result.ErrorMessage);
}
```

### 3. Xóa File
```csharp
await _fileUploadService.DeleteFileAsync("avatars/old-file.jpg");
```

## Common Use Cases

### Avatar Upload
```csharp
var options = new FileUploadOptions
{
    SubDirectory = "avatars",
    AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
    MaxFileSize = 5 * 1024 * 1024
};
```

### Document Upload
```csharp
var options = new FileUploadOptions
{
    SubDirectory = "documents",
    AllowedExtensions = new[] { ".pdf", ".docx", ".xlsx" },
    MaxFileSize = 10 * 1024 * 1024
};
```

## Xem Thêm

Chi tiết đầy đủ: [FILE_UPLOAD_SERVICE_DOCUMENTATION.md](../../../../FILE_UPLOAD_SERVICE_DOCUMENTATION.md)

