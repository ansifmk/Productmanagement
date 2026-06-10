using System;
using System.Threading;
using System.Threading.Tasks;
using ProductManagement.Application.DTOs.Product;
using ProductManagement.Application.Responses;

namespace ProductManagement.Application.Interfaces
{
    public interface IProductService
    {
        Task<PagedResponse<ProductDto>> GetProductsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
        Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
        Task DeleteProductAsync(int id, CancellationToken cancellationToken = default);
    }
}
