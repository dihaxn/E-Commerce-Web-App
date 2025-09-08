using System.Text;
using System.Text.Json.Nodes;
using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace E_Commerce_BE.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private string StripePublishableKey { get; set; } = "";
        private string StripeSecretKey { get; set; } = "";

        private readonly decimal shippingFee;
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public CheckoutController(IConfiguration configuration, ApplicationDbContext context
            , UserManager<ApplicationUser> userManager)
        {
            StripePublishableKey = configuration["StripeSettings:PublishableKey"]!;
            StripeSecretKey = configuration["StripeSettings:SecretKey"]!;
            
            // Configure Stripe
            StripeConfiguration.ApiKey = StripeSecretKey;

            shippingFee = configuration.GetValue<decimal>("CartSettings:ShippingFee");
            this.context = context;
            this.userManager = userManager;
        }

        public IActionResult Index()
        {
            List<OrderItem> cartItems = CartHelper.GetCartItems(Request, Response, context);
            decimal total = CartHelper.GetSubtotal(cartItems) + shippingFee;

            string deliveryAddress = TempData["DeliveryAddress"] as string ?? "";
            TempData.Keep();

            ViewBag.DeliveryAddress = deliveryAddress;
            ViewBag.Total = total;
            ViewBag.StripePublishableKey = StripePublishableKey;
            return View();
        }


        [HttpPost]
        public async Task<JsonResult> CreatePaymentIntent()
        {
            List<OrderItem> cartItems = CartHelper.GetCartItems(Request, Response, context);
            decimal totalAmount = CartHelper.GetSubtotal(cartItems) + shippingFee;

            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(totalAmount * 100), // Convert to cents
                    Currency = "usd",
                    PaymentMethodTypes = new List<string>
                    {
                        "card"
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", User.Identity?.Name ?? "" },
                        { "delivery_address", TempData["DeliveryAddress"]?.ToString() ?? "" }
                    }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                return new JsonResult(new { client_secret = paymentIntent.ClientSecret });
            }
            catch (StripeException ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }


        [HttpPost]
        public async Task<JsonResult> CompleteOrder([FromBody] JsonObject data)
        {
            var paymentIntentId = data?["paymentIntentId"]?.ToString();
            var deliveryAddress = data?["deliveryAddress"]?.ToString();

            if (paymentIntentId == null || deliveryAddress == null)
            {
                return new JsonResult(new { success = false, message = "Missing payment or delivery information" });
            }

            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);

                if (paymentIntent.Status == "succeeded")
                {
                    // Save the order in the database
                    await SaveOrder(paymentIntent, deliveryAddress);
                    return new JsonResult(new { success = true, message = "Order completed successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Payment not completed" });
                }
            }
            catch (StripeException ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task SaveOrder(PaymentIntent paymentIntent, string deliveryAddress)
        {
            // get cart items
            var cartItems = CartHelper.GetCartItems(Request, Response, context);

            var appUser = await userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return;
            }

            // save the order
            var order = new Order
            {
                ClientId = appUser.Id,
                Items = cartItems,
                ShippingFee = shippingFee,
                DeliveryAddress = deliveryAddress,
                PaymentMethod = "credit_card",
                PaymentStatus = "accepted",
                PaymentDetails = $"Stripe Payment Intent: {paymentIntent.Id}, Amount: {paymentIntent.Amount / 100.0m:C}",
                OrderStatus = "pending",
                CreatedAt = DateTime.Now,
            };

            context.Orders.Add(order);
            context.SaveChanges();

            // delete the shopping cart cookie
            Response.Cookies.Delete("shopping_cart");
        }


    }
}

