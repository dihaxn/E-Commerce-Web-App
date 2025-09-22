using E_Commerce_BE.Controllers;
using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace E_Commerce_BE.Tests.Controllers
{
    public class ClientOrdersControllerTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;

        public ClientOrdersControllerTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_ClientOrders")
                .Options;

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
        }

        private ApplicationDbContext GetInMemoryDbContext()
        {
            var context = new ApplicationDbContext(_options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            return context;
        }

        private ClientOrdersController CreateController(ApplicationDbContext context, ApplicationUser user)
        {
            _userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var controller = new ClientOrdersController(context, _userManager.Object);
            var httpContext = new DefaultHttpContext();
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            }));
            httpContext.User = claimsPrincipal;
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            return controller;
        }

        [Fact]
        public async Task Index_ReturnsViewWithClientOrders()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var user = new ApplicationUser { Id = "test-client-id", UserName = "testclient" };
            context.Users.Add(user);
            context.Orders.AddRange(
                new Order { ClientId = user.Id, DeliveryAddress = "Address 1" },
                new Order { ClientId = "other-client", DeliveryAddress = "Address 2" }
            );
            context.SaveChanges();

            var controller = CreateController(context, user);

            // Act
            var result = await controller.Index(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var orders = Assert.IsAssignableFrom<List<Order>>(result.ViewData["Orders"]);
            Assert.Single(orders);
            Assert.Equal(user.Id, orders.First().ClientId);
        }

        [Fact]
        public async Task Details_ReturnsViewWithOrderDetails_ForOwner()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var user = new ApplicationUser { Id = "test-client-id", UserName = "testclient" };
            var product = new Product { Id = 1, Name = "Test Product", Price = 10 };
            var orderItem = new OrderItem { Product = product, Quantity = 1, UnitPrice = 10 };
            var order = new Order { Id = 1, ClientId = user.Id, DeliveryAddress = "Test Address", Items = new List<OrderItem> { orderItem } };
            context.Users.Add(user);
            context.Products.Add(product);
            context.Orders.Add(order);
            context.SaveChanges();

            var controller = CreateController(context, user);

            // Act
            var result = await controller.Details(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<Order>(result.Model);
            Assert.Equal(order.Id, model.Id);
        }

        [Fact]
        public async Task Details_RedirectsToIndex_ForNonExistentOrder()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var user = new ApplicationUser { Id = "test-client-id", UserName = "testclient" };
            context.Users.Add(user);
            context.SaveChanges();

            var controller = CreateController(context, user);

            // Act
            var result = await controller.Details(999) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }

        [Fact]
        public async Task Details_RedirectsToIndex_ForOrderNotOwnedByClient()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var owner = new ApplicationUser { Id = "owner-id", UserName = "owner" };
            var viewer = new ApplicationUser { Id = "viewer-id", UserName = "viewer" };
            var order = new Order { Id = 1, ClientId = owner.Id, DeliveryAddress = "Test Address" };
            context.Users.AddRange(owner, viewer);
            context.Orders.Add(order);
            context.SaveChanges();

            var controller = CreateController(context, viewer);

            // Act
            var result = await controller.Details(1) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }
    }
}
