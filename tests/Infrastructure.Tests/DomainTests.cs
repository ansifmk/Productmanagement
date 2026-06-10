using System;
using System.Collections.Generic;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using Xunit;

namespace ProductManagement.Infrastructure.Tests
{
    public class DomainTests
    {
        [Fact]
        public void RefreshToken_IsExpired_ReturnsCorrectValues()
        {
            // Arrange
            var activeToken = new RefreshToken { Expires = DateTime.UtcNow.AddMinutes(5) };
            var expiredToken = new RefreshToken { Expires = DateTime.UtcNow.AddMinutes(-5) };

            // Assert
            Assert.False(activeToken.IsExpired);
            Assert.True(expiredToken.IsExpired);
        }

        [Fact]
        public void RefreshToken_IsActive_ReturnsCorrectValues()
        {
            // Arrange & Act & Assert
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var user = new User { Id = userId };
            var createdAt = DateTime.UtcNow;

            var activeToken = new RefreshToken 
            { 
                Id = id,
                Token = "sometoken",
                CreatedAt = createdAt,
                UserId = userId,
                User = user,
                Expires = DateTime.UtcNow.AddMinutes(5), 
                RevokedAt = null,
                RevokedByIp = "127.0.0.1",
                ReplacedByToken = "newToken"
            };
            Assert.True(activeToken.IsActive);
            Assert.Equal(id, activeToken.Id);
            Assert.Equal("sometoken", activeToken.Token);
            Assert.Equal(createdAt, activeToken.CreatedAt);
            Assert.Equal(userId, activeToken.UserId);
            Assert.Equal(user, activeToken.User);
            Assert.Equal("127.0.0.1", activeToken.RevokedByIp);
            Assert.Equal("newToken", activeToken.ReplacedByToken);

            var revokedToken = new RefreshToken { Expires = DateTime.UtcNow.AddMinutes(5), RevokedAt = DateTime.UtcNow };
            Assert.False(revokedToken.IsActive);

            var expiredToken = new RefreshToken { Expires = DateTime.UtcNow.AddMinutes(-5), RevokedAt = null };
            Assert.False(expiredToken.IsActive);
        }

        [Fact]
        public void Product_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var product = new Product
            {
                Id = 1,
                ProductName = "Laptop",
                CreatedBy = "Admin",
                CreatedOn = DateTime.UtcNow,
                ModifiedBy = "User",
                ModifiedOn = DateTime.UtcNow,
                Items = new List<Item>()
            };

            // Assert
            Assert.Equal(1, product.Id);
            Assert.Equal("Laptop", product.ProductName);
            Assert.Equal("Admin", product.CreatedBy);
            Assert.NotEqual(default(DateTime), product.CreatedOn);
            Assert.Equal("User", product.ModifiedBy);
            Assert.NotNull(product.ModifiedOn);
            Assert.Empty(product.Items);
        }

        [Fact]
        public void Item_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var product = new Product { Id = 1 };
            var item = new Item
            {
                Id = 10,
                ProductId = 1,
                Quantity = 5,
                Product = product
            };

            // Assert
            Assert.Equal(10, item.Id);
            Assert.Equal(1, item.ProductId);
            Assert.Equal(5, item.Quantity);
            Assert.Equal(product, item.Product);
        }

        [Fact]
        public void User_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var tokens = new List<RefreshToken>();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashed_pass",
                Role = Role.Admin,
                CreatedAt = DateTime.UtcNow,
                RefreshTokens = tokens
            };

            // Assert
            Assert.NotEqual(Guid.Empty, user.Id);
            Assert.Equal("testuser", user.Username);
            Assert.Equal("test@example.com", user.Email);
            Assert.Equal("hashed_pass", user.PasswordHash);
            Assert.Equal(Role.Admin, user.Role);
            Assert.NotEqual(default(DateTime), user.CreatedAt);
            Assert.Equal(tokens, user.RefreshTokens);
        }
    }
}
