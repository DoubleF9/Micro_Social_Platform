using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace MicroSocialPlatform.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The group name is required.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "The description is required.")]
        public string Description { get; set; }

        public string? Image { get; set; }

        public string? ModeratorId { get; set; }

        public virtual ApplicationUser? Moderator { get; set; }

        //public virtual ICollection<Post>? Posts { get; set; }


        public virtual ICollection<UserGroup>? UserGroups { get; set; }


    }
}
