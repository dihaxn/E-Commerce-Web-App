using System.Text.Json;
using E_Commerce_BE.Models;

namespace E_Commerce_BE.Services
{
    public class CartHelper
    {
        private static SecureCookieService? _cookieService;

        public static void Initialize(SecureCookieService cookieService)
        {
            _cookieService = cookieService;
        }

        public static Dictionary<int, int> GetCartDictionary(HttpRequest request, HttpResponse response)
        {
            if (_cookieService == null)
            {
                throw new InvalidOperationException("CartHelper not initialized. Call Initialize() first.");
            }

            try
            {
                var cart = _cookieService.GetShoppingCartCookie(request);
                return cart ?? new Dictionary<int, int>();
            }
            catch (Exception)
            {
                // If there's any error, clear the cookie and return empty cart
                _cookieService.DeleteSecureCookie(response, "shopping_cart");
                return new Dictionary<int, int>();
            }
        }

        public static int GetCartSize(HttpRequest request, HttpResponse response)
        {
            int cartSize = 0;
            var cartDictionary = GetCartDictionary(request, response);
            foreach (var keyValuePair in cartDictionary)
            {
                cartSize += keyValuePair.Value;
            }
            return cartSize;
        }

        public static List<OrderItem> GetCartItems(HttpRequest request, HttpResponse response, ApplicationDbContext context)
        {
            var cartItems = new List<OrderItem>();
            var cartDictionary = GetCartDictionary(request, response);

            foreach (var pair in cartDictionary)
            {
                int productId = pair.Key;
                int quantity = pair.Value;
                var product = context.Products.Find(productId);
                if (product == null) continue;

                var item = new OrderItem
                {
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    Product = product
                };

                cartItems.Add(item);
            }

            return cartItems;
        }

        public static decimal GetSubtotal(List<OrderItem> cartItems)
        {
            decimal subtotal = 0;

            foreach (var item in cartItems)
            {
                subtotal += item.Quantity * item.UnitPrice;
            }

            return subtotal;
        }

        public static void UpdateCart(HttpRequest request, HttpResponse response, Dictionary<int, int> cartData)
        {
            if (_cookieService == null)
            {
                throw new InvalidOperationException("CartHelper not initialized. Call Initialize() first.");
            }

            _cookieService.SetShoppingCartCookie(response, cartData);
        }

        public static void ClearCart(HttpRequest request, HttpResponse response)
        {
            if (_cookieService == null)
            {
                throw new InvalidOperationException("CartHelper not initialized. Call Initialize() first.");
            }

            _cookieService.DeleteSecureCookie(response, "shopping_cart");
        }
    }
}
