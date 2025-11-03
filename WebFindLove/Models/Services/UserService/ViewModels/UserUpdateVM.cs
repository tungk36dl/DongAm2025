using System.ComponentModel.DataAnnotations;

namespace WebFindLove.Models.Services.UserService.ViewModels
{
    public class UserUpdateVM : UserCreateVM
    {
        public Guid Id { get; set; }
        
        [Display(Name = "Free Profile Updates Left")]
        public int? FreeProfileUpdatesLeft { get; set; }
    }
}
