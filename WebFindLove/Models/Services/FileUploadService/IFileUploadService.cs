using Microsoft.AspNetCore.Http;
using WebFindLove.Models.Services.FileUploadService.Dto;

namespace WebFindLove.Models.Services.FileUploadService
{
    /// <summary>
    /// Service for handling file uploads
    /// </summary>
    public interface IFileUploadService
    {
        /// <summary>
        /// Upload a file to the server
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <param name="options">Upload options</param>
        /// <returns>Result containing file path and metadata</returns>
        Task<FileUploadResult> UploadFileAsync(IFormFile file, FileUploadOptions options);

        /// <summary>
        /// Delete a file from the server
        /// </summary>
        /// <param name="filePath">Relative path to the file (e.g., "avatars/filename.jpg")</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Check if a file exists
        /// </summary>
        /// <param name="filePath">Relative path to the file</param>
        /// <returns>True if file exists</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// Get the full server path for a relative file path
        /// </summary>
        /// <param name="relativePath">Relative path (e.g., "avatars/filename.jpg")</param>
        /// <returns>Full server path</returns>
        string GetFullPath(string relativePath);

        /// <summary>
        /// Validate file before upload
        /// </summary>
        /// <param name="file">The file to validate</param>
        /// <param name="options">Upload options containing validation rules</param>
        /// <returns>Error message if validation fails, null if valid</returns>
        string? ValidateFile(IFormFile file, FileUploadOptions options);
    }
}

