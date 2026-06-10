using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.DTOs.Product;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Interfaces;
using Xunit;

namespace ProductManagement.Application.Tests
{
    public class ProductServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<ProductService>> _mockLogger;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<ProductService>>();
            _productService = new ProductService(_mockUnitOfWork.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetProductByIdAsync_WhenProductExists_ReturnsProductDto()
        {
            // Arrange
            var productId = 1;
            var product = new Product { Id = productId, ProductName = "Test Product", CreatedBy = "Admin", CreatedOn = DateTime.UtcNow };
            var productDto = new ProductDto(productId, "Test Product", "Admin", DateTime.UtcNow, null, null, new List<ItemDto>());

            _mockUnitOfWork.Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _mockMapper.Setup(m => m.Map<ProductDto>(product))
                .Returns(productDto);

            // Act
            var result = await _productService.GetProductByIdAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
            Assert.Equal("Test Product", result.ProductName);
        }

        [Fact]
        public async Task GetProductByIdAsync_WhenProductDoesNotExist_ThrowsNotFoundException()
        {
            // Arrange
            var productId = 99;
            _mockUnitOfWork.Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _productService.GetProductByIdAsync(productId));
        }

        [Fact]
        public async Task CreateProductAsync_ValidRequest_CreatesAndReturnsProductDto()
        {
            // Arrange
            var request = new CreateProductRequest("New Product", "Admin", new List<CreateItemRequest> { new CreateItemRequest(5) });
            var product = new Product { ProductName = "New Product", CreatedBy = "Admin" };
            var expectedDto = new ProductDto(1, "New Product", "Admin", DateTime.UtcNow, null, null, new List<ItemDto> { new ItemDto(1, 1, 5) });

            _mockMapper.Setup(m => m.Map<Product>(request)).Returns(product);
            _mockUnitOfWork.Setup(u => u.Products.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>())).Returns(expectedDto);

            // Act
            var result = await _productService.CreateProductAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Product", result.ProductName);
            _mockUnitOfWork.Verify(u => u.Products.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_WhenProductExists_DeletesProduct()
        {
            // Arrange
            var productId = 1;
            var product = new Product { Id = productId, ProductName = "Delete Me" };

            _mockUnitOfWork.Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(u => u.Products.Remove(product));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _productService.DeleteProductAsync(productId);

            // Assert
            _mockUnitOfWork.Verify(u => u.Products.Remove(product), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetProductsAsync_ReturnsPagedProductDtos()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 2;
            var totalItems = 10;
            var products = new List<Product>
            {
                new Product { Id = 1, ProductName = "P1", CreatedBy = "Admin", CreatedOn = DateTime.UtcNow },
                new Product { Id = 2, ProductName = "P2", CreatedBy = "Admin", CreatedOn = DateTime.UtcNow }
            };
            var productDtos = new List<ProductDto>
            {
                new ProductDto(1, "P1", "Admin", DateTime.UtcNow, null, null, new List<ItemDto>()),
                new ProductDto(2, "P2", "Admin", DateTime.UtcNow, null, null, new List<ItemDto>())
            };

            _mockUnitOfWork.Setup(u => u.Products.GetPagedProductsAsync(pageNumber, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);
            _mockUnitOfWork.Setup(u => u.Products.CountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(totalItems);
            _mockMapper.Setup(m => m.Map<IEnumerable<ProductDto>>(products))
                .Returns(productDtos);

            // Act
            var result = await _productService.GetProductsAsync(pageNumber, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(pageNumber, result.PageNumber);
            Assert.Equal(pageSize, result.PageSize);
            Assert.Equal(totalItems, result.TotalItems);
            Assert.Equal(productDtos, result.Items);
        }

        [Fact]
        public async Task UpdateProductAsync_WhenProductExists_UpdatesAndReturnsProductDto()
        {
            // Arrange
            var productId = 1;
            var request = new UpdateProductRequest("Updated Product", "Modifier", new List<CreateItemRequest> { new CreateItemRequest(10) });
            var product = new Product { Id = productId, ProductName = "Old Product", CreatedBy = "Admin", CreatedOn = DateTime.UtcNow, Items = new List<Item>() };
            var expectedDto = new ProductDto(productId, "Updated Product", "Admin", DateTime.UtcNow, "Modifier", DateTime.UtcNow, new List<ItemDto> { new ItemDto(1, productId, 10) });

            _mockUnitOfWork.Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
            _mockUnitOfWork.Setup(u => u.Products.Update(product));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<ProductDto>(product)).Returns(expectedDto);

            // Act
            var result = await _productService.UpdateProductAsync(productId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Product", product.ProductName);
            Assert.Equal("Modifier", product.ModifiedBy);
            Assert.Single(product.Items);
            Assert.Equal(10, product.Items.First().Quantity);
            _mockUnitOfWork.Verify(u => u.Products.Update(product), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_WhenProductDoesNotExist_ThrowsNotFoundException()
        {
            // Arrange
            var productId = 99;
            var request = new UpdateProductRequest("Name", "Modifier", new List<CreateItemRequest>());
            _mockUnitOfWork.Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _productService.UpdateProductAsync(productId, request));
        }

        [Fact]
        public async Task DeleteProductAsync_WhenProductDoesNotExist_ThrowsNotFoundException()
        {
            // Arrange
            var productId = 99;
            _mockUnitOfWork.Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _productService.DeleteProductAsync(productId));
        }
    }
}
