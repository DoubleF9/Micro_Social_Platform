using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MicroSocialPlatform.Controllers
{
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public GroupsController(
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

        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }
            var groups = await db.Groups
                .Include(g => g.Moderator)
                .ToListAsync();

            return View(groups);
        }

        [Authorize(Roles = "User,Editor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            var groups = from g in db.Groups.Include(g => g.Moderator)
                         select g;

            if (!string.IsNullOrEmpty(query))
            {
                groups = groups.Where(g => g.Name.Contains(query) || g.Description.Contains(query));
            }

            return View("Index", await groups.ToListAsync());
        }

        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> Show(int id)
        {
            var group = await db.Groups
                .Include(g => g.Moderator)
                .Include(g => g.UserGroups)
                .ThenInclude(ug => ug.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userGroup = group.UserGroups.FirstOrDefault(ug => ug.UserId == currentUser.Id && ug.Status == true);
            ViewBag.IsAcceptedMember = userGroup != null;

            return View(group);
        }


        [Authorize(Roles = "User,Editor,Admin")]
        [HttpPost]
        public async Task<IActionResult> JoinGroupRequest(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var existingRequest = await db.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == user.Id && ug.GroupId == groupId);

            if (existingRequest != null)
            {
                TempData["message"] = "You have already requested to join this group.";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", new { id = groupId });
            }

            var userGroup = new UserGroup
            {
                UserId = user.Id,
                GroupId = groupId,
                Status = false // Pending approval
            };

            db.UserGroups.Add(userGroup);
            await db.SaveChangesAsync();

            TempData["message"] = "Join request sent.";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Show", new { id = groupId });
        }

        [Authorize(Roles = "User,Editor,Admin")]
        [HttpPost]
        public async Task<IActionResult> ApproveRequest(string userId, int groupId)
        {
            var userGroup = await db.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId);

            if (userGroup == null)
            {
                return NotFound();
            }

            userGroup.Status = true; // Approved
            await db.SaveChangesAsync();

            TempData["message"] = "Join request approved.";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Show", new { id = groupId });
        }

        [Authorize(Roles = "User,Editor,Admin")]
        [HttpPost]
        public async Task<IActionResult> RejectRequest(string userId, int groupId)
        {
            var userGroup = await db.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId);

            if (userGroup == null)
            {
                return NotFound();
            }

            db.UserGroups.Remove(userGroup);
            await db.SaveChangesAsync();

            TempData["message"] = "Join request rejected.";
            TempData["messageType"] = "alert-danger";

            return RedirectToAction("Show", new { id = groupId });
        }

        [Authorize(Roles = "User,Editor,Admin")]
        [HttpPost]
        public async Task<IActionResult> KickMember(string userId, int groupId)
        {
            var group = await db.Groups.FindAsync(groupId);
            if (group == null || group.ModeratorId != _userManager.GetUserId(User))
            {
                return Forbid();
            }
            var userGroup = await db.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GroupId == groupId);

            if (userGroup == null)
            {
                return NotFound();
            }

            db.UserGroups.Remove(userGroup);
            await db.SaveChangesAsync();

            TempData["message"] = "Member kicked out.";
            TempData["messageType"] = "alert-danger";

            return RedirectToAction("Show", new { id = groupId });
        }

        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var group = await db.Groups.FindAsync(id);
            if (group == null || group.ModeratorId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return View(group);
        }

        [Authorize(Roles = "User,Editor,Admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Group group, IFormFile? Image)
        {
            var existingGroup = await db.Groups.FindAsync(id);
            if (existingGroup == null || existingGroup.ModeratorId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            if (Image != null && Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                var fileExtension = Path.GetExtension(Image.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("GroupImage", "The file needs to be a jpg, jpeg or png.");
                    return View(group);
                }

                var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
                var databaseFileName = "/images/" + Image.FileName;

                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }
                ModelState.Remove(nameof(group.Image));
                existingGroup.Image = databaseFileName;
            }

            if (ModelState.IsValid)
            {
                existingGroup.Name = group.Name;
                existingGroup.Description = group.Description;

                db.Update(existingGroup);
                await db.SaveChangesAsync();

                TempData["message"] = "Group updated successfully.";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Show", new { id = existingGroup.Id });
            }

            return View(group);
        }

        [Authorize(Roles = "User,Editor,Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var group = await db.Groups
                .Include(g => g.UserGroups)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null || group.ModeratorId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            // Delete related UserGroup entries
            db.UserGroups.RemoveRange(group.UserGroups);

            // Delete the group
            db.Groups.Remove(group);
            await db.SaveChangesAsync();

            TempData["message"] = "Group and related user entries deleted successfully.";
            TempData["messageType"] = "alert-success";

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult New()
        {
            Group group = new Group();
            return View(group);
        }

        [Authorize(Roles = "User,Editor,Admin")]
        [HttpPost]
        public async Task<IActionResult> New(Group group, IFormFile? Image)
        {
            // adaug imaginea in folder si in tabel
            group.Image = "/images/" + "default_group_pic.png"; //img default

            if (Image != null && Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                var fileExtension = Path.GetExtension(Image.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("GroupImage", "The file needs to be a jpg, jpeg or png.");
                    return View(group);
                }

                var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
                var databaseFileName = "/images/" + Image.FileName;

                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }
                ModelState.Remove(nameof(group.Image));
                group.Image = databaseFileName;
            }
            // daca nu am bagat imagine, ia pe aia default

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                group.ModeratorId = userId;

                db.Groups.Add(group);
                db.SaveChanges();

                UserGroup usergroup = new UserGroup();
                usergroup.UserId = userId;
                usergroup.GroupId = group.Id;
                usergroup.Status = true;
                db.UserGroups.Add(usergroup);
                db.SaveChanges();

                TempData["message"] = "The group has been created.";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");

            }
            else
            {
                return View(group);
            }

        }
        [Authorize(Roles = "User,Editor,Admin")]
        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userGroup = await db.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == currentUser.Id && ug.GroupId == groupId && ug.Status == true);

            if (userGroup == null)
            {
                TempData["Error"] = "You are not an accepted member of this group.";
                return RedirectToAction("GroupMessages", new { groupId });
            }

            db.UserGroups.Remove(userGroup);
            await db.SaveChangesAsync();

            TempData["Success"] = "You have left the group.";
            return RedirectToAction("Index", "Groups");
        }

    }
}