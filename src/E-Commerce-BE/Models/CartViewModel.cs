using E_Commerce_BE.Models;

namespace E_Commerce_BE.Models
{
    public class CartViewModel
    {
        public List<OrderItem> CartItems { get; set; } = new List<OrderItem>();
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Total { get; set; }

        public string DeliveryAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
