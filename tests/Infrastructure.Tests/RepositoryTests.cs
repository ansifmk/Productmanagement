using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Enums;
using ProductManagement.Infrastructure.Data;
using ProductManagement.Infrastructure.Repositories;
using Xunit;

namespace ProductManagement.Infrastructure.Tests
{
    public class RepositoryTests
    {
        private DbContextOptions<ApplicationDbContext> CreateNewContextOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task AnyAsync_WhenNoUsersExist_ReturnsFalse()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            var repository = new UserRepository(context);

            // Act
            var result = await repository.AnyAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AnyAsync_WhenUsersExist_ReturnsTrue()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            context.Users.Add(new User { Id = Guid.NewGuid(), Username = "test", Email = "test@example.com" });
            await context.SaveChangesAsync();
            var repository = new UserRepository(context);

            // Act
            var result = await repository.AnyAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ProductRepository_GetByIdAsync_EagerLoadsItems()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            var product = new Product
            {
                Id = 1,
                ProductName = "P1",
                CreatedBy = "Admin",
                Items = new List<Item> { new Item { Id = 10, Quantity = 5 } }
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var repository = new ProductRepository(context);

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(5, result.Items.First().Quantity);
        }

        [Fact]
        public async Task CascadeDelete_WhenProductDeleted_DeletesItems()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            var product = new Product
            {
                Id = 1,
                ProductName = "P1",
                CreatedBy = "Admin",
                Items = new List<Item> { new Item { Id = 10, Quantity = 5 } }
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var unitOfWork = new UnitOfWork(context, new ProductRepository(context), new UserRepository(context), new ItemRepository(context));

            // Act
            var pToDelete = await unitOfWork.Products.GetByIdAsync(1);
            Assert.NotNull(pToDelete);
            unitOfWork.Products.Remove(pToDelete);
            await unitOfWork.SaveChangesAsync();

            // Assert
            var remainingItems = await context.Items.ToListAsync();
            Assert.Empty(remainingItems);
        }

        [Fact]
        public async Task Repository_GetByIdAsync_WhenIdIsNull_ReturnsNull()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            var repository = new UserRepository(context);

            // Act & Assert
            // Guid? id = null; but the generic method signature takes TKey. Since Guid is a struct, we can test with Guid.Empty or we can mock/instantiate with a nullable key repository if needed. 
            // Wait, the generic signature says: "if (id == null) return null;". Since Guid is not nullable, let's use a class-key entity, or pass null to a class key ID repository.
            // Do we have any entity with a class key (e.g. string or object)?
            // Wait, Product has int key (struct). User has Guid key (struct). Item has int key (struct). RefreshToken has Guid key (struct).
            // But we can invoke GetByIdAsync(null) by writing:
            // var repository = new Repository<User, string>(context); // Repository can take any key type!
            var repoWithStringKey = new Repository<User, string>(context);
            var result = await repoWithStringKey.GetByIdAsync(null!);
            Assert.Null(result);
        }

        [Fact]
        public async Task Repository_GetByIdAsync_WithNonNullKey_ReturnsEntity()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            var user = new User { Id = Guid.NewGuid(), Username = "user", Email = "u@e.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repository = new Repository<User, Guid>(context);

            // Act
            var result = await repository.GetByIdAsync(user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Username, result.Username);
        }

        [Fact]
        public async Task Repository_GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            context.Users.Add(new User { Id = Guid.NewGuid(), Username = "u1", Email = "u1@e.com" });
            context.Users.Add(new User { Id = Guid.NewGuid(), Username = "u2", Email = "u2@e.com" });
            await context.SaveChangesAsync();
            var repository = new UserRepository(context);

            // Act
            var results = await repository.GetAllAsync();

