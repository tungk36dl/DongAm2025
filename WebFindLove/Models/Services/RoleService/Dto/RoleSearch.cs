using WebFindLove.Models.Services;

namespace WebFindLove.Models.Services.RoleService.Dto
{
    public class RoleSearch : SearchBase
    {
        public string? Query { get; set; }
        public bool? IsActive { get; set; }
    }
}

