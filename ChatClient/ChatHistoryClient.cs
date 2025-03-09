using ChatService;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChatClient
{
    public class ChatHistoryClient
    {
        private readonly ChatHistory.ChatHistoryClient _client;

        public ChatHistoryClient(string serverUrl)
        {
            var channel = GrpcChannel.ForAddress(serverUrl, new GrpcChannelOptions
            {
                HttpHandler = new GrpcWebHandler(new HttpClientHandler())
            });
            
            _client = new ChatHistory.ChatHistoryClient(channel);
        }

        public async Task<List<string>> GetChatHistoryAsync(string userId, string recipientId = null)
        {
            try
            {
                var request = new GetChatHistoryRequest
                {
                    UserId = userId,
                    RecipientId = recipientId ?? string.Empty
                };

                var response = await _client.GetChatHistoryAsync(request);
                var formattedMessages = new List<string>();

                foreach (var message in response.Messages)
                {
                    string formattedMessage;
                    var timestamp = DateTime.Parse(message.Timestamp);
                    
                    if (message.IsBroadcast)
                    {
                        formattedMessage = $"[{timestamp:g}] {message.SenderId} (Broadcast): {message.Content}";
                    }
                    else
                    {
                        var direction = message.SenderId == userId ? "You → " : $"{message.SenderId} → ";
                        var recipient = message.ReceiverId == userId ? "You" : message.ReceiverId;
                        formattedMessage = $"[{timestamp:g}] {direction}{recipient}: {message.Content}";
                    }
                    
                    formattedMessages.Add(formattedMessage);
                }

                return formattedMessages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching chat history: {ex.Message}");
                return new List<string> { "Error fetching chat history" };
            }
        }
    }
}