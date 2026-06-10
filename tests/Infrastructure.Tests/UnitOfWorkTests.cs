using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProductManagement.Domain.Interfaces;
using ProductManagement.Infrastructure.Data;
using ProductManagement.Infrastructure.Repositories;
using Xunit;

namespace ProductManagement.Infrastructure.Tests
{
    public class UnitOfWorkTests
    {
        private DbContextOptions<ApplicationDbContext> CreateNewContextOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task UnitOfWork_SavesChangesAndDisposes()
        {
            // Arrange
            var options = CreateNewContextOptions();
            var context = new ApplicationDbContext(options);
            var mockProductRepo = new Mock<IProductRepository>();
            var mockUserRepo = new Mock<IUserRepository>();
            var mockItemRepo = new Mock<IItemRepository>();

            var uow = new UnitOfWork(context, mockProductRepo.Object, mockUserRepo.Object, mockItemRepo.Object);

            // Act & Assert
            Assert.Equal(mockProductRepo.Object, uow.Products);
            Assert.Equal(mockUserRepo.Object, uow.Users);
            Assert.Equal(mockItemRepo.Object, uow.Items);

            var saveResult = await uow.SaveChangesAsync(CancellationToken.None);
            Assert.Equal(0, saveResult);

            uow.Dispose();

            // Verify context is disposed by expecting exception on access
            Assert.Throws<ObjectDisposedException>(() => context.Users.Add(new Domain.Entities.User()));
        }
    }
}
