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
                .Include(m => m.Sender) // Include sender information
                .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == currentUser.Id))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            if (messages == null)
            {
                messages = new List<Message>();
            }

            ViewBag.ReceiverId = userId;
            ViewBag.CurrentUserId = currentUser.Id;
            ViewBag.CurrentUserName = currentUser.FirstName;

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
            //Verificam daca utilizatorul curent este membru al grupului
            var userGroup = await db.UserGroups
                .FirstOrDefaultAsync(ug => ug.UserId == currentUser.Id && ug.GroupId == groupId);
            if (userGroup == null)
            {
                TempData["Error"] = "You are not a member of this group.";
                return RedirectToAction("Index", "Groups");
            }

            var messages = await db.Messages
                .Include(m => m.Sender) // Include sender information
                .Where(m => m.GroupId == groupId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            if (messages == null)
            {
                messages = new List<Message>();
            }

            ViewBag.GroupId = groupId;
            ViewBag.CurrentUserId = currentUser.Id;
            ViewBag.CurrentUserName = currentUser.FirstName;

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
        return NotFound();
    }

    if (message.SenderId != currentUser.Id)
    {
        return Forbid();
    }

    db.Messages.Remove(message);
    await db.SaveChangesAsync();

    // Redirecționează utilizatorul înapoi la pagina de mesaje
    if (message.ReceiverId != null)
    {
        return RedirectToAction("DirectMessages", new { userId = message.ReceiverId });
    }
    else if (message.GroupId != null)
    {
        return RedirectToAction("GroupMessages", new { groupId = message.GroupId });
    }

    return RedirectToAction("Index", "Home");
}
        [HttpGet]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> EditMessage(int id)
        {
            var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }

            // Check if the current user is the owner of the message or an admin
            var currentUser = await _userManager.GetUserAsync(User);
            if (message.SenderId != currentUser.Id && !User.IsInRole("Admin"))
            {
                TempData["Message"] = "You do not have permission to edit this message";
                TempData["Alert"] = "alert-danger";

                if (message.ReceiverId != null)
                {
                    return RedirectToAction("DirectMessages", new { userId = message.ReceiverId });
                }
                else if (message.GroupId != null)
                {
                    return RedirectToAction("GroupMessages", new { groupId = message.GroupId });
                }
            }

            ViewBag.Message = message;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> EditMessage(int id, [Bind("Content")] Message requestMessage)
        {
            var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }

            // Check if the current user is the owner of the message or an admin
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || (message.SenderId != currentUser.Id && !User.IsInRole("Admin")))
            {
                TempData["Message"] = "You do not have permission to edit this message";
                TempData["Alert"] = "alert-danger";
                return RedirectToAction("Index", "Posts");
            }

            if (!ModelState.IsValid)
            {
                TempData["EditMessageError"] = "Message content is required";
                // Redirect the user back to the messages page
                if (message.ReceiverId != null)
                {
                    return RedirectToAction("DirectMessages", new { userId = message.ReceiverId });
                }
                else if (message.GroupId != null)
                {
                    return RedirectToAction("GroupMessages", new { groupId = message.GroupId });
                }
            }

            message.Content = requestMessage.Content;
            message.Timestamp = DateTime.Now;
            await db.SaveChangesAsync();

            // Redirect the user back to the messages page
            if (message.ReceiverId != null)
            {
                return RedirectToAction("DirectMessages", new { userId = message.ReceiverId });
            }
            else if (message.GroupId != null)
            {
                return RedirectToAction("GroupMessages", new { groupId = message.GroupId });
            }

            return RedirectToAction("Index", "Home");
        }
    }
}