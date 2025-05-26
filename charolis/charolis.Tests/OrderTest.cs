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
    public class OrderControllerTests
    {
        private EfContext GetContext()
        {
            var options = new DbContextOptionsBuilder<EfContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{System.Guid.NewGuid()}")
                .Options;
            return new EfContext(options);
        }

        private OrderController SetupController(EfContext context)
        {
            var controller = new OrderController(context);
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
        public async Task Index_ReturnsViewWithOrders()
        {
            var context = GetContext();
            var user = new User { Id = 1, Username = "test", PasswordHash = "hash" };
            context.Users.Add(user);
            context.Orders.Add(new Order
            {
                Id = 1,
                UserId = 1,
                User = user,
                Items = new List<OrderItem>()
            });
            await context.SaveChangesAsync();

            var controller = SetupController(context);
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Order>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_ValidPost_CreatesOrder()
        {
            var context = GetContext();
            context.Users.Add(new User { Id = 1, Username = "user", PasswordHash = "dummyhash" });
            context.Products.Add(new Product { Id = 1, Name = "Prod", Price = 10 });
            await context.SaveChangesAsync();

            var controller = SetupController(context);
            var result = await controller.Create(1, new[] { 1 }, new[] { 2 });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Single(context.Orders);
        }

        [Fact]
        public async Task Create_InvalidUser_ReturnsViewWithError()
        {
            var context = GetContext();
            context.Products.Add(new Product { Id = 1, Name = "P1", Price = 20 });
            await context.SaveChangesAsync();

            var controller = SetupController(context);
            var result = await controller.Create(99, new[] { 1 }, new[] { 2 });

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
        }

        [Fact]
        public async Task DeleteConfirmed_RemovesOrder()
        {
            var context = GetContext();
            var product = new Product { Id = 1, Name = "Test Product", Price = 10 };
            var orderItem = new OrderItem
            {
                Id = 1,
                Product = product,
                ProductId = 1,
                Quantity = 1,
                PriceAtPurchase = 10
            };
            var order = new Order { Id = 1, Items = new List<OrderItem> { orderItem } };

            context.Products.Add(product);
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = SetupController(context);
            var result = controller.DeleteConfirmed(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Empty(context.Orders);
            Assert.Empty(context.OrderItems);
        }

        [Fact]
        public async Task Edit_ValidUpdate_ChangesOrder()
        {
            var context = GetContext();
            var user = new User { Id = 1, Username = "user", PasswordHash = "dummyhash" };
            var product = new Product { Id = 1, Name = "Product 1", Price = 10 };

            context.Users.Add(user);
            context.Products.Add(product);

            var orderItem = new OrderItem { ProductId = 1, Quantity = 1, PriceAtPurchase = 10 };
            var order = new Order { Id = 1, UserId = 1, Items = new List<OrderItem> { orderItem }, Total = 10 };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = SetupController(context);
            var result = await controller.Edit(1, 1, new[] { 1 }, new[] { 2 });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var updatedOrder = context.Orders.Include(o => o.Items).First();
            Assert.Single(updatedOrder.Items);
            Assert.Equal(20, updatedOrder.Total);
        }
    }
}
