using ChatServer.Data;
using ChatServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatServer.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ChatDbContext _context;

        public MessageRepository(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<Message> SaveMessageAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<List<Message>> GetUserMessagesAsync(string userId)
        {
            return await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId || m.IsBroadcast)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<List<Message>> GetChatHistoryAsync(string userId, string recipientId)
        {
            return await _context.Messages
                .Where(m => 
                    (m.SenderId == userId && m.ReceiverId == recipientId) || 
                    (m.SenderId == recipientId && m.ReceiverId == userId) ||
                    (m.IsBroadcast && (m.SenderId == userId || m.SenderId == recipientId)))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }
    }
}