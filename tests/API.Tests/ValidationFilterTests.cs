using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using FluentValidation;
using FluentValidation.Results;
using ProductManagement.API.Filters;
using Xunit;

namespace ProductManagement.API.Tests
{
    public class ValidationFilterTests
    {
        private readonly ValidationFilter _filter;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly DefaultHttpContext _httpContext;

        public ValidationFilterTests()
        {
            _filter = new ValidationFilter();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _httpContext = new DefaultHttpContext();
            _httpContext.RequestServices = _mockServiceProvider.Object;
        }

        private ActionExecutingContext CreateContext(Dictionary<string, object?> actionArguments)
        {
            var actionContext = new ActionContext(
                _httpContext,
                new RouteData(),
                new ActionDescriptor()
            );

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments,
                new Mock<Controller>().Object
            );
        }

        public class TestModel { public string Name { get; set; } = ""; }

        [Fact]
        public async Task OnActionExecutionAsync_WhenArgumentIsNull_SkipsAndCallsNext()
        {
            // Arrange
            var actionArguments = new Dictionary<string, object?> { { "model", null } };
            var context = CreateContext(actionArguments);
            var nextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new Mock<Controller>().Object));
            };

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WhenNoValidatorExists_CallsNext()
        {
            // Arrange
            var model = new TestModel { Name = "Test" };
            var actionArguments = new Dictionary<string, object?> { { "model", model } };
            var context = CreateContext(actionArguments);
            
            // Service provider returns null for IValidator<TestModel>
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidator<TestModel>))).Returns((object?)null);

            var nextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new Mock<Controller>().Object));
            };

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WhenValidatorSucceeds_CallsNext()
        {
            // Arrange
            var model = new TestModel { Name = "Valid" };
            var actionArguments = new Dictionary<string, object?> { { "model", model } };
            var context = CreateContext(actionArguments);

            var mockValidator = new Mock<IValidator<TestModel>>();
            mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<object>>(), default))
                .ReturnsAsync(new ValidationResult()); // Default is valid (no errors)

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidator<TestModel>))).Returns(mockValidator.Object);

            var nextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new Mock<Controller>().Object));
            };

            // Act
            await _filter.OnActionExecutionAsync(context, next);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WhenValidatorFails_ThrowsValidationException()
        {
            // Arrange
            var model = new TestModel { Name = "" };
            var actionArguments = new Dictionary<string, object?> { { "model", model } };
            var context = CreateContext(actionArguments);

            var mockValidator = new Mock<IValidator<TestModel>>();
            var failures = new List<ValidationFailure> { new ValidationFailure("Name", "Name cannot be empty") };
            mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<object>>(), default))
                .ReturnsAsync(new ValidationResult(failures));

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IValidator<TestModel>))).Returns(mockValidator.Object);

            ActionExecutionDelegate next = () => Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new Mock<Controller>().Object));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _filter.OnActionExecutionAsync(context, next));
            Assert.Single(exception.Errors);
            Assert.Equal("Name cannot be empty", System.Linq.Enumerable.First(exception.Errors).ErrorMessage);
        }
    }
}
