using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsShoppingCart.Models.Data;
using CmsShoppingCart.Models.ViewModels.Pages;

namespace CmsShoppingCart.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PagesController : Controller
    {
        // GET: Admin/Pages
        public ActionResult Index()
        {
            // Declare list of PageVM
            List<PageViewModel> pagesList;

            
            using (Db db = new Db())
            {
                // Init the list
                pagesList = db.Pages.ToArray().OrderBy(x => x.Sorting).Select(x => new PageViewModel(x)).ToList();
            }

            //Return
            return View(pagesList);
        }

        [HttpGet]
        public ActionResult AddPage()
        {

            return View();
        }

        [HttpPost]
        public ActionResult AddPage(PageViewModel model)
        {
            // Check Model state
            if (!ModelState.IsValid)
                return View(model);
            // Declare slug
            using (Db db = new Db())
            {
                string slug;

                PageDTO dto = new PageDTO();
                dto.Title = model.Title;

                if (string.IsNullOrWhiteSpace(model.Slug))
                {
                    slug = model.Title.Replace(" ", "-").ToLower();
                }
                else
                {
                    slug = model.Slug.Replace(" ", "-").ToLower();
                }

                //Make sure title and slug are unique
                if (db.Pages.Any(x => x.Title == model.Title) || db.Pages.Any(x => x.Slug == slug))
                {
                    ModelState.AddModelError("", "That title or slug already exists");
                    return View(model);
                }
                // DTO the rest
                dto.Slug = slug;
                dto.Body = model.Body;
                dto.HasSidebar = model.HasSidebar;
                dto.Sorting = model.Sorting;
                //save DTO
                db.Pages.Add(dto);
                db.SaveChanges();
                //Set tempdata message & redirect
            }
            TempData["SM"] = "You have added a new page!";

            return RedirectToAction("AddPage");
        }

        [HttpGet]
        public ActionResult EditPage(int id)
        {
            // Declare pageVM
            PageViewModel model;

            using (Db db = new Db())
            {
                // Get the page
                PageDTO dto = db.Pages.Find(id);

                // Confirm page exists
                if (dto == null)
                {
                    return Content("The page does not exist.");
                }

                // Init pageVM
                model = new PageViewModel(dto);
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult EditPage(PageViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using (Db db = new Db())
            {
                int id = model.Id;

                string slug = "home";

                PageDTO dto = db.Pages.Find(id);

                dto.Title = model.Title;
                if (model.Slug != "home")
                {
                    if (string.IsNullOrWhiteSpace(model.Slug))
                    {
                        slug = model.Title.Replace(" ", "-").ToLower();
                    }
                    else
                    {
                        slug = model.Slug.Replace(" ", "-").ToLower();
                    }
                }

                // Make sure title and slug are unique
                if (db.Pages.Where(x => x.Id != id).Any(x => x.Title == model.Title) ||
                    db.Pages.Where(x => x.Id != id).Any(x => x.Slug == slug))
                {
                    ModelState.AddModelError("", "That title or slug already exists.");
                    return View(model);
                }

                dto.Slug = slug;
                dto.Body = model.Body;
                dto.HasSidebar = model.HasSidebar;

                db.SaveChanges();
            }
            TempData["SM"] = "You have edited the page!";
            return RedirectToAction("EditPage");
        }

        public ActionResult PageDetails(int id)
        {
            // declare pagevm
            PageViewModel model;

            using (Db db = new Db())
            {
                // get the page
                PageDTO dto = db.Pages.Find(id);
                // confirm page exists
                if (dto == null)
                    return Content("The page does not exist.");

                // init pagevm
                model = new PageViewModel(dto);
            }


            //redirect
            return View(model);
        }

        public ActionResult DeletePage(int id)
        {
            using (Db db = new Db())
            {
                PageDTO dto = db.Pages.Find(id);
                db.Pages.Remove(dto);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public void ReorderPages(int[] id)
        {
            using (Db db = new Db())
            {
                int count = 1;
                PageDTO dto;

                foreach (var pageId in id)
                {
                    dto = db.Pages.Find(pageId);
                    dto.Sorting = count;

                    db.SaveChanges();

                    count++;
                }
            }
        }

        [HttpGet]
        public ActionResult EditSidebar()
        {
            // Declare model
            SidebarViewModel model;
            using (Db db = new Db())
            {
                // Get the DTO
                SidebarDTO dto = db.Sidebar.Find(1);
                // init model
                model = new SidebarViewModel(dto);
                // Return view with model
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult EditSidebar(SidebarViewModel model)
        {
            using (Db db = new Db())
            {
                SidebarDTO dto = db.Sidebar.Find(1);

                dto.Body = model.Body;

                db.SaveChanges();
            }
            TempData["SM"] = "You have edited the sidebar!";

            //Redirect()
            return RedirectToAction("EditSidebar");
        }
    }
}