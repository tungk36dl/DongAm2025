using System.ComponentModel.DataAnnotations;

namespace WebFindLove.Models.Entity
{
    public abstract class DomainEntity<TKey>
    {
        [Key]
        public TKey Id { get; set; }
    }
}
