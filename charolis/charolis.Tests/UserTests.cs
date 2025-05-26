using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using charolis.Controllers;
using charolis.DAL;
using charolis.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace charolis.Tests.Controllers
{
    public class UserControllerTests
    {
        private EfContext GetContext()
        {
            var options = new DbContextOptionsBuilder<EfContext>()
                .UseInMemoryDatabase(databaseName: $"UserTestDb_{System.Guid.NewGuid()}")
                .Options;
            return new EfContext(options);
        }

        private UserController SetupController(EfContext context, string username, string? role = null)
        {
            var controller = new UserController(context);

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, username) };
            if (role != null)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            return controller;
        }

        [Fact]
        public async Task Index_ReturnsViewWithUsers()
        {
            var context = GetContext();
            context.Users.Add(new User { Id = 1, Username = "user1", Email = "a@b.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass") });
            context.Users.Add(new User { Id = 2, Username = "user2", Email = "c@d.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass") });
            await context.SaveChangesAsync();

            var controller = SetupController(context, "user1");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<User>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public void Create_GET_ReturnsViewWithRoles()
        {
            var context = GetContext();
            var controller = SetupController(context, "user1");

            var result = controller.Create();

            var viewResult = Assert.IsType<ViewResult>(result);

            var roles = controller.ViewBag.Roles as string[];
            Assert.NotNull(roles);
            Assert.Contains("User", roles);
            Assert.Contains("Admin", roles);
        }

        [Fact]
        public async Task Create_POST_ValidData_CreatesUserAndRedirects()
        {
            var context = GetContext();
            var controller = SetupController(context, "admin", "Admin");

            var result = await controller.Create(
                username: "newuser",
                email: "newuser@example.com",
                phoneNumber: "123456",
                address: "Some address",
                role: "User",
                password: "password123",
                confirmPassword: "password123");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
            Assert.NotNull(userInDb);
            Assert.Equal("newuser@example.com", userInDb.Email);
            Assert.True(BCrypt.Net.BCrypt.Verify("password123", userInDb.PasswordHash));
        }

        [Fact]
        public async Task Create_POST_InvalidData_ReturnsViewWithErrors()
        {
            var context = GetContext();
            var controller = SetupController(context, "admin", "Admin");

            // Додаємо користувача з необхідним PasswordHash для уникнення помилки EF
            context.Users.Add(new User 
            { 
                Username = "existing", 
                Email = "exist@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("somepassword") 
            });
            await context.SaveChangesAsync();

            var result = await controller.Create(
                username: "existing",
                email: "exist@example.com",
                phoneNumber: null,
                address: null,
                role: "User",
                password: "123",
                confirmPassword: "321");

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.NotEmpty(controller.ModelState[""].Errors);

            var roles = controller.ViewBag.Roles as string[];
            Assert.NotNull(roles);
            Assert.Contains("User", roles);
            Assert.Contains("Admin", roles);
        }

        [Fact]
        public async Task Edit_GET_AdminEditingOtherUser_ReturnsUserView()
        {
            var context = GetContext();
            var user = new User { Id = 1, Username = "user1", Email = "user1@example.com", Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass") };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = SetupController(context, "admin", "Admin");

            var result = await controller.Edit(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<User>(viewResult.Model);
            Assert.Equal("user1", model.Username);

            var roles = controller.ViewBag.Roles as string[];
            Assert.NotNull(roles);
        }

        [Fact]
        public async Task Edit_GET_UserEditingSelf_ReturnsUserView()
        {
            var context = GetContext();
            var user = new User { Id = 1, Username = "user1", Email = "user1@example.com", Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass") };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = SetupController(context, "user1");

            var result = await controller.Edit(null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<User>(viewResult.Model);
            Assert.Equal("user1", model.Username);

            var roles = controller.ViewBag.Roles as string[];
            Assert.NotNull(roles);
        }

        [Fact]
        public async Task Edit_GET_UserEditingOtherUser_ReturnsForbid()
        {
            var context = GetContext();
            var user = new User { Id = 1, Username = "user1", Email = "user1@example.com", Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass") };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = SetupController(context, "user2");

            var result = await controller.Edit(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Edit_POST_UserEditingSelf_UpdatesProfile()
        {
            var context = GetContext();
            var user = new User { Id = 1, Username = "user1", Email = "old@example.com", Role = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass") };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = SetupController(context, "user1");

            var result = await controller.Edit(
                id: null,
                username: "user1",
                email: "new@example.com",
                phoneNumber: "12345",
                address: "Address",
                role: "Admin", // має ігноруватися
                newPassword: "newpassword",
                confirmNewPassword: "newpassword",
                balance: null // ігнорується для не адміна
            );

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);

            var updatedUser = await context.Users.FindAsync(1);
            Assert.Equal("new@example.com", updatedUser.Email);
            Assert.Equal("User", updatedUser.Role);
            Assert.True(BCrypt.Net.BCrypt.Verify("newpassword", updatedUser.PasswordHash));
        }

        [Fact]
        public async Task Edit_POST_AdminEditingOtherUser_UpdatesAllFields()
        {
            var context = GetContext();
            var user = new User { Id = 1, Username = "user1", Email = "old@example.com", Role = "User", Balance = 10m, PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass") };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = SetupController(context, "admin", "Admin");

            var result = await controller.Edit(
                id: 1,
                username: "newuser",
                email: "new@example.com",
                phoneNumber: "123",
                address: "addr",
                role: "Admin",
                newPassword: "",
                confirmNewPassword: "",
                balance: 100m
            );

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);

            var updatedUser = await context.Users.FindAsync(1);
            Assert.Equal("newuser", updatedUser.Username);
            Assert.Equal("new@example.com", updatedUser.Email);
            Assert.Equal("Admin", updatedUser.Role);
            Assert.Equal(100m, updatedUser.Balance);
            Assert.Equal("123", updatedUser.PhoneNumber);
            Assert.Equal("addr", updatedUser.Address);
        }

        [Fact]
        public async Task Delete_GET_ReturnsViewWithUser()
        {
            var context = GetContext();
            var user = new User { Id = 5, Username = "user5", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass") };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = SetupController(context, "admin", "Admin");

            var result = await controller.Delete(5);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<User>(viewResult.Model);
            Assert.Equal("user5", model.Username);
        }

        [Fact]
        public async Task Delete_GET_UserNotFound_ReturnsNotFound()
        {
            var context = GetContext();
            var controller = SetupController(context, "admin", "Admin");

            var result = await controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_RemovesUserAndRedirects()
        {
            var context = GetContext();
            var user = new User { Id = 6, Username = "user6", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass") };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = SetupController(context, "admin", "Admin");

            var result = await controller.DeleteConfirmed(6);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var userInDb = await context.Users.FindAsync(6);
            Assert.Null(userInDb);
        }

        [Fact]
        public async Task Profile_ReturnsUserView()
        {
            var context = GetContext();
            var user = new User { Id = 7, Username = "user7", Email = "user7@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass") };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = SetupController(context, "user7");

            var result = await controller.Profile();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<User>(viewResult.Model);
            Assert.Equal("user7", model.Username);
        }

        [Fact]
        public async Task Profile_UserNotFound_ReturnsNotFound()
        {
            var context = GetContext();
            var controller = SetupController(context, "nonexistent");

            var result = await controller.Profile();

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
