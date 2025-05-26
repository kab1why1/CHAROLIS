using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using charolis.Controllers;
using charolis.DAL;
using charolis.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace charolis.Tests.Controllers
{
    public class ProductControllerTests
    {
        private EfContext GetContext()
        {
            var options = new DbContextOptionsBuilder<EfContext>()
                .UseInMemoryDatabase(databaseName: $"ProductTestDb_{System.Guid.NewGuid()}")
                .Options;
            return new EfContext(options);
        }

        private ProductController SetupController(EfContext context)
        {
            var controller = new ProductController(context);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var tempData = new Mock<ITempDataDictionary>();
            controller.TempData = tempData.Object;

            return controller;
        }

        [Fact]
        public async Task Index_ReturnsViewWithProducts()
        {
            var context = GetContext();
            context.Products.Add(new Product { Id = 1, Name = "P1", Price = 10 });
            await context.SaveChangesAsync();

            var controller = SetupController(context);
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Product>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_ValidProduct_RedirectsToIndex()
        {
            var context = GetContext();
            var controller = SetupController(context);

            var product = new Product { Name = "Test Product", Price = 15 };

            var result = await controller.Create(product);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Single(context.Products);
        }

        [Fact]
        public async Task Create_InvalidModel_ReturnsView()
        {
            var context = GetContext();
            var controller = SetupController(context);
            controller.ModelState.AddModelError("Name", "Required");

            var product = new Product();

            var result = await controller.Create(product);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(product, viewResult.Model);
        }

        [Fact]
        public async Task Edit_Get_ReturnsProduct()
        {
            var context = GetContext();
            var product = new Product { Id = 1, Name = "Prod", Price = 5 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var controller = SetupController(context);
            var result = await controller.Edit(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Product>(viewResult.Model);
            Assert.Equal(1, model.Id);
        }

        [Fact]
        public async Task Edit_Post_ValidUpdate_RedirectsToIndex()
        {
            var context = GetContext();
            var product = new Product { Id = 1, Name = "Old", Price = 10 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var controller = SetupController(context);

            // Очищаємо ChangeTracker, щоб уникнути дублю
            context.ChangeTracker.Clear();

            var updated = new Product { Id = 1, Name = "New", Price = 20 };

            var result = await controller.Edit(1, updated);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var updatedProduct = await context.Products.FindAsync(1);
            Assert.Equal("New", updatedProduct.Name);
            Assert.Equal(20, updatedProduct.Price);
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsView()
        {
            var context = GetContext();
            var controller = SetupController(context);
            controller.ModelState.AddModelError("Name", "Required");

            var product = new Product { Id = 1 };

            var result = await controller.Edit(1, product);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(product, viewResult.Model);
        }

        [Fact]
        public async Task DeleteConfirmed_RemovesProduct()
        {
            var context = GetContext();
            var product = new Product { Id = 1, Name = "Prod", Price = 10 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var controller = SetupController(context);
            var result = await controller.DeleteConfirmed(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Empty(context.Products);
        }

        [Fact]
        public async Task Details_ReturnsProductWithReviews()
        {
            var context = GetContext();
            var user = new User { Id = 1, Username = "user", PasswordHash = "hash" };
            var product = new Product
            {
                Id = 1,
                Name = "Prod",
                Price = 20,
                Reviews = new List<Review>
                {
                    new Review { Id = 1, Comment = "Good", User = user }
                }
            };
            context.Users.Add(user);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var controller = SetupController(context);
            var result = await controller.Details(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Product>(viewResult.Model);
            Assert.Single(model.Reviews);
        }
    }
}
