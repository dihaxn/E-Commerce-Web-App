using Xunit;
using Moq;
using E_Commerce_BE.Controllers;
using E_Commerce_BE.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace E_Commerce_BE.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);

            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(_mockUserManager.Object, contextAccessorMock.Object, userPrincipalFactoryMock.Object, null, null, null, null);

            _mockConfiguration = new Mock<IConfiguration>();

            _controller = new AccountController(_mockUserManager.Object, _mockSignInManager.Object, _mockConfiguration.Object);
        }

        private void SetupUser(bool isSignedIn)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "test@example.com"),
            }, "mock"));

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(c => c.User).Returns(user);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = mockHttpContext.Object,
            };

            _mockSignInManager.Setup(s => s.IsSignedIn(It.IsAny<ClaimsPrincipal>())).Returns(isSignedIn);
        }

        [Fact]
        public void Register_GET_ReturnsViewResult_WhenUserNotSignedIn()
        {
            // Arrange
            SetupUser(false);

            // Act
            var result = _controller.Register();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Register_GET_RedirectsToHome_WhenUserIsSignedIn()
        {
            // Arrange
            SetupUser(true);

            // Act
            var result = _controller.Register();

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }

        [Fact]
        public async Task Register_POST_ReturnsRedirectToHome_WhenModelIsValidAndRegistrationSucceeds()
        {
            // Arrange
            SetupUser(false);
            var registerDto = new RegisterDto { Email = "test@test.com", Password = "Password123" };
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }

        [Fact]
        public async Task Login_POST_ReturnsRedirectToHome_WhenCredentialsAreValid()
        {
            // Arrange
            SetupUser(false);
            var loginDto = new LoginDto { Email = "test@test.com", Password = "Password123" };
            _mockSignInManager.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }

        [Fact]
        public async Task Logout_RedirectsToHome()
        {
            // Arrange
            SetupUser(true);

            // Act
            var result = await _controller.Logout();

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }
    }
}
