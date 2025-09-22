using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure database with production optimizations
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Database connection string is not configured");
    }

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);

        sqlOptions.CommandTimeout(30);

    });
});

// Configure Identity with strong security settings
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Strong password requirements
    options.Password.RequiredLength = 12;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;

    // Account lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false; // Set to true in production
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure session with security settings
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add production services
builder.Services.AddSingleton<SecurityConfiguration>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ISanitizationService, SanitizationService>();
builder.Services.AddScoped<ISecureFileUploadService, SecureFileUploadService>();
builder.Services.AddScoped<ISecureCookieService, SecureCookieService>();
builder.Services.AddSingleton<IRateLimitingService, RateLimitingService>();
builder.Services.AddScoped<DatabaseConfigurationService>();
builder.Services.AddSingleton<HttpsConfigurationService>();
builder.Services.AddSingleton<MonitoringService>();
builder.Services.AddScoped<BackupService>();

// Configure Brevo API
var brevoApiKey = builder.Configuration["BrevoSettings:ApiKey"];
if (!string.IsNullOrEmpty(brevoApiKey))
{
    Configuration.Default.ApiKey.Add("api-key", brevoApiKey);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Configure HTTPS and security headers
var httpsService = app.Services.GetService<HttpsConfigurationService>();
if (httpsService != null)
{
    httpsService.ConfigureHttps(app);
}

// Security middleware
app.UseHttpsRedirection();

// Add security headers (if not using HTTPS service)
if (httpsService == null)
{
    app.Use(async (context, next) =>
    {
        // Security Headers
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

        // Content Security Policy
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://www.paypal.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "img-src 'self' data: https:; " +
            "connect-src 'self' https://www.paypal.com; " +
            "frame-src https://www.paypal.com;");

        await next();
    });
}

app.UseStaticFiles();

// Session middleware
app.UseSession();

app.UseRouting();

// Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map health check endpoints
app.MapControllerRoute(
    name: "health",
    pattern: "health",
    defaults: new { controller = "Health", action = "GetHealth" });

// Create the roles and the first admin user if not available yet
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetService(typeof(UserManager<ApplicationUser>))
        as UserManager<ApplicationUser>;

    var roleManager = scope.ServiceProvider.GetService(typeof(RoleManager<IdentityRole>))
        as RoleManager<IdentityRole>;

    await DatabaseInitializer.SeedDataAsync(userManager, roleManager);
}

// Start background services in a scope
using (var backgroundScope = app.Services.CreateScope())
{
    var rateLimitingService = backgroundScope.ServiceProvider.GetService<RateLimitingService>();
    if (rateLimitingService != null)
    {
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            while (true)
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromMinutes(5));
                rateLimitingService.CleanupExpiredEntries();
            }
        });
    }

    var monitoringService = backgroundScope.ServiceProvider.GetService<MonitoringService>();
    if (monitoringService != null)
    {
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            while (true)
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromMinutes(10));
                try
                {
                    await monitoringService.PerformHealthCheckAsync();
                }
                catch (Exception ex)
                {
                    // Log error but don't crash the background task
                    var logger = backgroundScope.ServiceProvider.GetService<ILogger<Program>>();
                    logger?.LogError(ex, "Background health check failed");
                }
            }
        });
    }

    var backupService = backgroundScope.ServiceProvider.GetService<BackupService>();
    if (backupService != null)
    {
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            while (true)
            {
                var now = DateTime.UtcNow;
                var nextBackup = now.Date.AddDays(1).AddHours(2); // 2 AM UTC
                var delay = nextBackup - now;

                await System.Threading.Tasks.Task.Delay(delay);

                try
                {
                    await backupService.CreateFullBackupAsync();
                }
                catch (Exception ex)
                {
                    var logger = backgroundScope.ServiceProvider.GetService<ILogger<Program>>();
                    logger?.LogError(ex, "Scheduled backup failed");
                }
            }
        });
    }
}

app.Run();
