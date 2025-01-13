using Microsoft.AspNetCore.Mvc;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace MicroSocialPlatform.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        // GET: Messages/DirectMessages
        public async Task<IActionResult> DirectMessages(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var receiver = await _userManager.FindByIdAsync(userId);
            if (receiver == null)
            {
                return NotFound();
            }

            ViewBag.ReceiverId = userId;
            ViewBag.ReceiverFirstName = receiver.FirstName;
            ViewBag.ReceiverLastName = receiver.LastName;
            ViewBag.ReceiverProfilePicture = receiver.ProfilePicture;
            ViewBag.CurrentUserId = currentUser.Id;

            var messages = await db.Messages
                .Include(m => m.Sender) // Include sender information
                .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == currentUser.Id))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return View(messages);
        }



        // GET: Messages/GroupMessages
        public async Task<IActionResult> GroupMessages(int groupId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var group = await db.Groups
                .Include(g => g.UserGroups)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return NotFound();
            }

            // Check if the current user is an accepted member of the group
            var userGroup = group.UserGroups.FirstOrDefault(ug => ug.UserId == currentUser.Id && ug.Status == true);
            if (userGroup == null)
            {
                TempData["Error"] = "You are not an accepted member of this group.";
                return RedirectToAction("Index", "Groups");
            }

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.Name;
            ViewBag.GroupIcon = group.Image;
            ViewBag.CurrentUserId = currentUser.Id;

            var messages = await db.Messages
                .Include(m => m.Sender) // Include sender information
                .Where(m => m.GroupId == groupId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return View(messages);
        }




        // POST: Messages/SendDirectMessage
        [HttpPost]
        public async Task<IActionResult> SendDirectMessage(string receiverId, string content)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            if (string.IsNullOrEmpty(content))
            {
                TempData["Error"] = "Message content is required.";
                return RedirectToAction("DirectMessages", new { userId = receiverId });
            }

            var message = new Message
            {
                SenderId = currentUser.Id,
                ReceiverId = receiverId,
                Content = content
            };

            db.Messages.Add(message);
            await db.SaveChangesAsync();

            return RedirectToAction("DirectMessages", new { userId = receiverId });
        }

        // POST: Messages/SendGroupMessage
        [HttpPost]
        public async Task<IActionResult> SendGroupMessage(int groupId, string content)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            if (string.IsNullOrEmpty(content))
            {
                TempData["Error"] = "Message content is required.";
                return RedirectToAction("GroupMessages", new { groupId = groupId });
            }

            var message = new Message
            {
                SenderId = currentUser.Id,
                GroupId = groupId,
                Content = content
            };

            db.Messages.Add(message);
            await db.SaveChangesAsync();

            return RedirectToAction("GroupMessages", new { groupId = groupId });
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var message = await db.Messages.FindAsync(id);
            if (message == null)
            {
                TempData["Error"] = "Message not found.";
                return RedirectToAction("DirectMessages", new { userId = message?.ReceiverId });
            }

            if (message.SenderId != currentUser.Id && !User.IsInRole("Admin"))
            {
                TempData["Error"] = "You are not authorized to delete this message.";
                return Forbid();
            }

            db.Messages.Remove(message);
            await db.SaveChangesAsync();

            TempData["Success"] = "Message deleted successfully.";

            if (!string.IsNullOrEmpty(message.ReceiverId))
            {
                return RedirectToAction("DirectMessages", new { userId = message.ReceiverId });
            }
            else if (message.GroupId.HasValue)
            {
                return RedirectToAction("GroupMessages", new { groupId = message.GroupId.Value });
            }

            return RedirectToAction("Index", "Home");
        }



        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditMessage(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var message = await db.Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            if (message.SenderId != currentUser.Id)
            {
                return Forbid();
            }

            return PartialView("_EditMessagePartial", message);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditMessage(int id, string content)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var message = await db.Messages.FindAsync(id);
            if (message == null)
            {
                TempData["Error"] = "Message not found.";
                return RedirectToAction("DirectMessages", new { userId = message?.ReceiverId });
            }

            if (message.SenderId != currentUser.Id)
            {
                TempData["Error"] = "You are not authorized to edit this message.";
                return Forbid();
            }

            if (string.IsNullOrEmpty(content))
            {
                TempData["Error"] = "Message content is required.";
                return RedirectToAction("DirectMessages", new { userId = message.ReceiverId });
            }

            message.Content = content;
            await db.SaveChangesAsync();

            TempData["Success"] = "Message edited successfully.";

            if (!string.IsNullOrEmpty(message.ReceiverId))
            {
                return RedirectToAction("DirectMessages", new { userId = message.ReceiverId });
            }
            else if (message.GroupId.HasValue)
            {
                return RedirectToAction("GroupMessages", new { groupId = message.GroupId.Value });
            }

            return RedirectToAction("Index", "Home");
        }








    }
}