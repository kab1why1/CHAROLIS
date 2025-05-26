using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using charolis.Controllers;
using charolis.DAL;
using charolis.Entities;
using charolis.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace charolis.Tests.Controllers
{
    public class ReviewControllerTests
    {
        private EfContext GetContext()
        {
            var options = new DbContextOptionsBuilder<EfContext>()
                .UseInMemoryDatabase(databaseName: $"ReviewTestDb_{System.Guid.NewGuid()}")
                .Options;
            return new EfContext(options);
        }

        private ReviewController SetupController(EfContext context, int userId, string? role = null)
        {
            var controller = new ReviewController(context);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            if (role != null)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            return controller;
        }

        [Fact]
        public async Task Create_ValidModel_AddsReviewAndRedirects()
        {
            var context = GetContext();
            var controller = SetupController(context, userId: 1);

            var model = new ReviewViewModel
            {
                ProductId = 10,
                Rating = 5,
                Comment = "Great!"
            };

            var result = await controller.Create(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Product", redirect.ControllerName);
            Assert.Equal(10, redirect.RouteValues["id"]);

            var reviewInDb = await context.Reviews.FirstOrDefaultAsync();
            Assert.NotNull(reviewInDb);
            Assert.Equal(1, reviewInDb.UserId);
            Assert.Equal(10, reviewInDb.ProductId);
            Assert.Equal(5, reviewInDb.Rating);
            Assert.Equal("Great!", reviewInDb.Comment);
        }

        [Fact]
        public async Task Create_InvalidModel_RedirectsToProductDetails()
        {
            var context = GetContext();
            var controller = SetupController(context, userId: 1);
            controller.ModelState.AddModelError("Rating", "Required");

            var model = new ReviewViewModel { ProductId = 5 };

            var result = await controller.Create(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Product", redirect.ControllerName);
            Assert.Equal(5, redirect.RouteValues["id"]);

            Assert.Empty(await context.Reviews.ToListAsync());
        }

        [Fact]
        public async Task Edit_OwnerCanEdit_UpdatesReview()
        {
            var context = GetContext();
            var review = new Review
            {
                Id = 1,
                ProductId = 100,
                UserId = 2,
                Rating = 3,
                Comment = "Ok"
            };
            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            var controller = SetupController(context, userId: 2); // same user

            var model = new ReviewViewModel
            {
                Id = 1,
                ProductId = 100,
                Rating = 4,
                Comment = "Better"
            };

            var result = await controller.Edit(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Product", redirect.ControllerName);
            Assert.Equal(100, redirect.RouteValues["id"]);

            var updatedReview = await context.Reviews.FindAsync(1);
            Assert.Equal(4, updatedReview.Rating);
            Assert.Equal("Better", updatedReview.Comment);
        }

        [Fact]
        public async Task Edit_AdminCanEdit_UpdatesReview()
        {
            var context = GetContext();
            var review = new Review
            {
                Id = 2,
                ProductId = 101,
                UserId = 3,
                Rating = 2,
                Comment = "Bad"
            };
            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            var controller = SetupController(context, userId: 999, role: "Admin"); // admin user

            var model = new ReviewViewModel
            {
                Id = 2,
                ProductId = 101,
                Rating = 5,
                Comment = "Excellent"
            };

            var result = await controller.Edit(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Product", redirect.ControllerName);
            Assert.Equal(101, redirect.RouteValues["id"]);

            var updatedReview = await context.Reviews.FindAsync(2);
            Assert.Equal(5, updatedReview.Rating);
            Assert.Equal("Excellent", updatedReview.Comment);
        }

        [Fact]
        public async Task Edit_OtherUserCannotEdit_ReturnsForbid()
        {
            var context = GetContext();
            var review = new Review
            {
                Id = 3,
                ProductId = 102,
                UserId = 4,
                Rating = 3,
                Comment = "So-so"
            };
            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            var controller = SetupController(context, userId: 5); // different user, not admin

            var model = new ReviewViewModel
            {
                Id = 3,
                ProductId = 102,
                Rating = 4,
                Comment = "Changed"
            };

            var result = await controller.Edit(model);

            Assert.IsType<ForbidResult>(result);

            var unchangedReview = await context.Reviews.FindAsync(3);
            Assert.Equal(3, unchangedReview.Rating);
            Assert.Equal("So-so", unchangedReview.Comment);
        }

        [Fact]
        public async Task Delete_OwnerCanDelete_RemovesReview()
        {
            var context = GetContext();
            var review = new Review
            {
                Id = 4,
                ProductId = 103,
                UserId = 6,
                Rating = 1,
                Comment = "Terrible"
            };
            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            var controller = SetupController(context, userId: 6);

            var result = await controller.Delete(4);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Product", redirect.ControllerName);
            Assert.Equal(103, redirect.RouteValues["id"]);

            var reviewInDb = await context.Reviews.FindAsync(4);
            Assert.Null(reviewInDb);
        }

        [Fact]
        public async Task Delete_AdminCanDelete_RemovesReview()
        {
            var context = GetContext();
            var review = new Review
            {
                Id = 5,
                ProductId = 104,
                UserId = 7,
                Rating = 3,
                Comment = "Meh"
            };
            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            var controller = SetupController(context, userId: 999, role: "Admin");

            var result = await controller.Delete(5);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Product", redirect.ControllerName);
            Assert.Equal(104, redirect.RouteValues["id"]);

            var reviewInDb = await context.Reviews.FindAsync(5);
            Assert.Null(reviewInDb);
        }

        [Fact]
        public async Task Delete_OtherUserCannotDelete_ReturnsForbid()
        {
            var context = GetContext();
            var review = new Review
            {
                Id = 6,
                ProductId = 105,
                UserId = 8,
                Rating = 2,
                Comment = "Not good"
            };
            context.Reviews.Add(review);
            await context.SaveChangesAsync();

            var controller = SetupController(context, userId: 9); // not owner, not admin

            var result = await controller.Delete(6);

            Assert.IsType<ForbidResult>(result);

            var reviewInDb = await context.Reviews.FindAsync(6);
            Assert.NotNull(reviewInDb);
        }
    }
}
