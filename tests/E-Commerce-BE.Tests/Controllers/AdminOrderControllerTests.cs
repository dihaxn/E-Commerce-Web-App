using Xunit;
using E_Commerce_BE.Controllers;
using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace E_Commerce_BE.Tests.Controllers
{
    public class AdminOrdersControllerTests
    {
        private DbContextOptions<ApplicationDbContext> _dbOptions;

        public AdminOrdersControllerTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
        }

        private ApplicationDbContext CreateContext() => new ApplicationDbContext(_dbOptions);

        [Fact]
        public void Index_ReturnsAViewResult_WithListOfOrders()
        {
            // Arrange
            using var context = CreateContext();
            var client = new ApplicationUser { Id = "client1", UserName = "client@test.com" };
            context.Users.Add(client);
            context.Orders.AddRange(
                new Order { Id = 1, ClientId = "client1", Client = client },
                new Order { Id = 2, ClientId = "client1", Client = client }
            );
            context.SaveChanges();
            var controller = new AdminOrdersController(context);

            // Act
            var result = controller.Index(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(viewResult.ViewData["Orders"]);
            Assert.Equal(2, orders.Count());
        }

        [Fact]
        public void Details_ReturnsViewResult_WithOrder_WhenOrderExists()
        {
            // Arrange
            using var context = CreateContext();
            var client = new ApplicationUser { Id = "client1", UserName = "client@test.com" };
            context.Users.Add(client);
            var order = new Order { Id = 1, ClientId = "client1", Client = client };
            context.Orders.Add(order);
            context.SaveChanges();
            var controller = new AdminOrdersController(context);

            // Act
            var result = controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<Order>(viewResult.ViewData.Model);
            Assert.Equal(1, model.Id);
        }

        [Fact]
        public void Details_RedirectsToIndex_WhenOrderDoesNotExist()
        {
            // Arrange
            using var context = CreateContext();
            var controller = new AdminOrdersController(context);

            // Act
            var result = controller.Details(999);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        [Fact]
        public void Edit_UpdatesOrderStatus_AndRedirectsToDetails()
        {
            // Arrange
            using var context = CreateContext();
            var order = new Order { Id = 1, OrderStatus = "Pending" };
            context.Orders.Add(order);
            context.SaveChanges();
            var controller = new AdminOrdersController(context);

            // Act
            var result = controller.Edit(1, null, "Shipped");

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectToActionResult.ActionName);
            Assert.Equal("Shipped", context.Orders.Find(1).OrderStatus);
        }
    }
}
