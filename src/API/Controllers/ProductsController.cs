using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Application.DTOs.Product;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Responses;

namespace ProductManagement.API.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<ProductDto>>>> GetAll([FromQuery] ProductQueryParameters query, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductsAsync(query.PageNumber, query.PageSize, cancellationToken);
            return Ok(ApiResponse<PagedResponse<ProductDto>>.SuccessResponse(result, "Products retrieved successfully"));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<ProductDto>.SuccessResponse(result, "Product retrieved successfully"));
        }

        [HttpGet("{id:int}/items")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ItemDto>>>> GetItemsByProductId([FromRoute] int id, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductByIdAsync(id, cancellationToken);
            return Ok(ApiResponse<IEnumerable<ItemDto>>.SuccessResponse(product.Items, "Product items retrieved successfully"));
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
        {
            var result = await _productService.CreateProductAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ProductDto>.SuccessResponse(result, "Product created successfully"));
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> Update([FromRoute] int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
        {
            var result = await _productService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(ApiResponse<ProductDto>.SuccessResponse(result, "Product updated successfully"));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<object>>> Delete([FromRoute] int id, CancellationToken cancellationToken)
        {
            await _productService.DeleteProductAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null!, "Product deleted successfully"));
        }
    }
}
