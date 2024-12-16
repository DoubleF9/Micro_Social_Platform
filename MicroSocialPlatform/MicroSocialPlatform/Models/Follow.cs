using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MicroSocialPlatform.Models
{
    public class Follow
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }
        public string? FollowerId { get; set; }
        public string? FollowedId { get; set; }

        public virtual ApplicationUser? Follower { get; set; }

        public virtual ApplicationUser? Followed { get; set; }

        public bool IsAccepted { get; set; } = false;
    }
}
