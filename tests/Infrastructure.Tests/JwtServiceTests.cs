using System;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using ProductManagement.Infrastructure.Services;
using Xunit;

namespace ProductManagement.Infrastructure.Tests
{
    public class JwtServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly JwtService _jwtService;
        private const string Secret = "THIS_IS_A_VERY_STRONG_SECRET_KEY_FOR_TESTING_PURPOSES_ONLY_123456";

        public JwtServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["JwtSettings:Secret"]).Returns(Secret);
            _mockConfiguration.Setup(c => c["JwtSettings:AccessTokenExpirationMinutes"]).Returns("15");
            _mockConfiguration.Setup(c => c["JwtSettings:RefreshTokenExpirationDays"]).Returns("7");

            _jwtService = new JwtService(_mockConfiguration.Object);
        }

        [Fact]
        public void GenerateAccessToken_ValidUser_GeneratesTokenWithCorrectClaims()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                Role = Role.User
            };

            // Act
            var token = _jwtService.GenerateAccessToken(user);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var principal = _jwtService.GetPrincipalFromExpiredToken(token);
            Assert.NotNull(principal);
            Assert.Equal(user.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal(user.Email, principal.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Equal(user.Username, principal.FindFirst(ClaimTypes.Name)?.Value);
            Assert.Equal(user.Role.ToString(), principal.FindFirst(ClaimTypes.Role)?.Value);
        }

        [Fact]
        public void GenerateRefreshToken_ValidIpAddress_SetsPropertiesAndExpiration()
        {
            // Arrange
            var ipAddress = "127.0.0.1";

            // Act
            var token = _jwtService.GenerateRefreshToken(ipAddress);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token.Token);
            Assert.Equal(ipAddress, token.CreatedByIp);
            Assert.True(token.Expires > DateTime.UtcNow);
            Assert.True(token.Expires <= DateTime.UtcNow.AddDays(7).AddMinutes(1)); // Allow minor clock skew
            Assert.True(token.IsActive);
            Assert.False(token.IsExpired);
        }

        [Fact]
        public void GenerateAccessToken_MissingSecret_ThrowsInvalidOperationException()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["JwtSettings:Secret"]).Returns((string?)null);
            var service = new JwtService(config.Object);
            var user = new User { Id = Guid.NewGuid(), Email = "a@a.com", Username = "user" };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => service.GenerateAccessToken(user));
            Assert.Equal("JWT Secret is not configured.", ex.Message);
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_MissingSecret_ThrowsInvalidOperationException()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["JwtSettings:Secret"]).Returns((string?)null);
            var service = new JwtService(config.Object);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => service.GetPrincipalFromExpiredToken("dummyToken"));
            Assert.Equal("JWT Secret is not configured.", ex.Message);
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_InvalidToken_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsAny<Exception>(() => _jwtService.GetPrincipalFromExpiredToken("invalid_jwt_token_format"));
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_InvalidAlgorithm_ThrowsSecurityTokenException()
        {
            // Arrange: Generate token signed with different algorithm or configuration (e.g. HmacSha512)
            // But we can test it simply by generating a valid token structure manually but with a different algorithm, or mocking security token
            // Wait, an easier way is to create a token signed with another signature algorithm (like HS512) or check validation behavior.
            // Let's generate a token with a symmetric key but using HS512 signature algorithm.
            var mySecret = "THIS_IS_A_VERY_STRONG_SECRET_KEY_FOR_TESTING_PURPOSES_ONLY_123456";
            var key = System.Text.Encoding.ASCII.GetBytes(mySecret);
            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("sub", "123") }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha512Signature) // Different alg
            };
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtTokenString = tokenHandler.WriteToken(token);

            // Act & Assert
            var ex = Assert.Throws<Microsoft.IdentityModel.Tokens.SecurityTokenException>(() => _jwtService.GetPrincipalFromExpiredToken(jwtTokenString));
            Assert.Equal("Invalid token", ex.Message);
        }
    }
}
