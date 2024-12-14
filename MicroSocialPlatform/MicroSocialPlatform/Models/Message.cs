using System;
using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }

        [Required]
        public string ReceiverId { get; set; }

        [Required]
        [StringLength(500)]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
