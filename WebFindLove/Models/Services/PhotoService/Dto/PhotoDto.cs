namespace WebFindLove.Models.Services.PhotoService.Dto
{
    /// <summary>
    /// DTO cho Photo entity
    /// </summary>
    public class PhotoDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        
        // Related data
        public string? UserName { get; set; }
    }
}

