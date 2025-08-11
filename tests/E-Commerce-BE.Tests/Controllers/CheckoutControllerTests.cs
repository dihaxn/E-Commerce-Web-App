using E_Commerce_BE.Controllers;
using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using Xunit;

namespace E_Commerce_BE.Tests.Controllers
{
    public class CheckoutControllerTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;

        public CheckoutControllerTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            var inMemorySettings = new Dictionary<string, string> {
                {"PaypalSettings:ClientId", "test_client_id"},
                {"PaypalSettings:Secret", "test_secret"},
                {"PaypalSettings:Url", "https://api.sandbox.paypal.com"},
                {"CartSettings:ShippingFee", "5.00"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _configuration = new Mock<IConfiguration>();
            _configuration.Setup(c => c["PaypalSettings:ClientId"]).Returns("test_client_id");
            _configuration.Setup(c => c["PaypalSettings:Secret"]).Returns("test_secret");
            _configuration.Setup(c => c["PaypalSettings:Url"]).Returns("https://api.sandbox.paypal.com");
            _configuration.Setup(c => c.GetSection("CartSettings:ShippingFee")).Returns(configuration.GetSection("CartSettings:ShippingFee"));



            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
        }

        private ApplicationDbContext GetInMemoryDbContext()
        {
            var context = new ApplicationDbContext(_options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public void Index_ReturnsViewWithCheckoutDetails()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var controller = new CheckoutController(_configuration.Object, context, _userManager.Object);
            var httpContext = new DefaultHttpContext();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
            }, "mock"));
            httpContext.User = user;
            controller.ControllerContext.HttpContext = httpContext;
            controller.TempData = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary>().Object;


            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ViewData["Total"]);
        }
    }
}
