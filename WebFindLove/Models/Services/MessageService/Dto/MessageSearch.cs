namespace WebFindLove.Models.Services.MessageService.Dto
{
    public class MessageSearch : SearchBase
    {
        public Guid UserId1 { get; set; }
        public Guid UserId2 { get; set; }
    }
}
