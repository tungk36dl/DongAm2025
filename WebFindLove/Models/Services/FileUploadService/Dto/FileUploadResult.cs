namespace WebFindLove.Models.Services.FileUploadService.Dto
{
    /// <summary>
    /// Result of file upload operation
    /// </summary>
    public class FileUploadResult
    {
        /// <summary>
        /// Indicates if upload was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The relative path to the uploaded file (e.g., "avatars/filename.jpg")
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// The filename only (e.g., "filename.jpg")
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// The full server path to the file
        /// </summary>
        public string? FullPath { get; set; }

        /// <summary>
        /// Error message if upload failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Content type of the uploaded file
        /// </summary>
        public string? ContentType { get; set; }
    }
}

