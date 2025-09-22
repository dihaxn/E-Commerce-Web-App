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
        private readonly Mock<ICartService> _cartService;

        public CheckoutControllerTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_Checkout")
                .Options;

            var inMemorySettings = new Dictionary<string, string?> {
                {"StripeSettings:PublishableKey", "pk_test_123"},
                {"StripeSettings:SecretKey", "sk_test_123"},
                {"StripeSettings:WebhookSecret", "whsec_123"},
                {"CartSettings:ShippingFee", "5.00"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _configuration = new Mock<IConfiguration>();
            _configuration.Setup(c => c["StripeSettings:PublishableKey"]).Returns("pk_test_123");
            _configuration.Setup(c => c["StripeSettings:SecretKey"]).Returns("sk_test_123");
            _configuration.Setup(c => c["StripeSettings:WebhookSecret"]).Returns("whsec_123");
            _configuration.Setup(c => c.GetSection("CartSettings:ShippingFee")).Returns(configuration.GetSection("CartSettings:ShippingFee"));



            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object,
                new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<ApplicationUser>>().Object,
                new IUserValidator<ApplicationUser>[0],
                new IPasswordValidator<ApplicationUser>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<ApplicationUser>>>().Object
            );
            _cartService = new Mock<ICartService>();
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
            _cartService.Setup(s => s.GetCartItems()).Returns(new List<OrderItem>());
            _cartService.Setup(s => s.GetSubtotal(It.IsAny<List<OrderItem>>())).Returns(0);

            var controller = new CheckoutController(_configuration.Object, context, _userManager.Object, _cartService.Object);
            var httpContext = new DefaultHttpContext();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
            }, "mock"));
            httpContext.User = user;
            controller.ControllerContext.HttpContext = httpContext;
            var tempData = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary>();
            controller.TempData = tempData.Object;


            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ViewData["Total"]);
        }
    }
}
