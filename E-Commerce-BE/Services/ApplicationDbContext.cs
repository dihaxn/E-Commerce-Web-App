using E_Commerce_BE.Models;

using Microsoft.EntityFrameworkCore;

namespace E_Commerce_BE.Services
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ApplicationDbContext context;

        public ApplicationDbContext(DbContextOptions options)
        {
            this.context = context;

        }

        public DbSet<Product> Products /*dbname*/ { get; set; }

    }
}
