using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ChatServer.Models;
using ChatServer.Repositories;
using System;

namespace ChatServer.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();
        private readonly ILogger<ChatHub> _logger;
        private readonly IMessageRepository _messageRepository;

        public ChatHub(ILogger<ChatHub> logger, IMessageRepository messageRepository)
        {
            _logger = logger;
            _messageRepository = messageRepository;
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
                await Clients.Others.SendAsync("UserDisconnected", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string receiverUserId, string message)
        {
            var senderUserId = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            _logger.LogInformation($"Message from {senderUserId} to {receiverUserId}: {message}");

            // Save message to database
            await _messageRepository.SaveMessageAsync(new Message
            {
                SenderId = senderUserId,
                ReceiverId = receiverUserId,
                Content = message,
                IsBroadcast = false
            });

            if (UserConnections.TryGetValue(receiverUserId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderUserId, message);
                // Also send to sender to confirm delivery
                await Clients.Caller.SendAsync("ReceiveMessage", "You to " + receiverUserId, message);
            }
            else
            {
                _logger.LogWarning($"User {receiverUserId} is not connected.");
                await Clients.Caller.SendAsync("ReceiveMessage", "System", $"User {receiverUserId} is not currently online. Message saved.");
            }
        }

        public async Task BroadcastMessage(string senderUserId, string message)
        {
            _logger.LogInformation($"Broadcast message from {senderUserId}: {message}");
            
            // Save broadcast message to database
            await _messageRepository.SaveMessageAsync(new Message
            {
                SenderId = senderUserId,
                Content = message,
                IsBroadcast = true
            });
            
            await Clients.All.SendAsync("ReceiveMessage", senderUserId, message);
        }
        
        public async Task<List<string>> GetMessageHistory()
        {
            var userId = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            _logger.LogInformation($"User {userId} requested message history");
            
            var messages = await _messageRepository.GetUserMessagesAsync(userId);
            
            return messages.Select(m => 
                m.IsBroadcast 
                    ? $"[{m.Timestamp:g}] {m.SenderId} (Broadcast): {m.Content}"
                    : $"[{m.Timestamp:g}] {(m.SenderId == userId ? "You" : m.SenderId)} to {(m.ReceiverId == userId ? "You" : m.ReceiverId)}: {m.Content}"
            ).ToList();
        }
    }
}