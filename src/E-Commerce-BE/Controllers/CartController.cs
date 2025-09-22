using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_BE.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ICartService _cartService;
        private readonly decimal shippingFee;

        public CartController(ApplicationDbContext context, IConfiguration configuration,
            UserManager<ApplicationUser> userManager, ICartService cartService)
        {
            this.context = context;
            this.userManager = userManager;
            _cartService = cartService;
            shippingFee = configuration.GetValue<decimal>("CartSettings:ShippingFee");
        }

        public IActionResult Index()
        {
            List<OrderItem> cartItems = _cartService.GetCartItems();
            decimal subtotal = _cartService.GetSubtotal(cartItems);

            var model = new CartViewModel
            {
                CartItems = cartItems,
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                Total = subtotal + shippingFee
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(CartViewModel model)
        {
            List<OrderItem> cartItems = _cartService.GetCartItems();
            decimal subtotal = _cartService.GetSubtotal(cartItems);

            model.CartItems = cartItems;
            model.Subtotal = subtotal;
            model.ShippingFee = shippingFee;
            model.Total = subtotal + shippingFee;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if shopping cart is empty or not
            if (cartItems.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Your cart is empty");
                return View(model);
            }

            TempData["DeliveryAddress"] = model.DeliveryAddress;
            TempData["PaymentMethod"] = model.PaymentMethod;

            if (model.PaymentMethod == "paypal" || model.PaymentMethod == "credit_card")
            {
                return RedirectToAction("Index", "Checkout");
            }

            return RedirectToAction("ConfirmOrder");
        }

        public IActionResult Confirm()
        {
            List<OrderItem> cartItems = _cartService.GetCartItems();
            decimal total = _cartService.GetSubtotal(cartItems) + shippingFee;
            int cartSize = _cartService.GetCartSize();

            string deliveryAddress = TempData["DeliveryAddress"] as string ?? "";
            string paymentMethod = TempData["PaymentMethod"] as string ?? "";
            TempData.Keep();

            if (cartSize == 0 || deliveryAddress.Length == 0 || paymentMethod.Length == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.DeliveryAddress = deliveryAddress;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.Total = total;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder()
        {
            List<OrderItem> cartItems = _cartService.GetCartItems();
            decimal total = _cartService.GetSubtotal(cartItems) + shippingFee;

            string deliveryAddress = TempData["DeliveryAddress"] as string ?? "";
            string paymentMethod = TempData["PaymentMethod"] as string ?? "";

            if (cartItems.Count == 0 || deliveryAddress.Length == 0 || paymentMethod.Length == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var appUser = await userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Create the order
            var order = new Order
            {
                ClientId = appUser.Id,
                Items = cartItems,
                ShippingFee = shippingFee,
                DeliveryAddress = deliveryAddress,
                PaymentMethod = paymentMethod,
                PaymentStatus = "pending",
                OrderStatus = "pending",
                CreatedAt = DateTime.UtcNow,
            };

            context.Orders.Add(order);
            context.SaveChanges();

            // Clear the shopping cart
            _cartService.ClearCart();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Invalid quantity" });
            }

            var cartData = _cartService.GetCartDictionary();

            if (cartData.ContainsKey(productId))
            {
                cartData[productId] += quantity;
            }
            else
            {
                cartData[productId] = quantity;
            }

            _cartService.UpdateCart(cartData);

            return Json(new { success = true, cartSize = _cartService.GetCartSize() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Invalid quantity" });
            }

            var cartData = _cartService.GetCartDictionary();

            if (cartData.ContainsKey(productId))
            {
                cartData[productId] = quantity;
                _cartService.UpdateCart(cartData);
                return Json(new { success = true, cartSize = _cartService.GetCartSize() });
            }

            return Json(new { success = false, message = "Product not found in cart" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int productId)
        {
            var cartData = _cartService.GetCartDictionary();

            if (cartData.ContainsKey(productId))
            {
                cartData.Remove(productId);
                _cartService.UpdateCart(cartData);
                return Json(new { success = true, cartSize = _cartService.GetCartSize() });
            }

            return Json(new { success = false, message = "Product not found in cart" });
        }
    }
}
