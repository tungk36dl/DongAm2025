namespace WebFindLove.Models.Services.PhotoService.Dto
{
    /// <summary>
    /// Search criteria cho Photo
    /// </summary>
    public class PhotoSearch : SearchBase
    {
        public Guid? UserId { get; set; }
        public bool? IsPrimary { get; set; }
        public bool? IsActive { get; set; }
    }
}

