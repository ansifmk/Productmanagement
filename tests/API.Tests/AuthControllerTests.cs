using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductManagement.API.Controllers;
using ProductManagement.Application.DTOs.Auth;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Responses;
using Xunit;

namespace ProductManagement.API.Tests
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _controller;
        private readonly DefaultHttpContext _httpContext;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _controller = new AuthController(_mockAuthService.Object);
            
            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public async Task Register_ValidRequest_ReturnsOkAndSetsCookies()
        {
            // Arrange
            var request = new RegisterRequest("user", "test@test.com", "Password123!");
            var authResponse = new AuthResponse(Guid.NewGuid(), "user", "test@test.com", "User", "accessToken", "refreshToken", DateTime.UtcNow.AddDays(7));
            _mockAuthService.Setup(s => s.RegisterAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _controller.Register(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResponse>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(authResponse, apiResponse.Data);
            
            // Verify cookies are set
            var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("accessToken=accessToken", cookies);
            Assert.Contains("refreshToken=refreshToken", cookies);
        }

        [Fact]
        public async Task Login_ValidRequest_ReturnsOkAndSetsCookies()
        {
            // Arrange
            var request = new LoginRequest("test@test.com", "Password123!");
            var authResponse = new AuthResponse(Guid.NewGuid(), "user", "test@test.com", "User", "accessToken", "refreshToken", DateTime.UtcNow.AddDays(7));
            _mockAuthService.Setup(s => s.LoginAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _controller.Login(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResponse>>(okResult.Value);
            Assert.True(apiResponse.Success);
            
            var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("accessToken=accessToken", cookies);
            Assert.Contains("refreshToken=refreshToken", cookies);
        }

        [Fact]
        public async Task RefreshToken_WithCookieToken_ReturnsOkAndRotatesCookies()
        {
            // Arrange
            _httpContext.Request.Headers["Cookie"] = "refreshToken=oldRefreshToken";
            var authResponse = new AuthResponse(Guid.NewGuid(), "user", "test@test.com", "User", "newAccessToken", "newRefreshToken", DateTime.UtcNow.AddDays(7));
            _mockAuthService.Setup(s => s.RefreshTokenAsync("oldRefreshToken", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _controller.RefreshToken(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResponse>>(okResult.Value);
            Assert.True(apiResponse.Success);
            
            var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("accessToken=newAccessToken", cookies);
            Assert.Contains("refreshToken=newRefreshToken", cookies);
        }

        [Fact]
        public async Task RefreshToken_WithoutCookieToken_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.RefreshToken(CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal("Refresh token is required.", apiResponse.Message);
        }

        [Fact]
        public async Task Logout_ValidToken_ClearsCookiesAndReturnsOk()
        {
            // Arrange
            _httpContext.Request.Headers["Cookie"] = "refreshToken=someToken";
            _mockAuthService.Setup(s => s.LogoutAsync("someToken", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(apiResponse.Success);

            // Verify cookies are expired/deleted
            var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("accessToken=; expires=", cookies);
            Assert.Contains("refreshToken=; expires=", cookies);
        }

        [Fact]
        public async Task Register_WhenXForwardedForHeaderPresent_UsesHeaderIp()
        {
            // Arrange
            var request = new RegisterRequest("user", "test@test.com", "Password123!");
            var authResponse = new AuthResponse(Guid.NewGuid(), "user", "test@test.com", "User", "accessToken", "refreshToken", DateTime.UtcNow.AddDays(7));
            _httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.195";

            _mockAuthService.Setup(s => s.RegisterAsync(request, "203.0.113.195", It.IsAny<CancellationToken>()))
                .ReturnsAsync(authResponse);

            // Act
            await _controller.Register(request, CancellationToken.None);

            // Assert
            _mockAuthService.Verify(s => s.RegisterAsync(request, "203.0.113.195", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Register_WhenRemoteIpAddressPresent_UsesRemoteIp()
        {
            // Arrange
            var request = new RegisterRequest("user", "test@test.com", "Password123!");
            var authResponse = new AuthResponse(Guid.NewGuid(), "user", "test@test.com", "User", "accessToken", "refreshToken", DateTime.UtcNow.AddDays(7));
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.0.2.1");

            _mockAuthService.Setup(s => s.RegisterAsync(request, "192.0.2.1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(authResponse);

            // Act
            await _controller.Register(request, CancellationToken.None);

            // Assert
            _mockAuthService.Verify(s => s.RegisterAsync(request, "192.0.2.1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Register_WhenEnvironmentIsProduction_SetsSecureCookies()
        {
            // Arrange
            var request = new RegisterRequest("user", "test@test.com", "Password123!");
            var authResponse = new AuthResponse(Guid.NewGuid(), "user", "test@test.com", "User", "accessToken", "refreshToken", DateTime.UtcNow.AddDays(7));
            
            // Temporarily set ASPNETCORE_ENVIRONMENT to Production
            var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            try
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
                _mockAuthService.Setup(s => s.RegisterAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(authResponse);

                // Act
                await _controller.Register(request, CancellationToken.None);

                // Assert
                var cookies = _httpContext.Response.Headers["Set-Cookie"].ToString();
                Assert.Contains("secure", cookies);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
            }
        }
    }
}
