using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using System.Web.WebPages;
using CmsShoppingCart.Areas.Admin.Models.ViewModels.Shop;
using CmsShoppingCart.Models.Data;
using CmsShoppingCart.Models.ViewModels.Shop;
using PagedList;

namespace CmsShoppingCart.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ShopController : Controller
    {
        // GET: Admin/Shop
        public ActionResult Categories()
        {
            // declare a list of models
            List<CategoryViewModel> categoryVMList;

            using (Db db = new Db())
            {
                categoryVMList = db.Categories
                    .ToArray()
                    .OrderBy(x => x.Sorting)
                    .Select(x => new CategoryViewModel(x))
                    .ToList();
            }
            // init the list


            return View(categoryVMList);
        }

        [HttpPost]
        public string AddNewCategory(string catName)
        {
            // declare id
            string id;

            using (Db db = new Db())
            {
                if (db.Categories.Any(x => x.Name == catName))
                    return "titletaken";
                // Init DTO
                CategoryDTO dto = new CategoryDTO();
                // Save DTO
                dto.Name = catName;
                dto.Slug = catName.Replace(" ", "-").ToLower();
                dto.Sorting = 100;
                db.Categories.Add(dto);
                db.SaveChanges();

                // Get the id
                id = dto.Id.ToString();

            }

            return id;
        }

        [HttpPost]
        public void ReorderCategories(int[] id)
        {
            using (Db db = new Db())
            {
                int count = 1;
                CategoryDTO dto;

                foreach (var catId in id)
                {
                    dto = db.Categories.Find(catId);
                    dto.Sorting = count;

                    db.SaveChanges();

                    count++;
                }
            }
        }

        public ActionResult DeleteCategory(int id)
        {
            using (Db db = new Db())
            {
                CategoryDTO dto = db.Categories.Find(id);
                db.Categories.Remove(dto);
                db.SaveChanges();
            }

            return RedirectToAction("Categories");
        }

        [HttpPost]
        public string RenameCategory(string newCatName, int id)
        {
            //CheckBox category name is ValidateRequest
            using (Db db = new Db())
            {
                if (db.Categories.Any(x => x.Name == newCatName))
                    return "titletaken";

                CategoryDTO dto = db.Categories.Find(id);

                dto.Name = newCatName;
                dto.Slug = newCatName.Replace(" ", "-").ToLower();
                db.SaveChanges();
            }

            return "ok";
        }

        [HttpGet]
        public ActionResult AddProduct()
        {
            ProductViewModel model = new ProductViewModel();

            using (Db db = new Db())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");

            }

            return View(model);
        }

        [HttpPost]
        public ActionResult AddProduct(ProductViewModel model, HttpPostedFileBase file)
        {
            // check model state
            if (!ModelState.IsValid)
            {
                using (Db db = new Db())
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    return View(model);
                }
            }
            // make sure product name is unique
            using (Db db = new Db())
            {
                if (db.Products.Any(x => x.Name == model.Name))
                {
                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    ModelState.AddModelError("", "That product name is taken!");
                    return View(model);
                }
                
            }
            // declare product id
            int id;
            // init and save product DTO
            using (Db db = new Db())
            {
                ProductDTO product = new ProductDTO();

                product.Name = model.Name;
                product.Slug = model.Name.Replace(" ", "-").ToLower();
                product.Description = model.Description;
                product.Price = model.Price;
                product.CategoryId = model.CategoryId;

                CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                product.CategoryName = catDTO.Name;

                db.Products.Add(product);
                db.SaveChanges();

                id = product.Id;
            }
            // get inserted id

            // set tempdata msg
            TempData["SM"] = "You have added a product!";

            #region Upload Image

            // create necessary directories
            var originalDirectory = new DirectoryInfo(String.Format("{0}Images\\Uploads", Server.MapPath(@"\")));
            var pathString1 = Path.Combine(originalDirectory.ToString(), "Products");
            var pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() );
            var pathString3 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");
            var pathString4 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
            var pathString5 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");
            
            if (!Directory.Exists(pathString1))
                Directory.CreateDirectory(pathString1);

            if (!Directory.Exists(pathString2))
                Directory.CreateDirectory(pathString2);

            if (!Directory.Exists(pathString3))
                Directory.CreateDirectory(pathString3);

            if (!Directory.Exists(pathString4))
                Directory.CreateDirectory(pathString4);

            if (!Directory.Exists(pathString5))
                Directory.CreateDirectory(pathString5);
            // check if a file was uploaded
            if (file != null && file.ContentLength > 0)
            {
                // get file extension
                string ext = file.ContentType.ToLower();
                // verify extension// init image 
                if (ext != "image/jpg" &&
                    ext != "image/jpeg" &&
                    ext != "image/pjpeg" &&
                    ext != "image/gif" &&
                    ext != "image/x-png" &&
                    ext != "image/png")
                {
                    using (Db db = new Db())
                    {
                        model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                        ModelState.AddModelError("", "The image was not uploaded - wrong image extension!");
                        return View(model);
                    }
                }

                string imageName = file.FileName;

                // save image name to dto
                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;
                    db.SaveChanges();
                }
                // set original and thumb image paths
                var path = string.Format("{0}\\{1}", pathString2, imageName);
                var path2 = string.Format("{0}\\{1}", pathString3, imageName);
                // save original
                file.SaveAs(path);
                // create and save thumb
                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(path2);
            }
            #endregion

            // Redirect
            return RedirectToAction("AddProduct");
        }

        public ActionResult Products(int? page, int? catId)
        {
            // declare a list of ProductVM
            List<ProductViewModel> listOfProductVM;
            // set page number
            var pageNumber = page ?? 1;
            // init the list
            using (Db db = new Db())
            {
                listOfProductVM = db.Products.ToArray()
                    .Where(x => catId == null || catId == 0 || x.CategoryId == catId)
                    .Select(x => new ProductViewModel(x))
                    .ToList();
                // populate categories select list
                ViewBag.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                // set selected category
                ViewBag.SelectedCat = catId.ToString();
            }

            // set pagination
            var onePageOfProducts = listOfProductVM.ToPagedList(pageNumber, 3);
            ViewBag.OnePageOfProducts = onePageOfProducts;
            // return view with list
            return View(listOfProductVM);
        }

        [HttpGet]
        public ActionResult EditProduct(int id)
        {
            // declare productVM
            ProductViewModel model;

            using (Db db = new Db())
            {
                // get the product
                ProductDTO dto = db.Products.Find(id);
                // make sure it exists
                if (dto == null)
                {
                    return Content("That product doesn't exist.");
                }
                // init model
                model = new ProductViewModel(dto);
                // make a select list
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                // get all gallery images
                model.GalleryImages = Directory
                    .EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                    .Select(fn => Path.GetFileName(fn));
            }


            // return view with model
            return View(model);
        }

        [HttpPost]
        public ActionResult EditProduct(ProductViewModel model, HttpPostedFileBase file)
        {
            // get product id
            int id = model.Id;
            // populate categories select list and gallery images
            using (Db db = new Db())
            {
                model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
            }
            model.GalleryImages = Directory
                .EnumerateFiles(Server.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs"))
                .Select(fn => Path.GetFileName(fn));
            // check model state
            if (!ModelState.IsValid)
                return View(model);
            // make sure product name is unique
            using (Db db = new Db())
            {
                if (db.Products.Where(x => x.Id != id).Any(x => x.Name == model.Name))
                {
                    ModelState.AddModelError("", "That product name is taken!");
                    return View(model);
                }
            }
            // update product 
            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);

                dto.Name = model.Name;
                dto.Slug = model.Name.Replace(" ", "-").ToLower();
                dto.Description = model.Description;
                dto.Price = model.Price;
                dto.CategoryId = model.CategoryId;
                dto.ImageName = model.ImageName;

                CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                dto.CategoryName = catDTO.Name;

                db.SaveChanges();
            }
            // set TempData message
            TempData["SM"] = "You have edited the product!";
            #region Image Upload

            // check for file upload
            if (file != null && file.ContentLength > 0)
            {
                // get extension
                string ext = file.ContentType.ToLower();
                // verify extension
                if (ext != "image/jpg" &&
                    ext != "image/jpeg" &&
                    ext != "image/pjpeg" &&
                    ext != "image/gif" &&
                    ext != "image/x-png" &&
                    ext != "image/png")
                {
                    using (Db db = new Db())
                    {
                        ModelState.AddModelError("", "The image was not uploaded - wrong image extension!");
                        return View(model);
                    }
                }
                // set upload directory paths
                var originalDirectory = new DirectoryInfo(String.Format("{0}Images\\Uploads", Server.MapPath(@"\")));
                var pathString1 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());
                var pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");

                // delete files from directories
                DirectoryInfo di1 = new DirectoryInfo(pathString1);
                DirectoryInfo di2 = new DirectoryInfo(pathString2);

                foreach (FileInfo file2 in di1.GetFiles())
                    file2.Delete();

                foreach (FileInfo file3 in di2.GetFiles())
                    file3.Delete();
                // save image name
                string imageName = file.FileName;

                using (Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;

                    db.SaveChanges();
                }
                // save original and thumb images
                var path = string.Format("{0}\\{1}", pathString1, imageName);
                var path2 = string.Format("{0}\\{1}", pathString2, imageName);
                // save original
                file.SaveAs(path);
                // create and save thumb
                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(path2);
            }


            #endregion

            return RedirectToAction("EditProduct");
        }

        public ActionResult DeleteProduct(int id)
        {
            // delete product from db
            using (Db db = new Db())
            {
                ProductDTO dto = db.Products.Find(id);
                db.Products.Remove(dto);
                db.SaveChanges();
            }
            // delete product folder
            var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

            string pathString = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString());

            if (Directory.Exists(pathString))
                Directory.Delete(pathString, true);
            // redirect
            return RedirectToAction("Products");
        }

        [HttpPost]
        public void SaveGalleryImages(int id)
        {
            
            // Loop through files
            foreach (string fileName in Request.Files)
            {
                // init the file
                HttpPostedFileBase file = Request.Files[fileName];
                // check it's not null
                if (file != null && file.ContentLength > 0)
                {
                    // set directory paths
                    var originalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads", Server.MapPath(@"\")));

                    string pathString1 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
                    string pathString2 = Path.Combine(originalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");
                    // set image paths
                    var storedFileName = fileName + "_" + DateTime.Now.ToFileTime() + ".jpeg";
                    var path = string.Format("{0}\\{1}", pathString1, storedFileName);
                    var path2 = string.Format("{0}\\{1}", pathString2, storedFileName);
                    // save original and thumb
                    file.SaveAs(path);
                    WebImage img = new WebImage(file.InputStream);
                    img.Resize(200, 200);
                    img.Save(path2);
                }
            }
        }

        [HttpPost]
        public void DeleteImage(int id, string imageName)
        {
            string fullPath1 = Request.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/" + imageName);
            string fullPath2 = Request.MapPath("~/Images/Uploads/Products/" + id + "/Gallery/Thumbs/" + imageName);

            if (System.IO.File.Exists(fullPath1))
                System.IO.File.Delete(fullPath1);

            if (System.IO.File.Exists(fullPath2))
                System.IO.File.Delete(fullPath2);
        }

        public ActionResult Orders()
        {
            // initialize list of OrdersForAdminVM
            List<OrdersForAdminViewModel> ordersForAdmin = new List<OrdersForAdminViewModel>();

            using (Db db = new Db())
            {
                // init list of ordersVM
                List<OrderViewModel> orders = db.Orders.ToArray().Select(x => new OrderViewModel(x)).ToList();

                // loop through list of ordersVM
                foreach (var order in orders)
                {
                    // init product dictionary
                    Dictionary<string, int> productsAndQty = new Dictionary<string, int>();

                    // declare total
                    decimal total = 0m;

                    // init list of OrderDetailsDTO
                    List<OrderDetailsDTO> orderDetailsList =
                        db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();
                    // get username
                    UserDTO user = db.Users.FirstOrDefault(x => x.Id == order.UserId);
                    string username = user.Username;

                    // loop through list of orderdetailsDTO
                    foreach (var orderDetail in orderDetailsList)
                    {
                        // get product
                        ProductDTO product = db.Products.FirstOrDefault(x => x.Id == orderDetail.ProductId);
                        // get product price, name
                        decimal price = product.Price;
                        string productName = product.Name;

                        // add to product dictionary
                        productsAndQty.Add(productName, orderDetail.Quantity);

                        // get total
                        total += orderDetail.Quantity*price;
                    }
                    // Add to prderForAdminVM list
                    ordersForAdmin.Add(new OrdersForAdminViewModel
                    {
                        OrderNumber = order.OrderId,
                        Username = username,
                        Total = total,
                        ProductsAndQty = productsAndQty,
                        CreatedAt = order.CreatedAt
                    });
                }
            }

            // return view with ordersForAdmin list 
            return View(ordersForAdmin);
        }
    }
}