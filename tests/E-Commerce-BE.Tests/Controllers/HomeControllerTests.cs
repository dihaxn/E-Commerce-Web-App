using Xunit;
using E_Commerce_BE.Controllers;
using Microsoft.AspNetCore.Mvc;
using E_Commerce_BE.Services;
using Microsoft.EntityFrameworkCore;
using E_Commerce_BE.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace E_Commerce_BE.Tests.Controllers
{
    public class HomeControllerTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            var dbContext = new ApplicationDbContext(options);
            return dbContext;
        }

        [Fact]
        public void Index_ReturnsAViewResult_WithAListOfProducts()
        {
            // Arrange
            var dbContext = GetDbContext();
            dbContext.Products.Add(new Product { Id = 1, Name = "Test Product 1", CreatedAt = System.DateTime.Now.AddDays(-1) });
            dbContext.Products.Add(new Product { Id = 2, Name = "Test Product 2", CreatedAt = System.DateTime.Now });
            dbContext.SaveChanges();

            var controller = new HomeController(dbContext);

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public void AboutUs_ReturnsAViewResult()
        {
            // Arrange
            var dbContext = GetDbContext();
            var controller = new HomeController(dbContext);

            // Act
            var result = controller.AboutUs();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_ReturnsAViewResult_WithErrorViewModel()
        {
            // Arrange
            var dbContext = GetDbContext();
            var controller = new HomeController(dbContext);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<ErrorViewModel>(viewResult.ViewData.Model);
        }
    }
}
