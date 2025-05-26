using System.Diagnostics;
using charolis.Controllers;
using charolis.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace charolis.Tests.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _loggerMock;

        public HomeControllerTests()
        {
            _loggerMock = new Mock<ILogger<HomeController>>();
        }

        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Arrange
            var controller = new HomeController(_loggerMock.Object);

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model); // Index view does not pass a model
        }

        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            // Arrange
            var controller = new HomeController(_loggerMock.Object);

            // Act
            var result = controller.Privacy();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model); // Privacy view does not pass a model
        }

        [Fact]
        public void Error_ReturnsViewResultWithErrorViewModel()
        {
            // Arrange
            var controller = new HomeController(_loggerMock.Object);

            // Необхідно симулювати HttpContext
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<ErrorViewModel>(viewResult.Model);

            Assert.False(string.IsNullOrEmpty(model.RequestId));
        }
    }
}