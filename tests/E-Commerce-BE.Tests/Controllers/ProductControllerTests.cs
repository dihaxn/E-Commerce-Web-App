using Xunit;
using Moq;
using E_Commerce_BE.Controllers;
using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace E_Commerce_BE.Tests.Controllers
{
    public class ProductControllerTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly Mock<ISecureFileUploadService> _mockFileUploadService;
        private readonly Mock<ISanitizationService> _mockSanitizationService;

        public ProductControllerTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _mockWebHostEnvironment.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

            _mockFileUploadService = new Mock<ISecureFileUploadService>();
            _mockSanitizationService = new Mock<ISanitizationService>();
            _mockSanitizationService.Setup(s => s.Sanitize(It.IsAny<string>())).Returns((string s) => s);
        }

        private ApplicationDbContext CreateContext() => new ApplicationDbContext(_dbOptions);

        [Fact]
        public void Index_ReturnsAViewResult_WithAListOfProducts()
        {
            // Arrange
            using var context = CreateContext();
            context.Products.AddRange(
                new Product { Id = 1, Name = "Laptop", Brand = "Dell", Category = "Electronics", Price = 1200 },
                new Product { Id = 2, Name = "Mouse", Brand = "Logitech", Category = "Electronics", Price = 50 }
            );
            context.SaveChanges();
            var controller = new ProductController(context, _mockWebHostEnvironment.Object, _mockFileUploadService.Object, _mockSanitizationService.Object);

            // Act
            var result = controller.Index(1, null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public void Create_GET_ReturnsViewResult()
        {
            // Arrange
            using var context = CreateContext();
            var controller = new ProductController(context, _mockWebHostEnvironment.Object, _mockFileUploadService.Object, _mockSanitizationService.Object);

            // Act
            var result = controller.Create();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_POST_RedirectsToIndex_WhenModelIsValid()
        {
            // Arrange
            using var context = CreateContext();
            _mockFileUploadService.Setup(s => s.ValidateAndSaveFileAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync((true, "test.jpg", null));

            var controller = new ProductController(context, _mockWebHostEnvironment.Object, _mockFileUploadService.Object, _mockSanitizationService.Object);
            var mockImage = new Mock<IFormFile>();

            var productDto = new ProductDto
            {
                Name = "New Product",
                Brand = "New Brand",
                Category = "New Category",
                Price = 100,
                Description = "New Description",
                ImageFile = mockImage.Object
            };

            var productsPath = Path.Combine(_mockWebHostEnvironment.Object.WebRootPath, "products");
            if (!Directory.Exists(productsPath))
            {
                Directory.CreateDirectory(productsPath);
            }

            // Act
            var result = await controller.Create(productDto);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(1, context.Products.Count());
        }
        [Fact]
        public void Edit_GET_ReturnsViewResult_WithProductDto()
        {
            // Arrange
            using var context = CreateContext();
            var product = new Product { Id = 1, Name = "Test Product" };
            context.Products.Add(product);
            context.SaveChanges();
            var controller = new ProductController(context, _mockWebHostEnvironment.Object, _mockFileUploadService.Object, _mockSanitizationService.Object);

            // Act
            var result = controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<ProductDto>(viewResult.ViewData.Model);
            Assert.Equal("Test Product", model.Name);
        }

        [Fact]
        public void Delete_RedirectsToIndex_AndRemovesProduct()
        {
            // Arrange
            using var context = CreateContext();
            var product = new Product { Id = 1, Name = "Test Product", ImageFileName = "test.jpg" };
            context.Products.Add(product);
            context.SaveChanges();

            // Create a dummy file to be "deleted"
            var dummyFilePath = Path.Combine(_mockWebHostEnvironment.Object.WebRootPath, "products", product.ImageFileName);
            var imageDirectory = Path.GetDirectoryName(dummyFilePath);
            if (imageDirectory != null)
            {
                Directory.CreateDirectory(imageDirectory);
            }
            File.Create(dummyFilePath).Close();


            var controller = new ProductController(context, _mockWebHostEnvironment.Object, _mockFileUploadService.Object, _mockSanitizationService.Object);

            // Act
            var result = controller.Delete(1);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(0, context.Products.Count());
        }
    }
}
