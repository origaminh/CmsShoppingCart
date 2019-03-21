using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsShoppingCart.Models.Data;
using CmsShoppingCart.Models.ViewModels.Pages;

namespace CmsShoppingCart.Controllers
{
    public class PagesController : Controller
    {
        // GET: Index/{page}
        public ActionResult Index(string page = "")
        {
            if (page == "")
                page = "home";

            // declare model and DTO
            PageViewModel model;
            PageDTO dto;

            using (Db db = new Db())
            {
                if (!db.Pages.Any(x => x.Slug.Equals(page)))
                {
                    return RedirectToAction("Index", new {page = ""});
                }
            }

            // get page DTO
            using (Db db = new Db())
            {
                dto = db.Pages.FirstOrDefault(x => x.Slug == page);
            }

            // set page title
            ViewBag.PageTitle = dto.Title;

            // check for sidebar
            if (dto.HasSidebar == true)
            {
                ViewBag.Sidebar = "Yes";
            }
            else
            {
                ViewBag.Sidebar = "No";
            }

            // init model
            model = new PageViewModel(dto);

            return View(model);
        }

        public ActionResult PagesMenuPartial()
        {
            // declare list of pageviewmodel
            List<PageViewModel> pageVMList;

            // get all pages except home
            using (Db db = new Db())
            {
                pageVMList = db.Pages.ToArray().OrderBy(x => x.Sorting).Where(x => x.Slug != "home")
                    .Select(x => new PageViewModel(x)).ToList();

            }
            // return partial view with list

            return PartialView(pageVMList);
        }

        public ActionResult SidebarPartial()
        {
            // declare model
            SidebarViewModel model;
            // init model
            using (Db db = new Db())
            {
                SidebarDTO dto = db.Sidebar.Find(1);

                model = new SidebarViewModel(dto);
            }
            return PartialView(model);
        }
    }
}