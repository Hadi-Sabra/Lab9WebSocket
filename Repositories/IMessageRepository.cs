using ChatServer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatServer.Repositories
{
    public interface IMessageRepository
    {
        Task<Message> SaveMessageAsync(Message message);
        Task<List<Message>> GetUserMessagesAsync(string userId);
        Task<List<Message>> GetChatHistoryAsync(string userId, string recipientId);
    }
}