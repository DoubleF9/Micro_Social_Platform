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

            var messages = await db.Messages
                .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == currentUser.Id))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            if (messages == null)
            {
                messages = new List<Message>();
            }

            ViewBag.ReceiverId = userId;
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

            var messages = await db.Messages
                .Where(m => m.GroupId == groupId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            if (messages == null)
            {
                messages = new List<Message>();
            }

            ViewBag.GroupId = groupId;
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
    }
}
