namespace WebFindLove.Models.Services.UserService.Dto
{
    using WebFindLove.Models.Services;

    public class UserSearch : SearchBase
    {
        public string? Query { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public int? AgeMin { get; set; }
        public int? AgeMax { get; set; }
        public string? Gender { get; set; }
    }
}
