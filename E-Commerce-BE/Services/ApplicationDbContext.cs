

using E_Commerce_BE.Models;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce_BE.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {


        }

        public DbSet<Product> Products /*dbname*/ { get; set; }

    }
}
