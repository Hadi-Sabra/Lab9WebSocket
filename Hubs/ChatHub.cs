using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ChatServer.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            string userId = Context.GetHttpContext()?.Request.Query["userId"];
            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections[userId] = Context.ConnectionId;
                _logger.LogInformation($"User {userId} connected with ConnectionId: {Context.ConnectionId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections.TryRemove(userId, out _);
                _logger.LogInformation($"User {userId} disconnected.");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string receiverUserId, string message)
        {
            _logger.LogInformation($"Message from {Context.ConnectionId} to {receiverUserId}: {message}");

            if (UserConnections.TryGetValue(receiverUserId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", receiverUserId, message);
            }
            else
            {
                _logger.LogWarning($"User {receiverUserId} is not connected.");
            }
        }

        public async Task BroadcastMessage(string senderUserId, string message)
        {
            _logger.LogInformation($"Broadcast message from {senderUserId}: {message}");
            await Clients.All.SendAsync("ReceiveMessage", senderUserId, message);
        }
        
        
    }
    
}
