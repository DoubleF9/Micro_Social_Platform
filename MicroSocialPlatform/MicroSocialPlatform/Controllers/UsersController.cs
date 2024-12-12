using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MicroSocialPlatform.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;

        public UsersController(ApplicationDbContext context)
        {
            db = context;
        }

        [HttpGet]
        public IActionResult Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return View(new List<ApplicationUser>());
            }

            var users = db.Users
                          .Where(u => u.FirstName.Contains(query) || u.LastName.Contains(query) || (u.FirstName + " " + u.LastName).Contains(query))
                          .ToList();

            return View(users);
        }

        [HttpGet]
        public IActionResult Profile(string id)
        {
            var user = db.Users.FirstOrDefault(u => u.Id == id);
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
    }
}
