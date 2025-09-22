using E_Commerce_BE.Controllers;
using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace E_Commerce_BE.Tests.Controllers
{
    public class CartControllerTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<ICartService> _cartService;

        public CartControllerTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_Cart")
                .Options;

            _configuration = new Mock<IConfiguration>();
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(s => s.Value).Returns("5.00");
            _configuration.Setup(c => c.GetSection("CartSettings:ShippingFee")).Returns(configSection.Object);

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
        public void Index_ReturnsViewWithCartItems()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            _cartService.Setup(s => s.GetCartItems()).Returns(new List<OrderItem>());
            _cartService.Setup(s => s.GetSubtotal(It.IsAny<List<OrderItem>>())).Returns(0);

            var controller = new CartController(context, _configuration.Object, _userManager.Object, _cartService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<CartViewModel>(result.Model);
            Assert.NotNull(model.CartItems);
        }

        [Fact]
        public void Confirm_RedirectsToHome_WhenCartIsEmpty()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            _cartService.Setup(s => s.GetCartSize()).Returns(0);
            var controller = new CartController(context, _configuration.Object, _userManager.Object, _cartService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext.HttpContext = httpContext;
            var tempData = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary>();
            controller.TempData = tempData.Object;


            // Act
            var result = controller.Confirm() as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
        }
    }
}
