using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSocialPlatform.Controllers
{
    public class FollowsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public FollowsController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var followers = await db.Follows
                .Where(f => f.FollowedId == user.Id && f.IsAccepted)
                .Include(f => f.Follower)
                .ToListAsync();

            var followRequests = await db.Follows
                .Where(f => f.FollowedId == user.Id && !f.IsAccepted)
                .Include(f => f.Follower)
                .ToListAsync();

            var following = await db.Follows
                .Where(f => f.FollowerId == user.Id && f.IsAccepted)
                .Include(f => f.Followed)
                .ToListAsync();

            ViewBag.Followers = followers;
            ViewBag.FollowRequests = followRequests;
            ViewBag.Following = following;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendFollowRequest(string followedId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Check if a follow request already exists
            var existingFollow = await db.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == user.Id && f.FollowedId == followedId);

            if (existingFollow != null)
            {
                TempData["message"] = "Follow request already sent.";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Profile", "Users", new { id = followedId });
            }

            // Check if the followed user's profile is public
            var followedUser = await _userManager.FindByIdAsync(followedId);
            if (followedUser == null)
            {
                return NotFound();
            }

            var follow = new Follow
            {
                FollowerId = user.Id,
                FollowedId = followedId,
                IsAccepted = followedUser.IsPublic // Automatically accept if the profile is public
            };

            db.Follows.Add(follow);
            await db.SaveChangesAsync();

            TempData["message"] = follow.IsAccepted ? "You are now following this user." : "Follow request sent.";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Profile", "Users", new { id = followedId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFollowRequest(string followerId, string followedId)
        {
            var follow = await db.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);
            if (follow == null)
            {
                return NotFound();
            }

            follow.IsAccepted = true;
            await db.SaveChangesAsync();

            TempData["message"] = "Follow request accepted.";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectFollowRequest(string followerId, string followedId)
        {
            var follow = await db.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);
            if (follow == null)
            {
                return NotFound();
            }

            db.Follows.Remove(follow);
            await db.SaveChangesAsync();

            TempData["message"] = "Follow request rejected.";
            TempData["messageType"] = "alert-danger";

            return RedirectToAction("Index");
        }
    }
}




