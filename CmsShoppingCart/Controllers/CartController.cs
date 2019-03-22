using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsShoppingCart.Models.Data;
using CmsShoppingCart.Models.ViewModels.Cart;

namespace CmsShoppingCart.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        public ActionResult Index()
        {
            // initialize the cart list 
            var cart = Session["cart"] as List<CartViewModel> ?? new List<CartViewModel>();
            // check if cart is empty
            if (cart.Count == 0 || Session["cart"] == null)
            {
                ViewBag.Message = "You cart is empty.";
                return View();
            }
            // calculate total and save to ViewBag
            decimal total = 0m;

            foreach (var item in cart)
            {
                total += item.Total;
            }

            ViewBag.GrandTotal = total;
            // Return view with model
            return View();
        }

        public ActionResult CartPartial()
        {
            // init cartVM
            CartViewModel model = new CartViewModel();
            // init quantity
            int qty = 0;
            // init price
            decimal price = 0m;
            // check for cart session
            if (Session["cart"] != null)
            {
                // get total quantity and price
                var list = (List<CartViewModel>) Session["cart"];

                foreach (var item in list)
                {
                    qty += item.Quantity;
                    price += item.Quantity * item.Price;
                }
            }
            else
            {
                // or set qty and price to 0
                model.Quantity = 0;
                model.Price = 0;
            }

            // return partial view with model
            return PartialView(model);
        }

        public ActionResult AddToCartPartial(int id)
        {
            // init cartVM list
            List<CartViewModel> cart = Session["cart"] as List<CartViewModel> ?? new List<CartViewModel>();

            // init cartVM
            CartViewModel model = new CartViewModel();

            using (Db db = new Db())
            {
                // get the product
                ProductDTO product = db.Products.Find(id);
                // check if the product is already in cart
                var productInCart = cart.FirstOrDefault(x => x.ProductId == id);
                // if not, add new. if it is, increment
                if (productInCart == null)
                {
                    cart.Add(new CartViewModel()
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 1,
                        Price = product.Price,
                        Image = product.ImageName
                    });
                }
                else
                {
                    
                }
            }


            // get total qty and price and add to model

            // save cart back to session

            // return partial view with model

            return View();
        }
    }
}