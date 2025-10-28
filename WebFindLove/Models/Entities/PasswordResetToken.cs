using WebFindLove.Models.Entity;

namespace WebFindLove.Models.Entities
{
    public class PasswordResetToken : DomainEntity<Guid>
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public DateTime ExpiredAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
