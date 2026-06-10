using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Application.DTOs.Auth;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Responses;

namespace ProductManagement.API.Controllers
{
    [Asp.Versioning.ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.RegisterAsync(request, ipAddress, cancellationToken);
            SetTokenCookies(response);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "User registered successfully"));
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.LoginAsync(request, ipAddress, cancellationToken);
            SetTokenCookies(response);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "Login successful"));
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(ApiResponse<object>.Failure("Refresh token is required."));
            }

            var ipAddress = GetIpAddress();
            var response = await _authService.RefreshTokenAsync(refreshToken, ipAddress, cancellationToken);
            SetTokenCookies(response);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "Token refreshed successfully"));
        }

        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.LogoutAsync(refreshToken, cancellationToken);
            }

            ClearTokenCookies();
            return Ok(ApiResponse<object>.SuccessResponse(null!, "Logged out successfully"));
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return Request.Headers["X-Forwarded-For"]!;
            }
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        }

        private void SetTokenCookies(AuthResponse response)
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDevelopment, // Use secure cookies in production, optional for dev
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            };
            Response.Cookies.Append("accessToken", response.AccessToken, cookieOptions);

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDevelopment,
                SameSite = SameSiteMode.Strict,
                Expires = response.ExpiresAt
            };
            Response.Cookies.Append("refreshToken", response.RefreshToken, refreshCookieOptions);
        }

        private void ClearTokenCookies()
        {
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
        }
    }
}
