namespace WebFindLove.Models.Services.NotificationService.Dto
{
    /// <summary>
    /// DTO cho Notification
    /// </summary>
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Guid? SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAvatar { get; set; }
        public Guid ReceiverId { get; set; }
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public string? Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty; // "2 phút trước", "1 giờ trước"
    }
}

