using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace E_Commerce_BE.Services
{
    public class DatabaseConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseConfigurationService> _logger;

        public DatabaseConfigurationService(IConfiguration configuration, ILogger<DatabaseConfigurationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void ConfigureDatabase(DbContextOptionsBuilder options)
        {
            var connectionString = GetConnectionString();
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured");
            }

            // Configure SQL Server with production optimizations
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);

                sqlOptions.CommandTimeout(30);
                sqlOptions.EnableSensitiveDataLogging(false); // Disable in production
            });

            _logger.LogInformation("Database configured successfully");
        }

        private string GetConnectionString()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Database connection string not found in configuration");
                return string.Empty;
            }

            // Validate connection string format
            if (!IsValidConnectionString(connectionString))
            {
                _logger.LogError("Invalid database connection string format");
                return string.Empty;
            }

            return connectionString;
        }

        private bool IsValidConnectionString(string connectionString)
        {
            try
            {
                // Basic validation - check for required components
                var requiredParts = new[] { "Data Source", "Initial Catalog", "User ID", "Password" };
                return requiredParts.All(part => connectionString.Contains(part));
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var connectionString = GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                    return false;

                using var context = new ApplicationDbContext(
                    new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlServer(connectionString)
                        .Options);

                await context.Database.CanConnectAsync();
                _logger.LogInformation("Database connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return false;
            }
        }

        public async Task<bool> EnsureDatabaseExistsAsync()
        {
            try
            {
                var connectionString = GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                    return false;

                using var context = new ApplicationDbContext(
                    new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlServer(connectionString)
                        .Options);

                await context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database ensured to exist");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure database exists");
                return false;
            }
        }
    }
}
