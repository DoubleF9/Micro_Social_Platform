using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define composite primary key for Follow
            modelBuilder.Entity<Follow>()
                .HasKey(f => new { f.Id, f.FollowedId, f.FollowerId });

            // Define relationships with ApplicationUser
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Followed)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowedId);

            modelBuilder.Entity<UserGroup>()
               .HasKey(uc => new { uc.UserId, uc.GroupId });

            // definire relatii cu modelele Category si Article (FK)
            modelBuilder.Entity<UserGroup>()
                .HasOne(uc => uc.User)
                .WithMany(uc => uc.UserGroups)
                .HasForeignKey(uc => uc.UserId);
            modelBuilder.Entity<UserGroup>()
                .HasOne(uc => uc.Group)
                .WithMany(uc => uc.UserGroups)
                .HasForeignKey(uc => uc.GroupId);
        }
    }
}
