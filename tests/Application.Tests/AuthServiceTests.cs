using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.DTOs.Auth;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using ProductManagement.Domain.Interfaces;
using Xunit;

namespace ProductManagement.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockJwtService = new Mock<IJwtService>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _authService = new AuthService(_mockUnitOfWork.Object, _mockJwtService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task RegisterAsync_ValidRequest_RegistersUserAndReturnsAuthResponse()
        {
            // Arrange
            var request = new RegisterRequest("testuser", "test@example.com", "Password123!");
            var ipAddress = "127.0.0.1";
            var refreshToken = new RefreshToken { Token = "RefreshToken123", Expires = DateTime.UtcNow.AddDays(7) };

            _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockUnitOfWork.Setup(u => u.Users.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockUnitOfWork.Setup(u => u.Users.AnyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // First user is Admin
            _mockJwtService.Setup(j => j.GenerateRefreshToken(ipAddress))
                .Returns(refreshToken);
            _mockUnitOfWork.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
                .Returns("AccessToken123");

            // Act
            var result = await _authService.RegisterAsync(request, ipAddress);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal(Role.Admin.ToString(), result.Role); // First user registers as Admin
            Assert.Equal("AccessToken123", result.AccessToken);
            Assert.Equal("RefreshToken123", result.RefreshToken);
            _mockUnitOfWork.Verify(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ThrowsAuthenticationException()
        {
            // Arrange
            var request = new RegisterRequest("testuser", "duplicate@example.com", "Password123!");
            var existingUser = new User { Email = "duplicate@example.com" };

            _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act & Assert
            await Assert.ThrowsAsync<AuthenticationException>(() => _authService.RegisterAsync(request, "127.0.0.1"));
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var request = new LoginRequest("test@example.com", "Password123!");
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = passwordHash,
                Role = Role.User
            };
            var ipAddress = "127.0.0.1";
            var refreshToken = new RefreshToken { Token = "RefreshToken123", Expires = DateTime.UtcNow.AddDays(7) };

            _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockJwtService.Setup(j => j.GenerateRefreshToken(ipAddress))
                .Returns(refreshToken);
            _mockUnitOfWork.Setup(u => u.Users.Update(user));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mockJwtService.Setup(j => j.GenerateAccessToken(user))
                .Returns("AccessToken123");

            // Act
            var result = await _authService.LoginAsync(request, ipAddress);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("AccessToken123", result.AccessToken);
            Assert.Equal("RefreshToken123", result.RefreshToken);
        }

        [Fact]
        public async Task LoginAsync_CleansUpOldInactiveTokens()
        {
            // Arrange
            var request = new LoginRequest("test@example.com", "Password123!");
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");
            
            var oldInactiveToken = new RefreshToken { Token = "OldInactive", Expires = DateTime.UtcNow.AddDays(-10), RevokedAt = DateTime.UtcNow.AddDays(-11) }; // expires <= -7 days, inactive
            var recentInactiveToken = new RefreshToken { Token = "RecentInactive", Expires = DateTime.UtcNow.AddDays(-1), RevokedAt = DateTime.UtcNow.AddDays(-2) }; // expires > -7 days, inactive
            var activeToken = new RefreshToken { Token = "ActiveToken", Expires = DateTime.UtcNow.AddDays(1), RevokedAt = null }; // active
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = passwordHash,
                Role = Role.User,
                RefreshTokens = new List<RefreshToken> { oldInactiveToken, recentInactiveToken, activeToken }
            };
            var ipAddress = "127.0.0.1";
            var newRefreshToken = new RefreshToken { Token = "NewRefreshToken", Expires = DateTime.UtcNow.AddDays(7) };

            _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockJwtService.Setup(j => j.GenerateRefreshToken(ipAddress))
                .Returns(newRefreshToken);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mockJwtService.Setup(j => j.GenerateAccessToken(user))
                .Returns("AccessToken123");

            // Act
            await _authService.LoginAsync(request, ipAddress);

            // Assert
            Assert.DoesNotContain(oldInactiveToken, user.RefreshTokens); // Should be removed
            Assert.Contains(recentInactiveToken, user.RefreshTokens); // Should NOT be removed
            Assert.Contains(activeToken, user.RefreshTokens); // Should NOT be removed
            Assert.Contains(newRefreshToken, user.RefreshTokens); // Added new token
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsAuthenticationException()
        {
            // Arrange
            var request = new LoginRequest("test@example.com", "WrongPassword");
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");
            var user = new User { Email = "test@example.com", PasswordHash = passwordHash };

            _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<AuthenticationException>(() => _authService.LoginAsync(request, "127.0.0.1"));
        }

        [Fact]
        public async Task RefreshTokenAsync_ReusedRevokedToken_RevokesAllSessionsAndThrowsAuthenticationException()
        {
            // Arrange
            var tokenStr = "ReusedToken";
            var ipAddress = "127.0.0.1";
            var activeToken = new RefreshToken { Token = "ActiveToken", Expires = DateTime.UtcNow.AddDays(1), RevokedAt = null };
            var revokedToken = new RefreshToken { Token = tokenStr, Expires = DateTime.UtcNow.AddDays(-1), RevokedAt = DateTime.UtcNow.AddDays(-2) }; // Inactive
            
            var user = new User
            {
                RefreshTokens = new List<RefreshToken> { activeToken, revokedToken }
            };

            _mockUnitOfWork.Setup(u => u.Users.GetByRefreshTokenAsync(tokenStr, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.Users.Update(user));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act & Assert
            await Assert.ThrowsAsync<AuthenticationException>(() => _authService.RefreshTokenAsync(tokenStr, ipAddress));
            Assert.NotNull(activeToken.RevokedAt); // Reusing a revoked token should revoke all active tokens
            _mockUnitOfWork.Verify(u => u.Users.Update(user), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateUsername_ThrowsAuthenticationException()
        {
            // Arrange
            var request = new RegisterRequest("testuser", "test@example.com", "Password123!");
            _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockUnitOfWork.Setup(u => u.Users.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Username = "testuser" });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AuthenticationException>(() => _authService.RegisterAsync(request, "127.0.0.1"));
            Assert.Equal("Username is already taken.", ex.Message);
        }

        [Fact]
        public async Task RegisterAsync_SubsequentUser_RegistersAsUser()
        {
            // Arrange
            var request = new RegisterRequest("subsequent", "sub@example.com", "Password123!");
            var ipAddress = "127.0.0.1";
            var refreshToken = new RefreshToken { Token = "RefreshToken123", Expires = DateTime.UtcNow.AddDays(7) };

            _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockUnitOfWork.Setup(u => u.Users.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockUnitOfWork.Setup(u => u.Users.AnyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // NOT first user
            _mockJwtService.Setup(j => j.GenerateRefreshToken(ipAddress))
                .Returns(refreshToken);
            _mockUnitOfWork.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
                .Returns("AccessToken123");

            // Act
            var result = await _authService.RegisterAsync(request, ipAddress);

            // Assert
            Assert.Equal(Role.User.ToString(), result.Role); // Subsequent users get User role
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsAuthenticationException()
        {
            // Arrange
            var request = new LoginRequest("nonexistent@example.com", "Password123!");
            _mockUnitOfWork.Setup(u => u.Users.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AuthenticationException>(() => _authService.LoginAsync(request, "127.0.0.1"));
            Assert.Equal("Invalid email or password.", ex.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_TokenNotFound_ThrowsAuthenticationException()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Users.GetByRefreshTokenAsync("UnknownToken", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AuthenticationException>(() => _authService.RefreshTokenAsync("UnknownToken", "127.0.0.1"));
            Assert.Equal("Invalid refresh token.", ex.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_TokenExpired_ThrowsAuthenticationException()
        {
            // Arrange
            var tokenStr = "ExpiredToken";
            var expiredToken = new RefreshToken { Token = tokenStr, Expires = DateTime.UtcNow.AddDays(-1), RevokedAt = null };
            var user = new User { RefreshTokens = new List<RefreshToken> { expiredToken } };

            _mockUnitOfWork.Setup(u => u.Users.GetByRefreshTokenAsync(tokenStr, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AuthenticationException>(() => _authService.RefreshTokenAsync(tokenStr, "127.0.0.1"));
            Assert.Equal("Compromised refresh token reused. All active sessions revoked.", ex.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_ActiveToken_RotatesTokenAndReturnsAuthResponse()
        {
            // Arrange
            var oldTokenStr = "OldToken";
            var ipAddress = "127.0.0.1";
            var oldToken = new RefreshToken { Token = oldTokenStr, Expires = DateTime.UtcNow.AddDays(1), RevokedAt = null };
            var user = new User { Email = "test@example.com", RefreshTokens = new List<RefreshToken> { oldToken } };
            
            var newToken = new RefreshToken { Token = "NewToken", Expires = DateTime.UtcNow.AddDays(7) };

            _mockUnitOfWork.Setup(u => u.Users.GetByRefreshTokenAsync(oldTokenStr, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockJwtService.Setup(j => j.GenerateRefreshToken(ipAddress))
                .Returns(newToken);
            _mockJwtService.Setup(j => j.GenerateAccessToken(user))
                .Returns("NewAccessToken");
            _mockUnitOfWork.Setup(u => u.Users.Update(user));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _authService.RefreshTokenAsync(oldTokenStr, ipAddress);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NewAccessToken", result.AccessToken);
            Assert.Equal("NewToken", result.RefreshToken);
            Assert.NotNull(oldToken.RevokedAt); // Rotated
            Assert.Equal(ipAddress, oldToken.RevokedByIp);
            Assert.Equal("NewToken", oldToken.ReplacedByToken);
            Assert.Contains(newToken, user.RefreshTokens);
        }

        [Fact]
        public async Task LogoutAsync_TokenNotFound_ThrowsAuthenticationException()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Users.GetByRefreshTokenAsync("UnknownToken", It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AuthenticationException>(() => _authService.LogoutAsync("UnknownToken", CancellationToken.None));
            Assert.Equal("Invalid token.", ex.Message);

            // Verify no DB saves occur
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_TokenFound_RevokesToken()
        {
            // Arrange
            var tokenStr = "ActiveToken";
            var activeToken = new RefreshToken { Token = tokenStr, Expires = DateTime.UtcNow.AddDays(1), RevokedAt = null };
            var user = new User { RefreshTokens = new List<RefreshToken> { activeToken } };

            _mockUnitOfWork.Setup(u => u.Users.GetByRefreshTokenAsync(tokenStr, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _authService.LogoutAsync(tokenStr, CancellationToken.None);

            // Assert
            Assert.NotNull(activeToken.RevokedAt);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
