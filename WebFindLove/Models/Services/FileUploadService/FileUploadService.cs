using Microsoft.AspNetCore.Http;
using WebFindLove.Models.Services.FileUploadService.Dto;

namespace WebFindLove.Models.Services.FileUploadService
{
    /// <summary>
    /// Service for handling file uploads and management
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string _uploadsBasePath;

        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
            _uploadsBasePath = Path.Combine(_environment.WebRootPath, "uploads");
            
            // Ensure uploads directory exists
            Directory.CreateDirectory(_uploadsBasePath);
            
            _logger.LogInformation("FileUploadService initialized with base path: {BasePath}", _uploadsBasePath);
        }

        public async Task<FileUploadResult> UploadFileAsync(IFormFile file, FileUploadOptions options)
        {
            _logger.LogInformation("Starting file upload - FileName: {FileName}, Size: {Size} bytes, SubDirectory: {SubDirectory}", 
                file.FileName, file.Length, options.SubDirectory);

            var result = new FileUploadResult
            {
                ContentType = file.ContentType,
                FileSize = file.Length
            };

            try
            {
                // Validate file
                var validationError = ValidateFile(file, options);
                if (validationError != null)
                {
                    _logger.LogWarning("File validation failed - FileName: {FileName}, Error: {Error}", 
                        file.FileName, validationError);
                    result.Success = false;
                    result.ErrorMessage = validationError;
                    return result;
                }

                // Prepare directory
                var uploadDir = Path.Combine(_uploadsBasePath, options.SubDirectory);
                Directory.CreateDirectory(uploadDir);
                _logger.LogDebug("Upload directory ensured: {UploadDir}", uploadDir);

                // Generate filename
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                string fileName;

                if (options.GenerateUniqueFileName)
                {
                    fileName = $"{Guid.NewGuid()}{extension}";
                    _logger.LogDebug("Generated unique filename: {FileName}", fileName);
                }
                else if (options.PreserveOriginalFileName)
                {
                    fileName = Path.GetFileName(file.FileName);
                    _logger.LogDebug("Using original filename: {FileName}", fileName);
                }
                else
                {
                    // Sanitize original filename but keep it recognizable
                    var originalName = Path.GetFileNameWithoutExtension(file.FileName);
                    fileName = $"{SanitizeFileName(originalName)}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
                    _logger.LogDebug("Generated sanitized filename: {FileName}", fileName);
                }

                var filePath = Path.Combine(uploadDir, fileName);

                // Check if file exists and handle accordingly
                if (File.Exists(filePath) && !options.OverwriteExisting)
                {
                    _logger.LogWarning("File already exists and overwrite is disabled: {FilePath}", filePath);
                    result.Success = false;
                    result.ErrorMessage = "File already exists.";
                    return result;
                }

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("File uploaded successfully - Path: {FilePath}, Size: {Size} bytes", 
                    filePath, file.Length);

                // Set result
                result.Success = true;
                result.FileName = fileName;
                result.FilePath = Path.Combine(options.SubDirectory, fileName).Replace("\\", "/");
                result.FullPath = filePath;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file - FileName: {FileName}", file.FileName);
                result.Success = false;
                result.ErrorMessage = $"Error uploading file: {ex.Message}";
                return result;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    _logger.LogWarning("DeleteFileAsync called with empty file path");
                    return false;
                }

                var fullPath = GetFullPath(filePath);
                
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found for deletion: {FilePath}", fullPath);
                    return false;
                }

                await Task.Run(() => File.Delete(fullPath));
                _logger.LogInformation("File deleted successfully: {FilePath}", fullPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public bool FileExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var fullPath = GetFullPath(filePath);
            return File.Exists(fullPath);
        }

        public string GetFullPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;

            // Remove leading slash if present
            relativePath = relativePath.TrimStart('/');
            
            return Path.Combine(_uploadsBasePath, relativePath);
        }

        public string? ValidateFile(IFormFile file, FileUploadOptions options)
        {
            if (file == null || file.Length == 0)
            {
                return "No file provided or file is empty.";
            }

            // Check file size
            if (file.Length > options.MaxFileSize)
            {
                var maxSizeMB = options.MaxFileSize / (1024.0 * 1024.0);
                return $"File size exceeds maximum allowed size of {maxSizeMB:F2} MB.";
            }

            // Check file extension
            if (options.AllowedExtensions != null && options.AllowedExtensions.Length > 0)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (string.IsNullOrEmpty(extension))
                {
                    return "File must have an extension.";
                }

                if (!options.AllowedExtensions.Contains(extension))
                {
                    var allowedExts = string.Join(", ", options.AllowedExtensions);
                    return $"File type not allowed. Allowed types: {allowedExts}";
                }
            }

            return null; // Valid
        }

        /// <summary>
        /// Sanitize filename by removing invalid characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());
            
            // Replace spaces with underscores
            sanitized = sanitized.Replace(" ", "_");
            
            // Limit length
            if (sanitized.Length > 50)
            {
                sanitized = sanitized.Substring(0, 50);
            }

            return sanitized;
        }
    }
}

