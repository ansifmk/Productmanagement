using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using ProductManagement.API.Extensions;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Interfaces;
using ProductManagement.Infrastructure.Repositories;
using ProductManagement.Infrastructure.Services;
using ProductManagement.Infrastructure.Data;
using System.Linq;

namespace ProductManagement.API.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddApplicationServices_RegistersExpectedServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplicationServices();

            // Assert
            Assert.Contains(services, d => d.ServiceType == typeof(IProductService) && d.ImplementationType == typeof(ProductService) && d.Lifetime == ServiceLifetime.Scoped);
            Assert.Contains(services, d => d.ServiceType == typeof(IAuthService) && d.ImplementationType == typeof(AuthService) && d.Lifetime == ServiceLifetime.Scoped);
            Assert.Contains(services, d => d.ServiceType == typeof(IJwtService) && d.ImplementationType == typeof(JwtService) && d.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructureServices_RegistersExpectedServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockConfiguration = new Mock<IConfiguration>();
            
            // Mock connection string
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(s => s.Value).Returns("Server=localhost;Database=ProductDb;Trusted_Connection=True;TrustServerCertificate=True;");
            mockConfiguration.Setup(c => c.GetSection("ConnectionStrings:DefaultConnection")).Returns(mockSection.Object);

            // Act
            services.AddInfrastructureServices(mockConfiguration.Object);

            // Assert
            Assert.Contains(services, d => d.ServiceType == typeof(IRepository<,>) && d.ImplementationType == typeof(Repository<,>) && d.Lifetime == ServiceLifetime.Scoped);
            Assert.Contains(services, d => d.ServiceType == typeof(IProductRepository) && d.ImplementationType == typeof(ProductRepository) && d.Lifetime == ServiceLifetime.Scoped);
            Assert.Contains(services, d => d.ServiceType == typeof(IUserRepository) && d.ImplementationType == typeof(UserRepository) && d.Lifetime == ServiceLifetime.Scoped);
            Assert.Contains(services, d => d.ServiceType == typeof(IItemRepository) && d.ImplementationType == typeof(ItemRepository) && d.Lifetime == ServiceLifetime.Scoped);
            Assert.Contains(services, d => d.ServiceType == typeof(IUnitOfWork) && d.ImplementationType == typeof(UnitOfWork) && d.Lifetime == ServiceLifetime.Scoped);
            Assert.Contains(services, d => d.ServiceType == typeof(ApplicationDbContext) && d.Lifetime == ServiceLifetime.Scoped);
        }
    }
}
