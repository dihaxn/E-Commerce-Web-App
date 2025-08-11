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
    public class StoreControllerTests
    {
        private DbContextOptions<ApplicationDbContext> _dbOptions;

        public StoreControllerTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
        }

        private ApplicationDbContext CreateContext() => new ApplicationDbContext(_dbOptions);

        [Fact]
        public void Index_ReturnsAViewResult_WithAStoreSearchModel()
        {
            // Arrange
            using var context = CreateContext();
            context.Products.AddRange(
                new Product { Id = 1, Name = "Laptop", Brand = "Dell", Category = "Electronics", Price = 1200 },
                new Product { Id = 2, Name = "Mouse", Brand = "Logitech", Category = "Electronics", Price = 50 }
            );
            context.SaveChanges();
            var controller = new StoreController(context);

            // Act
            var result = controller.Index(1, null, null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<StoreSearchModel>(viewResult.ViewData.Model);
            var products = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.ViewData["Products"]);
            Assert.Equal(2, products.Count());
        }

        [Fact]
        public void Index_Search_ReturnsFilteredProducts()
        {
            // Arrange
            using var context = CreateContext();
            context.Products.AddRange(
                new Product { Id = 1, Name = "Gaming Laptop", Brand = "Dell", Category = "Electronics", Price = 1500 },
                new Product { Id = 2, Name = "Office Mouse", Brand = "Logitech", Category = "Electronics", Price = 50 }
            );
            context.SaveChanges();
            var controller = new StoreController(context);

            // Act
            var result = controller.Index(1, "Laptop", null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var products = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.ViewData["Products"]);
            Assert.Single(products);
            Assert.Equal("Gaming Laptop", products.First().Name);
        }

        [Fact]
        public void Details_ReturnsViewResult_WithProduct_WhenProductExists()
        {
            // Arrange
            using var context = CreateContext();
            var product = new Product { Id = 1, Name = "Test Product" };
            context.Products.Add(product);
            context.SaveChanges();
            var controller = new StoreController(context);

            // Act
            var result = controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<Product>(viewResult.ViewData.Model);
            Assert.Equal(1, model.Id);
        }

        [Fact]
        public void Details_RedirectsToIndex_WhenProductDoesNotExist()
        {
            // Arrange
            using var context = CreateContext();
            var controller = new StoreController(context);

            // Act
            var result = controller.Details(999);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }
    }
}
