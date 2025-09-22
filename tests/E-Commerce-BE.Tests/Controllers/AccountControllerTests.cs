using E_Commerce_BE.Controllers;
using E_Commerce_BE.Models;
using E_Commerce_BE.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace E_Commerce_BE.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IRateLimitingService> _mockRateLimitingService;
        private readonly Mock<ISanitizationService> _mockSanitizationService;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            // Mock UserManager
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
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

            // Mock SignInManager
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                httpContextAccessorMock.Object,
                userPrincipalFactoryMock.Object,
                new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>().Object,
                new Mock<Microsoft.Extensions.Logging.ILogger<SignInManager<ApplicationUser>>>().Object,
                new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().Object,
                new Mock<IUserConfirmation<ApplicationUser>>().Object
            );

            // Mock IConfiguration
            _mockConfiguration = new Mock<IConfiguration>();

            // Mock RateLimitingService
            _mockRateLimitingService = new Mock<IRateLimitingService>();

            // Mock SanitizationService
            _mockSanitizationService = new Mock<ISanitizationService>();
            _mockSanitizationService.Setup(s => s.Sanitize(It.IsAny<string>())).Returns((string s) => s);


            // Instantiate the Controller with mocks
            _controller = new AccountController(_mockUserManager.Object, _mockSignInManager.Object, _mockConfiguration.Object, _mockRateLimitingService.Object, _mockSanitizationService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    // Mock HttpContext and RemoteIpAddress for rate limiting tests
                    HttpContext = new DefaultHttpContext { Connection = { RemoteIpAddress = new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 }) } }
                }
            };
        }

        private void SetupUser(bool isSignedIn)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "test@example.com"),
            }, "mock"));

            _controller.ControllerContext.HttpContext.User = user;
            _mockSignInManager.Setup(s => s.IsSignedIn(It.IsAny<ClaimsPrincipal>())).Returns(isSignedIn);
        }

        // =================== Register Tests ===================

        [Fact]
        public async Task Register_Post_ReturnsRedirectToHome_WhenRegistrationIsSuccessful()
        {
            // Arrange
            SetupUser(false);
            var registerDto = new RegisterDto { Email = "test@example.com", Password = "Password123!" };
            _mockUserManager.Setup(um => um.FindByEmailAsync(registerDto.Email)).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "client")).ReturnsAsync(IdentityResult.Success);
            _mockSignInManager.Setup(sm => sm.SignInAsync(It.IsAny<ApplicationUser>(), false, null)).Returns(Task.CompletedTask);
            _mockRateLimitingService.Setup(r => r.IsRateLimited(It.IsAny<string>(), "register")).Returns(false);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }

        [Fact]
        public async Task Register_Post_ReturnsViewWithError_WhenUserAlreadyExists()
        {
            // Arrange
            SetupUser(false);
            var registerDto = new RegisterDto { Email = "existing@example.com" };
            _mockUserManager.Setup(um => um.FindByEmailAsync(registerDto.Email)).ReturnsAsync(new ApplicationUser());
            _mockRateLimitingService.Setup(r => r.IsRateLimited(It.IsAny<string>(), "register")).Returns(false);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.NotNull(_controller.ModelState["Email"]);
            Assert.Equal("Email address is already registered.", _controller.ModelState["Email"]?.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Register_Post_ReturnsViewWithError_WhenCreateAsyncFails()
        {
            // Arrange
            SetupUser(false);
            var registerDto = new RegisterDto { Email = "test@example.com", Password = "password" };
            var identityError = new IdentityError { Description = "Password is too weak." };
            _mockUserManager.Setup(um => um.FindByEmailAsync(registerDto.Email)).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password)).ReturnsAsync(IdentityResult.Failed(identityError));
            _mockRateLimitingService.Setup(r => r.IsRateLimited(It.IsAny<string>(), "register")).Returns(false);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.NotNull(_controller.ModelState[string.Empty]);
            Assert.Equal(identityError.Description, _controller.ModelState[string.Empty]?.Errors[0].ErrorMessage);
        }

        // =================== Login Tests ===================

        [Fact]
        public async Task Login_Post_ReturnsRedirectToHome_WhenLoginIsSuccessful()
        {
            // Arrange
            SetupUser(false);
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Password123!" };
            _mockSignInManager.Setup(sm => sm.PasswordSignInAsync(loginDto.Email, loginDto.Password, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _mockRateLimitingService.Setup(r => r.IsRateLimited(It.IsAny<string>(), "login")).Returns(false);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_ReturnsViewWithError_WhenLoginFails()
        {
            // Arrange
            SetupUser(false);
            var loginDto = new LoginDto { Email = "test@example.com", Password = "wrongpassword" };
            _mockSignInManager.Setup(sm => sm.PasswordSignInAsync(loginDto.Email, loginDto.Password, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
            _mockRateLimitingService.Setup(r => r.IsRateLimited(It.IsAny<string>(), "login")).Returns(false);
            _mockRateLimitingService.Setup(r => r.GetRateLimitStatus(It.IsAny<string>(), "login"))
                .Returns(new RateLimitStatus { RemainingAttempts = 4 });

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(_controller.ViewBag.ErrorMessage);
        }

        // =================== Profile Tests ===================

        [Fact]
        public async Task Profile_Post_ReturnsViewWithSuccessMessage_WhenUpdateIsSuccessful()
        {
            // Arrange
            var user = new ApplicationUser { Email = "test@example.com" };
            var profileDto = new ProfileDto { Email = "new@example.com", FirstName = "Test", LastName = "User" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            SetupUser(true);

            // Act
            var result = await _controller.Profile(profileDto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["SuccessMessage"]);
            Assert.Equal("Profile updated successfully", viewResult.ViewData["SuccessMessage"]);
            Assert.Equal(profileDto.Email, user.Email);
        }

        // =================== Password Tests ===================

        [Fact]
        public async Task Password_Post_ReturnsViewWithSuccessMessage_WhenPasswordIsChangedSuccessfully()
        {
            // Arrange
            var user = new ApplicationUser();
            var passwordDto = new PasswordDto { CurrentPassword = "OldPassword", NewPassword = "NewPassword123!", ConfirmPassword = "NewPassword123!" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.ChangePasswordAsync(user, passwordDto.CurrentPassword, passwordDto.NewPassword)).ReturnsAsync(IdentityResult.Success);
            SetupUser(true);

            // Act
            var result = await _controller.Password(passwordDto);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["SuccessMessage"]);
            Assert.Equal("Password updated successfully!", viewResult.ViewData["SuccessMessage"]);
        }
    }
}
