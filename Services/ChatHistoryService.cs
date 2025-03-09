using System;
using System.Linq;
using System.Threading.Tasks;
using ChatServer.Repositories;
using ChatService;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace ChatServer.Services
{
    public class ChatHistoryService : ChatHistory.ChatHistoryBase
    {
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<ChatHistoryService> _logger;

        public ChatHistoryService(IMessageRepository messageRepository, ILogger<ChatHistoryService> logger)
        {
            _messageRepository = messageRepository;
            _logger = logger;
        }

        public override async Task<GetChatHistoryResponse> GetChatHistory(GetChatHistoryRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Getting chat history for user {request.UserId}");
            
            var response = new GetChatHistoryResponse();
            
            try
            {
                var messages = string.IsNullOrEmpty(request.RecipientId)
                    ? await _messageRepository.GetUserMessagesAsync(request.UserId)
                    : await _messageRepository.GetChatHistoryAsync(request.UserId, request.RecipientId);

                response.Messages.AddRange(messages.Select(m => new ChatMessage
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId ?? string.Empty,
                    Content = m.Content,
                    Timestamp = m.Timestamp.ToString("o"),
                    IsBroadcast = m.IsBroadcast
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat history");
                throw new RpcException(new Status(StatusCode.Internal, "Error retrieving chat history"));
            }
            
            return response;
        }
    }
}