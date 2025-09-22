using E_Commerce_BE.Models;
using System.Text.Json;

namespace E_Commerce_BE.Services
{
    public class CartService : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly ISecureCookieService _cookieService;

        public CartService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context, ISecureCookieService cookieService)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _cookieService = cookieService;
        }

        private HttpRequest GetRequest() => _httpContextAccessor.HttpContext!.Request;
        private HttpResponse GetResponse() => _httpContextAccessor.HttpContext!.Response;

        public Dictionary<int, int> GetCartDictionary()
        {
            try
            {
                var cart = _cookieService.GetShoppingCartCookie(GetRequest());
                return cart ?? new Dictionary<int, int>();
            }
            catch (Exception)
            {
                // If there's any error, clear the cookie and return empty cart
                _cookieService.DeleteSecureCookie(GetResponse(), "shopping_cart");
                return new Dictionary<int, int>();
            }
        }

        public int GetCartSize()
        {
            int cartSize = 0;
            var cartDictionary = GetCartDictionary();
            foreach (var keyValuePair in cartDictionary)
            {
                cartSize += keyValuePair.Value;
            }
            return cartSize;
        }

        public List<OrderItem> GetCartItems()
        {
            var cartItems = new List<OrderItem>();
            var cartDictionary = GetCartDictionary();

            foreach (var pair in cartDictionary)
            {
                int productId = pair.Key;
                int quantity = pair.Value;
                var product = _context.Products.Find(productId);
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

        public decimal GetSubtotal(List<OrderItem> cartItems)
        {
            decimal subtotal = 0;

            foreach (var item in cartItems)
            {
                subtotal += item.Quantity * item.UnitPrice;
            }

            return subtotal;
        }

        public void UpdateCart(Dictionary<int, int> cartData)
        {
            _cookieService.SetShoppingCartCookie(GetResponse(), cartData);
        }

        public void ClearCart()
        {
            _cookieService.DeleteSecureCookie(GetResponse(), "shopping_cart");
        }
    }
}
