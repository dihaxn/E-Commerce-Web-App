using E_Commerce_BE.Models;

namespace E_Commerce_BE.Services
{
    public interface ICartService
    {
        Dictionary<int, int> GetCartDictionary();
        int GetCartSize();
        List<OrderItem> GetCartItems();
        decimal GetSubtotal(List<OrderItem> cartItems);
        void UpdateCart(Dictionary<int, int> cartData);
        void ClearCart();
    }
}
