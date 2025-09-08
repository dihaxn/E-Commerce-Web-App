using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace E_Commerce_BE.Services
{
    public class HttpsConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HttpsConfigurationService> _logger;
        private readonly IWebHostEnvironment _environment;

        public HttpsConfigurationService(
            IConfiguration configuration, 
            ILogger<HttpsConfigurationService> logger,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        public void ConfigureHttps(IApplicationBuilder app)
        {
            if (_environment.IsProduction())
            {
                // Force HTTPS in production
                app.UseHttpsRedirection();
                
                // Configure HSTS
                ConfigureHsts(app);
                
                // Configure security headers
                ConfigureSecurityHeaders(app);
                
                _logger.LogInformation("HTTPS configuration applied for production");
            }
            else
            {
                _logger.LogInformation("HTTPS configuration skipped for non-production environment");
            }
        }

        private void ConfigureHsts(IApplicationBuilder app)
        {
            var hstsMaxAge = _configuration.GetValue<int>("HttpsRedirection:HstsMaxAge", 365);
            
            app.UseHsts();

            _logger.LogInformation($"HSTS configured with max age: {hstsMaxAge} days");
        }

        private void ConfigureSecurityHeaders(IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                // Security Headers
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
                
                // Content Security Policy
                var csp = BuildContentSecurityPolicy();
                context.Response.Headers.Add("Content-Security-Policy", csp);
                
                await next();
            });
        }

        private string BuildContentSecurityPolicy()
        {
            var csp = new List<string>
            {
                "default-src 'self'",
                "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://www.paypal.com",
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com",
                "font-src 'self' https://fonts.gstatic.com",
                "img-src 'self' data: https:",
                "connect-src 'self' https://www.paypal.com",
                "frame-src https://www.paypal.com",
                "base-uri 'self'",
                "form-action 'self'",
                "upgrade-insecure-requests"
            };

            return string.Join("; ", csp);
        }

        public bool IsHttpsEnabled()
        {
            return _environment.IsProduction() && 
                   _configuration.GetValue<bool>("HttpsRedirection:Enabled", true);
        }

        public string GetHttpsPort()
        {
            return _configuration.GetValue<string>("HttpsPort", "443");
        }

        public string GetHttpPort()
        {
            return _configuration.GetValue<string>("HttpPort", "80");
        }
    }
}
