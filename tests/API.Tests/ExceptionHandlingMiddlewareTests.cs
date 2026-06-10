using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using ProductManagement.API.Middleware;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.Responses;
using Xunit;

namespace ProductManagement.API.Tests
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;
        private readonly Mock<IHostEnvironment> _mockHostEnvironment;

        public ExceptionHandlingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            _mockHostEnvironment = new Mock<IHostEnvironment>();
        }

        [Fact]
        public async Task InvokeAsync_WhenNoException_CallsNext()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockHostEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_WhenNotFoundExceptionThrown_Returns404AndJsonPayload()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            RequestDelegate next = (ctx) => throw new NotFoundException("Resource not found");

            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockHostEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody);

            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Equal("Resource not found", apiResponse.Message);
        }

        [Fact]
        public async Task InvokeAsync_WhenUnhandledExceptionThrown_InDevelopment_Returns500AndExposesDetails()
        {
            // Arrange
            _mockHostEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            RequestDelegate next = (ctx) => throw new Exception("Fatal DB crash");

            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockHostEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody);

            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Equal("Fatal DB crash", apiResponse.Message);
            Assert.NotEmpty(apiResponse.Errors);
        }

        [Fact]
        public async Task InvokeAsync_WhenUnhandledExceptionThrown_InProduction_Returns500AndHidesDetails()
        {
            // Arrange
            _mockHostEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            RequestDelegate next = (ctx) => throw new Exception("Fatal DB crash");

            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockHostEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody);

            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Equal("An unexpected error occurred.", apiResponse.Message);
            Assert.Empty(apiResponse.Errors);
        }

        [Fact]
        public async Task InvokeAsync_WhenAuthenticationExceptionThrown_Returns401AndJsonPayload()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            RequestDelegate next = (ctx) => throw new AuthenticationException("Invalid credentials");

            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockHostEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody);

            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Equal("Invalid credentials", apiResponse.Message);
        }

        [Fact]
        public async Task InvokeAsync_WhenBadHttpRequestExceptionThrown_Returns400AndJsonPayload()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            RequestDelegate next = (ctx) => throw new BadHttpRequestException("Malformed JSON request");

            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockHostEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody);

            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Equal("Malformed JSON request", apiResponse.Message);
        }

        [Fact]
        public async Task InvokeAsync_WhenValidationExceptionThrown_Returns400AndErrors()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var failures = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("ProductName", "Product name is required"),
                new FluentValidation.Results.ValidationFailure("Price", "Price must be positive")
            };
            RequestDelegate next = (ctx) => throw new FluentValidation.ValidationException(failures);

            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockHostEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody);

            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.Equal("Validation failed.", apiResponse.Message);
            Assert.Equal(2, System.Linq.Enumerable.Count(apiResponse.Errors));
            Assert.Contains("Product name is required", apiResponse.Errors);
            Assert.Contains("Price must be positive", apiResponse.Errors);
        }
    }
}
