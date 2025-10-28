using System.ComponentModel.DataAnnotations;

namespace WebFindLove.Models.Services.NotificationService.Dto
{
    /// <summary>
    /// DTO để tạo Notification mới
    /// </summary>
    public class NotificationCreateDto
    {
        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        [StringLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự")]
        public string Message { get; set; } = string.Empty;

        public Guid? SenderId { get; set; }

        [Required(ErrorMessage = "Người nhận là bắt buộc")]
        public Guid ReceiverId { get; set; }

        [StringLength(255, ErrorMessage = "Link không được vượt quá 255 ký tự")]
        public string? Link { get; set; }

        [StringLength(50, ErrorMessage = "Loại thông báo không được vượt quá 50 ký tự")]
        public string? Type { get; set; }
    }
}

