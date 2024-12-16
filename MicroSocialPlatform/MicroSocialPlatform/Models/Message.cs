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

        public string? ReceiverId { get; set; } // For direct messages

        public int? GroupId { get; set; } // For group messages

        [Required]
        [StringLength(500)]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public virtual ApplicationUser? Sender { get; set; }
        public virtual ApplicationUser? Receiver { get; set; }
        public virtual Group? Group { get; set; }
    }
}












