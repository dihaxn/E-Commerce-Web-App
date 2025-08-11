using System.ComponentModel.DataAnnotations;

namespace E_Commerce_BE.Models
{
    public class CheckoutDto
    {
        [Required(ErrorMessage = "The Delivery Address is required.")]
        public string DeliveryAddress { get; set; } = "";

        public string PaymentMethod { get; set; } = "";
    }
}
