using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("Enter your UserID: ");
            string userId = Console.ReadLine()?.Trim() ?? "Guest"; // Handle null input

            // SignalR client
            var signalRUrl = "http://localhost:5001/chatHub";
            var connection = new HubConnectionBuilder()
                .WithUrl($"{signalRUrl}?userId={Uri.EscapeDataString(userId)}") // Escape for safety
                .Build();

            // gRPC client
            var grpcUrl = "http://localhost:5001";
            var chatHistoryClient = new ChatHistoryClient(grpcUrl);

            // Setup SignalR event handlers
            connection.On<string, string>("ReceiveMessage", (sender, message) =>
            {
                Console.WriteLine($"{sender}: {message}");
            });

            connection.On<string>("UserDisconnected", (disconnectedUser) =>
            {
                Console.WriteLine($"User {disconnectedUser} has disconnected.");
            });

            try
            {
                await connection.StartAsync();
                Console.WriteLine("Connected to the chat server.");

                // Load initial chat history using gRPC
                Console.WriteLine("Loading your message history...");
                var initialHistory = await chatHistoryClient.GetChatHistoryAsync(userId);
                if (initialHistory.Count > 0)
                {
                    Console.WriteLine("Your recent messages:");
                    foreach (var msg in initialHistory)
                    {
                        Console.WriteLine(msg);
                    }
                }
                else
                {
                    Console.WriteLine("No message history found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to server: {ex.Message}");
                return; // Exit if connection fails
            }

            while (true)
            {
                Console.WriteLine("\nCommands:");
                Console.WriteLine("1. Send Private Message (Format: /msg <UserId> <Message>)");
                Console.WriteLine("2. Broadcast Message (Format: /broadcast <Message>)");
                Console.WriteLine("3. View Chat History with Everyone (/history)");
                Console.WriteLine("4. View Chat History with User (Format: /history <UserId>)");
                Console.WriteLine("5. Exit (/exit)");
                Console.Write("\nEnter command: ");

                string input = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(input)) continue;

                if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase)) break;

                if (input.Equals("/history", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Use gRPC to get message history
                        var messages = await chatHistoryClient.GetChatHistoryAsync(userId);
                        Console.WriteLine("Chat History:");
                        foreach (var msg in messages)
                        {
                            Console.WriteLine(msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching chat history: {ex.Message}");
                    }
                    continue;
                }

                if (input.StartsWith("/history ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var parts = input.Split(" ", 2);
                        string recipientId = parts[1].Trim();
                        
                        // Use gRPC to get history with specific user
                        var messages = await chatHistoryClient.GetChatHistoryAsync(userId, recipientId);
                        Console.WriteLine($"Chat History with {recipientId}:");
                        foreach (var msg in messages)
                        {
                            Console.WriteLine(msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching chat history: {ex.Message}");
                    }
                    continue;
                }

                if (input.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = input.Split(" ", 3);
                    if (parts.Length < 3)
                    {
                        Console.WriteLine("Invalid format. Use: /msg <UserId> <Message>");
                        continue;
                    }

                    string recipient = parts[1].Trim();
                    string message = parts[2].Trim();

                    try
                    {
                        await connection.InvokeAsync("SendMessage", recipient, message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message: {ex.Message}");
                    }
                }
                else if (input.StartsWith("/broadcast ", StringComparison.OrdinalIgnoreCase))
                {
                    string message = input.Substring(11).Trim();

                    try
                    {
                        await connection.InvokeAsync("BroadcastMessage", userId, message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error broadcasting message: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid command. Try again.");
                }
            }

            await connection.StopAsync();
            Console.WriteLine("Disconnected from chat server.");
        }
    }
}