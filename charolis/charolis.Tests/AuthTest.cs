using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using charolis.DAL;
using charolis.Entities;
using charolis.Controllers;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Routing;

namespace charolis.Tests.Controllers
{
    public class AccountControllerTests
    {
        private EfContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<EfContext>()
                .UseInMemoryDatabase(databaseName: $"AuthTestDb_{System.Guid.NewGuid()}")
                .Options;

            return new EfContext(options);
        }

        [Fact]
        public async Task Login_Success_RedirectsToHomeIndex()
        {
            // Arrange
            var context = GetInMemoryContext();
            var password = "test123";
            var user = new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "User"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new AccountController(context);

            // Mock SignInAsync
            var authMock = new Mock<IAuthenticationService>();
            authMock.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(),
                                               It.IsAny<string>(),
                                               It.IsAny<ClaimsPrincipal>(),
                                               It.IsAny<AuthenticationProperties>()))
                    .Returns(Task.CompletedTask);

            // Mock IUrlHelperFactory
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
                .Returns("/Home/Index");

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            // Service Provider with dependencies
            var serviceProvider = new ServiceCollection()
                .AddSingleton(authMock.Object)
                .AddSingleton(urlHelperFactory.Object)
                .BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.Login("testuser", password);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsViewWithError()
        {
            // Arrange
            var context = GetInMemoryContext();
            var controller = new AccountController(context);

            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.Login("nonexist", "wrongpass");

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState, ms => ms.Value.Errors.Count > 0);
        }
    }
}
