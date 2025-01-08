using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroSocialPlatform.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _env;

        public PostsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment env)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _env = env;
        }
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["Message"];
                ViewBag.Alert = TempData["Alert"];
            }
            var posts = db.Posts.Include("User").ToList();
            ViewBag.Posts = posts;
            return View();
        }
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show(int id)
        {
            var post = db.Posts
                         .Include(p => p.User)
                         .Include(p => p.Comments)
                         .ThenInclude(c => c.User)
                         .FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }
            post.Comments = post.Comments ?? new List<Comment>();
            SetAccessRights(post);
            return View(post);
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date = DateTime.Now;

            // Get the ID of the user posting the comment
            comment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Posts/Show/" + comment.PostId);
            }
            else
            {
                var post = db.Posts
                             .Include(p => p.User)
                             .Include(p => p.Comments)
                             .ThenInclude(c => c.User)
                             .FirstOrDefault(p => p.Id == comment.PostId);

                if (post == null)
                {
                    return NotFound();
                }

                SetAccessRights(post);

                return View(post);
            }
        }


        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult New()
        {
            return View();
        }
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> New(Post post, IFormFile? Image)
        {
            // Set default image if no image is uploaded
            post.Image = "/images/default_post_image.png";

            if (Image != null && Image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };

                var fileExtension = Path.GetExtension(Image.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("PostImage", "The file must be an image (jpg, jpeg, png, gif) or a video (mp4, mov).");
                    return View(post);
                }

                var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
                var databaseFileName = "/images/" + Image.FileName;

                var directory = Path.GetDirectoryName(storagePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }
                ModelState.Remove(nameof(post.Image));
                post.Image = databaseFileName;
            }

            if (ModelState.IsValid)
            {
                post.Date = DateTime.Now;
                post.UserId = _userManager.GetUserId(User);

                db.Posts.Add(post);
                await db.SaveChangesAsync();

                TempData["Message"] = "Post created successfully!";
                TempData["Alert"] = "alert-success";
                return RedirectToAction("Feed", "Users");
            }
            else
            {
                TempData["Message"] = "Failed to create post. Please check the form for errors.";
                TempData["Alert"] = "alert-danger";
                return View(post);
            }
        }




        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id)
        {
            var post = db.Posts.FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            if ((post.UserId == _userManager.GetUserId(User)) || User.IsInRole("Admin"))
            {
                return View(post);
            }
            else
            {
                TempData["Message"] = "You cannot edit other user's posts!";
                TempData["Alert"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> Edit(int id, Post requestPost, IFormFile? Image)
        {
            var post = db.Posts.Find(id);

            if (post == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if ((post.UserId == _userManager.GetUserId(User)) || User.IsInRole("Admin"))
                {
                    post.Content = requestPost.Content;
                    post.Date = DateTime.Now;

                    if (Image != null && Image.Length > 0)
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                        var fileExtension = Path.GetExtension(Image.FileName).ToLower();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("PostImage", "The file must be an image (jpg, jpeg, png, gif) or a video (mp4, mov).");
                            return View(post);
                        }

                        var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
                        var databaseFileName = "/images/" + Image.FileName;

                        var directory = Path.GetDirectoryName(storagePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        using (var fileStream = new FileStream(storagePath, FileMode.Create))
                        {
                            await Image.CopyToAsync(fileStream);
                        }

                        ModelState.Remove(nameof(post.Image));
                        post.Image = databaseFileName;
                    }
                    else
                    {
                        // Retain the existing image
                        post.Image = requestPost.Image;
                    }

                    TempData["Message"] = "Post edited successfully!";
                    TempData["Alert"] = "alert-success";
                    db.SaveChanges();
                    return RedirectToAction("Feed", "Users");
                }
                else
                {
                    TempData["Message"] = "You do not have permission to edit posts created by other users.";
                    TempData["Alert"] = "alert-danger";

                    return RedirectToAction("Feed", "Users");

                }
            }
            else
            {
                return View(post);
            }
        }


        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public ActionResult Delete(int id)
        {
            var post = db.Posts
                         .Include(p => p.Comments)
                         .FirstOrDefault(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            if ((post.UserId == _userManager.GetUserId(User)) || User.IsInRole("Admin"))
            {
                // Remove all comments associated with the post
                db.Comments.RemoveRange(post.Comments);

                // Remove the post
                db.Posts.Remove(post);
                db.SaveChanges();

                TempData["Message"] = "The post and its comments have been successfully deleted.";
                TempData["Alert"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Message"] = "You do not have permission to delete posts created by other users.";
                TempData["MessageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }




        private void SetAccessRights(Post post)
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Editor") || post.UserId == _userManager.GetUserId(User))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.UserCurent = _userManager.GetUserId(User);
            ViewBag.EsteAdmin = User.IsInRole("Admin");
        }


    }
}