            // Assert
            Assert.Equal(2, results.Count());
        }

        [Fact]
        public async Task Repository_FindAsync_ReturnsMatchingEntities()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            context.Users.Add(new User { Id = Guid.NewGuid(), Username = "match1", Email = "m1@e.com" });
            context.Users.Add(new User { Id = Guid.NewGuid(), Username = "nomatch", Email = "m2@e.com" });
            await context.SaveChangesAsync();
            var repository = new UserRepository(context);

            // Act
            var results = await repository.FindAsync(u => u.Username.StartsWith("match"));

            // Assert
            var list = results.ToList();
            Assert.Single(list);
            Assert.Equal("match1", list[0].Username);
        }

        [Fact]
        public async Task Repository_AddAsync_AddsEntityToContext()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            var repository = new UserRepository(context);
            var user = new User { Id = Guid.NewGuid(), Username = "newuser", Email = "new@e.com" };

            // Act
            await repository.AddAsync(user);
            await context.SaveChangesAsync();

            // Assert
            var addedUser = await context.Users.FindAsync(user.Id);
            Assert.NotNull(addedUser);
            Assert.Equal("newuser", addedUser.Username);
        }

        [Fact]
        public async Task Repository_Update_UpdatesEntityState()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            var user = new User { Id = Guid.NewGuid(), Username = "user", Email = "user@e.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Detach user to avoid tracking issues during manual update
            context.Entry(user).State = EntityState.Detached;

            var repository = new UserRepository(context);

            // Act
            user.Username = "updated";
            repository.Update(user);
            await context.SaveChangesAsync();

            // Assert
            var updatedUser = await context.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.Equal("updated", updatedUser.Username);
        }

        [Fact]
        public async Task UserRepository_GetByEmailAsync_ReturnsCorrectUser()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            var user = new User { Id = Guid.NewGuid(), Username = "user", Email = "findme@e.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByEmailAsync("findme@e.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task UserRepository_GetByUsernameAsync_ReturnsCorrectUser()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            var user = new User { Id = Guid.NewGuid(), Username = "username_to_find", Email = "u@e.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByUsernameAsync("username_to_find");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task UserRepository_GetByRefreshTokenAsync_ReturnsUserWithRefreshTokens()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            var user = new User { Id = Guid.NewGuid(), Username = "user", Email = "u@e.com" };
            var refreshToken = new RefreshToken { Id = Guid.NewGuid(), Token = "token123", User = user };
            user.RefreshTokens.Add(refreshToken);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetByRefreshTokenAsync("token123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Single(result.RefreshTokens);
            Assert.Equal("token123", result.RefreshTokens.First().Token);
        }

        [Fact]
        public async Task ProductRepository_GetPagedProductsAsync_ReturnsPagedResults()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            
            // Add products created at different times to check ordering
            var now = DateTime.UtcNow;
            var p1 = new Product { Id = 1, ProductName = "P1", CreatedBy = "Admin", CreatedOn = now.AddMinutes(-5) };
            var p2 = new Product { Id = 2, ProductName = "P2", CreatedBy = "Admin", CreatedOn = now };
            var p3 = new Product { Id = 3, ProductName = "P3", CreatedBy = "Admin", CreatedOn = now.AddMinutes(-10) };
            context.Products.AddRange(p1, p2, p3);
            await context.SaveChangesAsync();

            var repository = new ProductRepository(context);

            // Act: request page 1, size 2 (should return the 2 newest: p2 and p1, in descending order of CreatedOn)
            var pagedResults = (await repository.GetPagedProductsAsync(1, 2)).ToList();

            // Assert
            Assert.Equal(2, pagedResults.Count);
            Assert.Equal(2, pagedResults[0].Id); // Newest
            Assert.Equal(1, pagedResults[1].Id); // Second newest
        }

        [Fact]
        public async Task ProductRepository_CountAsync_ReturnsTotalCount()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new ApplicationDbContext(options);
            context.Products.Add(new Product { Id = 1, ProductName = "P1", CreatedBy = "Admin" });
            context.Products.Add(new Product { Id = 2, ProductName = "P2", CreatedBy = "Admin" });
            await context.SaveChangesAsync();
            var repository = new ProductRepository(context);

            // Act
            var count = await repository.CountAsync();

            // Assert
            Assert.Equal(2, count);
        }
    }
}
