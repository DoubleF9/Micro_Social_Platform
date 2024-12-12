namespace MicroSocialPlatform.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string ?Image { get; set; }
        public DateTime Date { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public ICollection<Comment>? Comments { get; set; }
    }
}
