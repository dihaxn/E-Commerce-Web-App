using E_Commerce_BE.Controllers;
using E_Commerce_BE.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace E_Commerce_BE.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<RoleManager<IdentityRole>> _roleManager;

        public UsersControllerTests()
        {
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManager = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);

            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            _roleManager = new Mock<RoleManager<IdentityRole>>(roleStoreMock.Object, null, null, null, null);
        }

        private UsersController CreateController(ApplicationUser currentUser = null)
        {
            var controller = new UsersController(_userManager.Object, _roleManager.Object);
            if (currentUser != null)
            {
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, currentUser.Id)
                }));
                var httpContext = new Mock<Microsoft.AspNetCore.Http.HttpContext>();
                httpContext.Setup(ctx => ctx.User).Returns(claimsPrincipal);
                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = httpContext.Object
                };

                _userManager.Setup(um => um.GetUserAsync(claimsPrincipal)).ReturnsAsync(currentUser);
            }
            return controller;
        }

        [Fact]
        public void Index_ReturnsViewWithUsers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", UserName = "user1" },
                new ApplicationUser { Id = "2", UserName = "user2" }
            }.AsQueryable();

            _userManager.Setup(um => um.Users).Returns(users);
            var controller = CreateController();

            // Act
            var result = controller.Index(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<List<ApplicationUser>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.Details(null) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }

        [Fact]
        public async Task EditRole_RedirectsToIndex_WhenUserNotFound()
        {
            // Arrange
            _userManager.Setup(um => um.FindByIdAsync("non-existent-id")).ReturnsAsync((ApplicationUser)null);
            var controller = CreateController();

            // Act
            var result = await controller.EditRole("non-existent-id", "admin") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }

        [Fact]
        public async Task DeleteAccount_CannotDeleteOwnAccount()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user-id", UserName = "testuser" };
            _userManager.Setup(um => um.FindByIdAsync(user.Id)).ReturnsAsync(user);
            var controller = CreateController(user);
            var tempData = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary>();
            controller.TempData = tempData.Object;


            // Act
            var result = await controller.DeleteAccount(user.Id) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Details", result.ActionName);
            Assert.Equal(user.Id, result.RouteValues["id"]);
            tempData.VerifySet(td => td["ErrorMessage"] = "You cannot delete your own account!");
        }
    }
}
