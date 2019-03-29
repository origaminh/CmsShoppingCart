using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using CmsShoppingCart.Models.Data;
using CmsShoppingCart.Models.ViewModels.Account;
using CmsShoppingCart.Models.ViewModels.Shop;

namespace CmsShoppingCart.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return Redirect("~/account/login");
        }

        [HttpGet]
        public ActionResult Login()
        {
            // confirm user is not logger in
            string username = User.Identity.Name;

            if(!string.IsNullOrEmpty(username))
                return RedirectToAction("user-profile");

            // return view
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginUserViewModel model)
        {
            // Check model state
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if the user is valid

            bool isValid = false;

            using (Db db = new Db())
            {
                if (db.Users.Any(x => x.Username.Equals(model.Username) && x.Password.Equals(model.Password)))
                {
                    isValid = true;
                }
            }

            if (!isValid)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }
            else
            {
                FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                return Redirect(FormsAuthentication.GetRedirectUrl(model.Username, model.RememberMe));
            }
        }

        [HttpGet]
        [ActionName("create-account")]
        public ActionResult CreateAccount()
        {

            return View("CreateAccount");
        }

        [HttpPost]
        [ActionName("create-account")]
        public ActionResult CreateAccount(UserViewModel model)
        {
            // check model state
            if (!ModelState.IsValid)
            {
                return View("CreateAccount", model);
            }
            // check if passwords match 
            if (!model.Password.Equals(model.ConfirmPassword))
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View("CreateAccount", model);
            }

            using (Db db = new Db())
            {
                // make sure username is unique
                if (db.Users.Any(x => x.Username.Equals(model.Username)))
                {
                    ModelState.AddModelError("", "Username " + model.Username + " is taken.");
                    model.Username = "";
                    return View("CreateAccount", model);
                }
                // create userDTO
                UserDTO userDTO = new UserDTO()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailAddress = model.EmailAddress,
                    Username = model.Username,
                    Password = model.Password
                };
                // Add the DTO
                db.Users.Add(userDTO);

                // Save
                db.SaveChanges();

                // Add to userRolesDTO
                int id = userDTO.Id;

                UserRoleDTO userRolesDTO = new UserRoleDTO()
                {
                    UserId = id,
                    RoleId = 2
                };

                db.UserRoles.Add(userRolesDTO);
                db.SaveChanges();
            }


            // create a TempData message
            TempData["SM"] = "You are now registered and can login.";

            // redirect
            return Redirect("~/account/login");
        }

        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return Redirect("~/account/login");
        }

        [Authorize]
        public ActionResult UserNavPartial()
        {
            // get username 
            string username = User.Identity.Name;

            // declare model
            UserNavPartialViewModel model;

            using (Db db = new Db())
            {
                // get the user
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == username);

                // build the model
                model = new UserNavPartialViewModel()
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName
                };
            }


            // return partial view with model
            return PartialView(model);
        }

        [HttpGet]
        [ActionName("user-profile")]
        [Authorize]
        public ActionResult UserProfile()
        {
            // get username
            string username = User.Identity.Name;

            // declare model
            UserProfileViewModel model;

            using (Db db = new Db())
            {
                // get user
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username == username);
                // build model 
                model = new UserProfileViewModel(dto);
            }

            // return view with model
            return View("UserProfile", model);
        }

        [HttpPost]
        [ActionName("user-profile")]
        [Authorize]
        public ActionResult UserProfile(UserProfileViewModel model)
        {
            // check model state
            if (!ModelState.IsValid)
            {
                return View("UserProfile", model);
            }

            // check if passwords match if need be
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (!model.Password.Equals(model.ConfirmPassword))
                {
                    ModelState.AddModelError("", "Passwords do not match.");
                    return View("UserProfile", model);
                }
            }

            using (Db db = new Db())
            {
                // get username
                string username = User.Identity.Name;

                // make sure username is unique
                if (db.Users.Where(x => x.Id != model.Id).Any(x => x.Username == username))
                {
                    ModelState.AddModelError("", "Username " + model.Username + " already exists.");
                    model.Username = "";
                    return View("UserProfile", model);
                }
                // edit DTO
                UserDTO dto = db.Users.Find(model.Id);

                dto.FirstName = model.FirstName;
                dto.LastName = model.LastName;
                dto.EmailAddress = model.EmailAddress;
                dto.Username = model.Username;

                if (!string.IsNullOrWhiteSpace(model.Password))
                    dto.Password = model.Password;
                // save
                db.SaveChanges();
            }

            // set TempData message
            TempData["SM"] = "You have edited your profile";

            // redirect
            return Redirect("~/account/user-profile");
        }

        [Authorize(Roles = "User")]
        public ActionResult Orders()
        {
            // init list of ordersForUserVM
            List<OrdersForUserViewModel> ordersForUser = new List<OrdersForUserViewModel>();

            using (Db db = new Db())
            {
                // get user id
                UserDTO user = db.Users.FirstOrDefault(x => x.Username == User.Identity.Name);
                int userId = user.Id;

                // init list of orderVM
                List<OrderViewModel> orders =
                    db.Orders.Where(x => x.UserId == userId).ToArray().Select(x => new OrderViewModel(x)).ToList();
                // loop through that list
                foreach (var order in orders)
                {
                    // init product dictionary
                    Dictionary<string, int> productsAndQty = new Dictionary<string, int>();

                    // declare total
                    decimal total = 0m;
                    // init list of OrderDetailsDTOs
                    List<OrderDetailsDTO> orderDetailsDTOs =
                        db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();
                    // list through list of orderDetailsDTOs
                    foreach (var orderDetail in orderDetailsDTOs)
                    {
                        // get product, price, name
                        ProductDTO product = db.Products.FirstOrDefault(x => x.Id == orderDetail.ProductId);
                        decimal price = product.Price;
                        string productName = product.Name;

                        // add to product Dictionary
                        productsAndQty.Add(productName, orderDetail.Quantity);
                        // get total
                        total += orderDetail.Quantity*price;
                    }

                    // add to OrdersForUserVM list
                    ordersForUser.Add(new OrdersForUserViewModel
                    {
                        OrderNumber = order.OrderId,
                        Total = total,
                        ProductsAndQty = productsAndQty,
                        CreatedAt = order.CreatedAt
                    });
                }
            }

            return View(ordersForUser);
        }
    }
}