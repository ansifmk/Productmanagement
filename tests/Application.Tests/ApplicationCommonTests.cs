using AutoMapper;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.DTOs.Auth;
using ProductManagement.Application.DTOs.Product;
using ProductManagement.Application.Mapping;
using ProductManagement.Application.Responses;
using System;
using System.Collections.Generic;
using Xunit;

namespace ProductManagement.Tests
{
    public class ApplicationCommonTests
    {
        [Fact]
        public void MappingProfile_Configuration_IsValid()
        {
            // Arrange
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());

            // Assert
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void ApiResponse_SuccessResponse_SetsCorrectProperties()
        {
            // Act
            var response = ApiResponse<string>.SuccessResponse("test-data", "Success message");

            // Assert
            Assert.True(response.Success);
            Assert.Equal("test-data", response.Data);
            Assert.Equal("Success message", response.Message);
            Assert.Empty(response.Errors);
        }

        [Fact]
        public void ApiResponse_Failure_SetsCorrectProperties()
        {
            // Arrange
            var errors = new[] { "error 1", "error 2" };

            // Act
            var response = ApiResponse<object>.Failure("Failed message", errors);

            // Assert
            Assert.False(response.Success);
            Assert.Null(response.Data);
            Assert.Equal("Failed message", response.Message);
            Assert.Equal(errors, response.Errors);
        }

        [Fact]
        public void PagedResponse_Create_SetsCorrectProperties()
        {
            // Arrange
            var data = new List<string> { "item1", "item2" };

            // Act
            var response = PagedResponse<string>.Create(data, 1, 10, 2);

            // Assert
            Assert.Equal(data, response.Items);
            Assert.Equal(1, response.PageNumber);
            Assert.Equal(10, response.PageSize);
            Assert.Equal(2, response.TotalItems);
            Assert.Equal(1, response.TotalPages);
        }

        [Fact]
        public void Exceptions_Constructor_SetsMessage()
        {
            // Act
            var authEx = new AuthenticationException("Auth failed");
            var notFoundEx = new NotFoundException("Not found");

            // Assert
            Assert.Equal("Auth failed", authEx.Message);
            Assert.Equal("Not found", notFoundEx.Message);
        }

        [Fact]
        public void DtoRecord_Properties_VerifyInit()
        {
            // Arrange
            var authResponse = new AuthResponse(Guid.Empty, "user", "email", "role", "access", "refresh", DateTime.MinValue);
            var itemDto = new ItemDto(1, 10, 2);
            var productDto = new ProductDto(1, "name", "cb", DateTime.MinValue, "mb", DateTime.MinValue, new List<ItemDto>());

            // Assert
            Assert.Equal(Guid.Empty, authResponse.UserId);
            Assert.Equal("user", authResponse.Username);
            Assert.Equal(1, itemDto.Id);
            Assert.Equal("name", productDto.ProductName);

            // Record compiler-generated methods coverage (Equals, ToString, GetHashCode)
            var authResponse2 = authResponse with { };
            Assert.Equal(authResponse, authResponse2);
            Assert.Equal(authResponse.GetHashCode(), authResponse2.GetHashCode());
            Assert.NotEmpty(authResponse.ToString());

            var itemDto2 = itemDto with { };
            Assert.Equal(itemDto, itemDto2);
            Assert.Equal(itemDto.GetHashCode(), itemDto2.GetHashCode());
            Assert.NotEmpty(itemDto.ToString());

            var productDto2 = productDto with { };
            Assert.Equal(productDto, productDto2);
            Assert.Equal(productDto.GetHashCode(), productDto2.GetHashCode());
            Assert.NotEmpty(productDto.ToString());

            // Positional deconstruction checks
            var (id, prodName, cb, co, mb, mo, items) = productDto;
            Assert.Equal(1, id);
            Assert.Equal("name", prodName);

            var (itemId, itemProdId, itemQty) = itemDto;
            Assert.Equal(1, itemId);
            Assert.Equal(10, itemProdId);
            Assert.Equal(2, itemQty);

            // Other request records
            var loginReq1 = new LoginRequest("e", "p");
            var loginReq2 = loginReq1 with { };
            Assert.Equal(loginReq1, loginReq2);
            Assert.NotEmpty(loginReq1.ToString());

            var registerReq1 = new RegisterRequest("u", "e", "p");
            var registerReq2 = registerReq1 with { };
            Assert.Equal(registerReq1, registerReq2);
            Assert.NotEmpty(registerReq1.ToString());

            var createItemReq1 = new CreateItemRequest(5);
            var createItemReq2 = createItemReq1 with { };
            Assert.Equal(createItemReq1, createItemReq2);
            Assert.NotEmpty(createItemReq1.ToString());

            var createProdReq1 = new CreateProductRequest("name", "user", new List<CreateItemRequest>());
            var createProdReq2 = createProdReq1 with { };
            Assert.Equal(createProdReq1, createProdReq2);
            Assert.NotEmpty(createProdReq1.ToString());

            var updateProdReq1 = new UpdateProductRequest("name", "user", new List<CreateItemRequest>());
            var updateProdReq2 = updateProdReq1 with { };
            Assert.Equal(updateProdReq1, updateProdReq2);
            Assert.NotEmpty(updateProdReq1.ToString());
        }
    }
}
