using Microsoft.EntityFrameworkCore;
using WebFindLove.Models.Entities;
using WebFindLove.Models.UnitOfWork;

namespace WebFindLove.Models.Repositories.ConversationParticipantRepo
{
    /// <summary>
    /// Repository implementation cho ConversationParticipant entity
    /// </summary>
    public class ConversationParticipantRepository : GenericRepository<ConversationParticipant, Guid>, IConversationParticipantRepository
    {
        public ConversationParticipantRepository(AppDbContext context) : base(context) { }

        public async Task<List<ConversationParticipant>> GetConversationParticipantsAsync(Guid conversationId)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.User)
                .Where(cp => cp.ConversationId == conversationId)
                .ToListAsync();
        }

        public async Task<bool> IsParticipantAsync(Guid conversationId, Guid userId)
        {
            return await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        }

        public async Task<ConversationParticipant?> GetParticipantAsync(Guid conversationId, Guid userId)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.User)
                .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        }

        public async Task UpdateLastReadAsync(Guid conversationId, Guid userId)
        {
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

            if (participant != null)
            {
                participant.LastReadAt = DateTime.UtcNow;
                _context.ConversationParticipants.Update(participant);
            }
        }
    }
}

