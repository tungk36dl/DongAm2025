namespace WebFindLove.Models.Services.FileUploadService.Dto
{
    /// <summary>
    /// Options for configuring file upload behavior
    /// </summary>
    public class FileUploadOptions
    {
        /// <summary>
        /// Subdirectory within wwwroot/uploads/ (e.g., "avatars", "documents")
        /// </summary>
        public string SubDirectory { get; set; } = "files";

        /// <summary>
        /// Allowed file extensions (e.g., [".jpg", ".png"])
        /// If null or empty, all extensions are allowed
        /// </summary>
        public string[]? AllowedExtensions { get; set; }

        /// <summary>
        /// Maximum file size in bytes (default: 5MB)
        /// </summary>
        public long MaxFileSize { get; set; } = 5 * 1024 * 1024; // 5MB

        /// <summary>
        /// Whether to generate a unique filename using GUID
        /// </summary>
        public bool GenerateUniqueFileName { get; set; } = true;

        /// <summary>
        /// Whether to preserve the original filename
        /// Only applies if GenerateUniqueFileName is false
        /// </summary>
        public bool PreserveOriginalFileName { get; set; } = false;

        /// <summary>
        /// Whether to overwrite existing file with the same name
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;
    }
}

