using Microsoft.AspNetCore.Mvc;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSocialPlatform.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return View(new List<ApplicationUser>());
            }

            var users = _context.Users
                          .Where(u => u.FirstName.Contains(query) || u.LastName.Contains(query) || (u.FirstName + " " + u.LastName).Contains(query))
                          .ToList();

            return View(users);
        }

        [HttpGet]
        public IActionResult Profile(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var isPublicProfile = true; // Assume a property or logic to check if the profile is public

            if (isPublicProfile)
            {
                return View(user);
            }
            else
            {
                var basicInfo = new
                {
                    user.FirstName,
                    user.LastName,
                    user.Description,
                    user.ProfilePicture
                };
                return View("BasicProfile", basicInfo);
            }
        }

        // GET: Users/Messages
        public async Task<IActionResult> Messages()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var messages = await _context.Messages
                .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
                .ToListAsync();

            return View(messages);
        }

        // GET: Users/CreateMessage
        [HttpGet]
        public IActionResult CreateMessage()
        {
            return View();
        }

        // POST: Users/CreateMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMessage(Message message)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                message.SenderId = user.Id;
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Messages));
            }
            return View(message);
        }
    }
}
