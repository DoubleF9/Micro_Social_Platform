using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MicroSocialPlatform.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public CommentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Delete(int id)
        {
            // Find the comment by its ID
            Comment comm = db.Comments.Find(id);

            // Check if the current user is the owner of the comment or an admin
            if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                // Remove the comment from the database
                db.Comments.Remove(comm);
                db.SaveChanges();

                // Redirect to the post's details page
                return Redirect("/Posts/Show/" + comm.PostId);
            }
            else
            {
                // Set a message indicating the user does not have permission to delete the comment
                TempData["Message"] = "You do not have permission to delete this comment";
                TempData["Alert"] = "alert-danger";

                // Redirect to the index page of posts
                return RedirectToAction("Index", "Posts");
            }
        }
        //[HttpPost]
        //[Authorize(Roles = "User,Editor,Admin")]
        //public IActionResult Edit(Comment comment)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Comments.Update(comment);
        //        db.SaveChanges();
        //        return Redirect("/Posts/Show/" + comment.PostId);
        //    }

        //    // If the model state is invalid, return to the post's show page
        //    return Redirect("/Posts/Show/" + comment.PostId);
        //}
        [HttpGet]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id)
        {
            var comment = db.Comments.FirstOrDefault(c => c.Id == id);
            if (comment == null)
            {
                return NotFound();
            }

            // Check if the current user is the owner of the comment or an admin
            if (comment.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["Message"] = "You do not have permission to edit this comment";
                TempData["Alert"] = "alert-danger";
                return RedirectToAction("Index", "Posts");
            }

            ViewBag.Comment = comment;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id, [Bind("Content")] Comment requestComment)
        {
            var comment = db.Comments.FirstOrDefault(c => c.Id == id);
            if (comment == null)
            {
                return NotFound();
            }

            // Check if the current user is the owner of the comment or an admin
            if (comment.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                TempData["Message"] = "You do not have permission to edit this comment";
                TempData["Alert"] = "alert-danger";
                return RedirectToAction("Index", "Posts");
            }

            if (!ModelState.IsValid)
            {
                TempData["EditCommentError"] = "Comment content is required";
                return RedirectToAction("Show", "Posts", new { id = comment.PostId });
            }

            comment.Content = requestComment.Content;
            comment.Date = DateTime.Now;
            db.SaveChanges();
            return RedirectToAction("Show", "Posts", new { id = comment.PostId });
        }



    }
}
