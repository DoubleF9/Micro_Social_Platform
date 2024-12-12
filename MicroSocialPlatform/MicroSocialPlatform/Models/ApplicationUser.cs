using Microsoft.AspNetCore.Identity;

namespace MicroSocialPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Description { get; set; }
        public bool? IsPublic { get; set; }
    }
}
