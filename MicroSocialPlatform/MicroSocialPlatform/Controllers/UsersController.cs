using Microsoft.AspNetCore.Mvc;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MicroSocialPlatform.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [Authorize(Roles = "User,Editor,Admin")]
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

        [Authorize(Roles = "User,Editor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Profile(string id)
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["Message"];
                ViewBag.Alert = TempData["Alert"];
            }

            var user = await _context.Users
                .Include(u => u.Posts)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.IsPublic)
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

        [Authorize(Roles = "User,Editor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || (user.Id != id && !User.IsInRole("Admin")))
            {
                TempData["Message"] = "You cannot edit other user's posts!";
                TempData["Alert"] = "alert-danger";
                return RedirectToAction("Index");
            }

            var profileUser = await _context.Users.FindAsync(id);
            if (profileUser == null)
            {
                return NotFound();
            }

            return View(profileUser);
        }

        // POST: Users/EditProfile
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> Edit(string id, ApplicationUser model, IFormFile ProfilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || (user.Id != id && !User.IsInRole("Admin")))
            {
                TempData["Message"] = "You do not have permission to edit posts created by other users.";
                TempData["Alert"] = "alert-danger";

                return RedirectToAction("Index");
            }

            var profileUser = await _context.Users.FindAsync(id);
            if (profileUser == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                profileUser.FirstName = model.FirstName;
                profileUser.LastName = model.LastName;
                profileUser.Description = model.Description;
                profileUser.IsPublic = model.IsPublic;

                if (ProfilePicture != null && ProfilePicture.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(ProfilePicture.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("ProfilePicture", "The file must be an image (jpg, jpeg, png, gif).");
                        return View(profileUser);
                    }

                    var storagePath = Path.Combine(_env.WebRootPath, "profile_pictures", ProfilePicture.FileName);
                    var databaseFileName = "/profile_pictures/" + ProfilePicture.FileName;

                    var directory = Path.GetDirectoryName(storagePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var fileStream = new FileStream(storagePath, FileMode.Create))
                    {
                        await ProfilePicture.CopyToAsync(fileStream);
                    }

                    profileUser.ProfilePicture = databaseFileName;
                }

                var result = await _userManager.UpdateAsync(profileUser);
                if (result.Succeeded)
                {
                    TempData["Message"] = "Profile edited successfully!";
                    TempData["messageType"] = "alert-success";
                    return RedirectToAction(nameof(Profile), new { id = profileUser.Id });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(profileUser);
        }

        // GET: Users/Messages
        [Authorize(Roles = "User,Editor,Admin")]
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
        [Authorize(Roles = "User,Editor,Admin")]
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
