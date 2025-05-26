using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using charolis.Controllers;
using charolis.DAL;
using charolis.Entities;
using charolis.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace charolis.Tests.Controllers
{
    public class CartControllerTests
    {
        private EfContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<EfContext>()
                .UseInMemoryDatabase(databaseName: $"CartTestDb_{System.Guid.NewGuid()}")
                .Options;

            return new EfContext(options);
        }

        private CartController SetupControllerWithUser(EfContext context, string username)
        {
            var controller = new CartController(context);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "mock"));

            var httpContext = new DefaultHttpContext
            {
                User = user
            };

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            // ✅ Реальна TempData, яка працює як словник
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            return controller;
        }

        [Fact]
        public async Task Index_ReturnsViewWithCartViewModel()
        {
            var context = GetInMemoryContext();

            var user = new User
            {
                Username = "testuser",
                Balance = 1000m,
                Id = 1,
                PasswordHash = "dummyhash"
            };
            context.Users.Add(user);

            var product = new Product { Id = 1, Name = "Product1", Price = 100m };
            context.Products.Add(product);

            var order = new Order
            {
                Id = 1,
                UserId = user.Id,
                IsPaid = false,
                Total = 100m,
                CreatedAt = System.DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 1, OrderId = 1, ProductId = product.Id, Quantity = 1, Product = product }
                }
            };
            context.Orders.Add(order);

            await context.SaveChangesAsync();

            var controller = SetupControllerWithUser(context, "testuser");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<CartViewModel>(viewResult.Model);

            Assert.Equal(user.Balance, model.Balance);
            Assert.Single(model.Orders);
            Assert.Equal(order.Id, model.Orders[0].Id);
        }

        [Fact]
        public async Task Pay_SuccessfulPayment_UpdatesBalanceAndOrder()
        {
            var context = GetInMemoryContext();

            var user = new User
            {
                Username = "testuser",
                Balance = 200m,
                Id = 1,
                PasswordHash = "dummyhash"
            };
            context.Users.Add(user);

            var order = new Order
            {
                Id = 1,
                UserId = user.Id,
                IsPaid = false,
                Total = 100m,
                Items = new List<OrderItem>(),
            };
            context.Orders.Add(order);

            await context.SaveChangesAsync();

            var controller = SetupControllerWithUser(context, "testuser");

            var result = await controller.Pay(order.Id);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var updatedUser = await context.Users.FindAsync(user.Id);
            var updatedOrder = await context.Orders.FindAsync(order.Id);

            Assert.Equal(100m, updatedUser.Balance); // 200 - 100
            Assert.True(updatedOrder.IsPaid);
            Assert.Equal("Замовлення оплачено успішно.", controller.TempData["Message"]);
        }

        [Fact]
        public async Task Pay_AlreadyPaidOrder_ShowsMessage()
        {
            var context = GetInMemoryContext();

            var user = new User
            {
                Username = "testuser",
                Balance = 200m,
                Id = 1,
                PasswordHash = "dummyhash"
            };
            context.Users.Add(user);

            var order = new Order
            {
                Id = 1,
                UserId = user.Id,
                IsPaid = true,
                Total = 100m,
                Items = new List<OrderItem>(),
            };
            context.Orders.Add(order);

            await context.SaveChangesAsync();

            var controller = SetupControllerWithUser(context, "testuser");

            var result = await controller.Pay(order.Id);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Це замовлення вже оплачено.", controller.TempData["Message"]);
        }

        [Fact]
        public async Task Pay_InsufficientBalance_ShowsError()
        {
            var context = GetInMemoryContext();

            var user = new User
            {
                Username = "testuser",
                Balance = 50m,
                Id = 1,
                PasswordHash = "dummyhash"
            };
            context.Users.Add(user);

            var order = new Order
            {
                Id = 1,
                UserId = user.Id,
                IsPaid = false,
                Total = 100m,
                Items = new List<OrderItem>(),
            };
            context.Orders.Add(order);

            await context.SaveChangesAsync();

            var controller = SetupControllerWithUser(context, "testuser");

            var result = await controller.Pay(order.Id);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Недостатньо коштів для оплати.", controller.TempData["Error"]);
        }

        [Fact]
        public async Task CancelPayment_SuccessfulRefund_UpdatesBalanceAndOrder()
        {
            var context = GetInMemoryContext();

            var user = new User
            {
                Username = "testuser",
                Balance = 100m,
                Id = 1,
                PasswordHash = "dummyhash"
            };
            context.Users.Add(user);

            var order = new Order
            {
                Id = 1,
                UserId = user.Id,
                IsPaid = true,
                Total = 50m,
                Items = new List<OrderItem>(),
            };
            context.Orders.Add(order);

            await context.SaveChangesAsync();

            var controller = SetupControllerWithUser(context, "testuser");

            var result = await controller.CancelPayment(order.Id);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var updatedUser = await context.Users.FindAsync(user.Id);
            var updatedOrder = await context.Orders.FindAsync(order.Id);

            Assert.Equal(150m, updatedUser.Balance); // 100 + 50
            Assert.False(updatedOrder.IsPaid);
            Assert.Equal("Оплата скасована, кошти повернено на баланс.", controller.TempData["Message"]);
        }

        [Fact]
        public async Task CancelPayment_OrderNotPaid_ShowsMessage()
        {
            var context = GetInMemoryContext();

            var user = new User
            {
                Username = "testuser",
                Balance = 100m,
                Id = 1,
                PasswordHash = "dummyhash"
            };
            context.Users.Add(user);

            var order = new Order
            {
                Id = 1,
                UserId = user.Id,
                IsPaid = false,
                Total = 50m,
                Items = new List<OrderItem>(),
            };
            context.Orders.Add(order);

            await context.SaveChangesAsync();

            var controller = SetupControllerWithUser(context, "testuser");

            var result = await controller.CancelPayment(order.Id);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Це замовлення не оплачено.", controller.TempData["Message"]);
        }
    }
}
