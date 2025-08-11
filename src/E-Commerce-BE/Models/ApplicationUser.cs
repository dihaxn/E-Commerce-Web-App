using Microsoft.AspNetCore.Identity;

namespace E_Commerce_BE.Models
{
    public class ApplicationUser : IdentityUser
    {
       
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Address { get; set; } = "";

        public DateTime CreateAt { get; set; }

    }
}
