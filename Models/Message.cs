using System;
using System.ComponentModel.DataAnnotations;

namespace ChatServer.Models
{
    public class Message
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string SenderId { get; set; }
        
        public string ReceiverId { get; set; } // Null for broadcast messages
        
        [Required]
        public string Content { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public bool IsBroadcast { get; set; }
    }
}