using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductManagement.API.Controllers;
using ProductManagement.Application.DTOs.Product;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Responses;
using Xunit;

namespace ProductManagement.API.Tests
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _mockProductService;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _mockProductService = new Mock<IProductService>();
            _controller = new ProductsController(_mockProductService.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithPagedProducts()
        {
            // Arrange
            var query = new ProductQueryParameters(1, 10);
            var pagedResponse = PagedResponse<ProductDto>.Create(new List<ProductDto>(), 1, 10, 0);
            _mockProductService.Setup(s => s.GetProductsAsync(1, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResponse);

            // Act
            var result = await _controller.GetAll(query, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedResponse<ProductDto>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(pagedResponse, apiResponse.Data);
        }

        [Fact]
        public async Task GetById_ExistingProduct_ReturnsOkWithProduct()
        {
            // Arrange
            var productId = 1;
            var productDto = new ProductDto(productId, "Test Product", "Admin", DateTime.UtcNow, null, null, new List<ItemDto>());
            _mockProductService.Setup(s => s.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(productDto);

            // Act
            var result = await _controller.GetById(productId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(productDto, apiResponse.Data);
        }

        [Fact]
        public async Task GetItemsByProductId_ReturnsOkWithItems()
        {
            // Arrange
            var productId = 1;
            var items = new List<ItemDto> { new ItemDto(10, productId, 5) };
            var productDto = new ProductDto(productId, "Test Product", "Admin", DateTime.UtcNow, null, null, items);
            _mockProductService.Setup(s => s.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(productDto);

            // Act
            var result = await _controller.GetItemsByProductId(productId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<IEnumerable<ItemDto>>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(items, apiResponse.Data);
        }

        [Fact]
        public async Task Create_ValidProduct_ReturnsCreatedAtAction()
        {
            // Arrange
            var request = new CreateProductRequest("P1", "Admin", new List<CreateItemRequest>());
            var productDto = new ProductDto(1, "P1", "Admin", DateTime.UtcNow, null, null, new List<ItemDto>());
            _mockProductService.Setup(s => s.CreateProductAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(productDto);

            // Act
            var result = await _controller.Create(request, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductDto>>(createdResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(productDto, apiResponse.Data);
        }

        [Fact]
        public async Task Update_ValidProduct_ReturnsOkWithProduct()
        {
            // Arrange
            var productId = 1;
            var request = new UpdateProductRequest("P1-Updated", "Admin", new List<CreateItemRequest>());
            var productDto = new ProductDto(productId, "P1-Updated", "Admin", DateTime.UtcNow, "Admin", DateTime.UtcNow, new List<ItemDto>());
            _mockProductService.Setup(s => s.UpdateProductAsync(productId, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(productDto);

            // Act
            var result = await _controller.Update(productId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductDto>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(productDto, apiResponse.Data);
        }

        [Fact]
        public async Task Delete_ExistingProduct_ReturnsOk()
        {
            // Arrange
            var productId = 1;
            _mockProductService.Setup(s => s.DeleteProductAsync(productId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(productId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(apiResponse.Success);
        }
    }
}
