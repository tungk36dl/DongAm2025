using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.Entities;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.ConversationRepo
{
    /// <summary>
    /// Repository implementation cho Conversation entity
    /// </summary>
    public class ConversationRepository : GenericRepository<Conversation, Guid>, IConversationRepository
    {
        public ConversationRepository(AppDbContext context) : base(context) { }

        public async Task<Conversation?> FindPrivateConversationAsync(Guid userId1, Guid userId2)
        {
            return await _context.Conversations
                .Include(c => c.Participants)
                .Where(c => c.Type == "private" &&
                    c.Participants!.Any(p => p.UserId == userId1) &&
                    c.Participants!.Any(p => p.UserId == userId2))
                .FirstOrDefaultAsync();
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(Guid userId)
        {
            return await _context.Conversations
                .Include(c => c.Participants!)
                    .ThenInclude(p => p.User)
                .Where(c => c.Participants!.Any(p => p.UserId == userId))
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Conversation?> GetConversationWithDetailsAsync(Guid conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Participants!)
                    .ThenInclude(p => p.User)
                .Include(c => c.Messages!.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task UpdateLastMessageAsync(Guid conversationId, string lastMessage)
        {
            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.LastMessage = lastMessage;
                conversation.LastMessageAt = DateTime.UtcNow;
                conversation.UpdatedAt = DateTime.UtcNow;
                _context.Conversations.Update(conversation);
            }
        }
    }
}

