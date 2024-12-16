using Microsoft.AspNetCore.Identity;

namespace MicroSocialPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public virtual ICollection<Post>? Posts { get; set; }
        public virtual ICollection<Comment>? Comments { get; set; }
        public virtual ICollection<Follow>? Following { get; set; }
        public virtual ICollection<Follow>? Followers { get; set; }
        public virtual ICollection<UserGroup>? UserGroups { get; set; }
    }
}
