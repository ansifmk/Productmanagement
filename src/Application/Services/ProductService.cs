using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Application.DTOs.Product;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Responses;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResponse<ProductDto>> GetProductsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var products = await _unitOfWork.Products.GetPagedProductsAsync(pageNumber, pageSize, cancellationToken);
            var totalItems = await _unitOfWork.Products.CountAsync(cancellationToken);
            
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            return PagedResponse<ProductDto>.Create(productDtos, pageNumber, pageSize, totalItems);
        }

        public async Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product lookup failed: ID {ProductId} not found.", id);
                throw new NotFoundException($"Product with ID {id} was not found.");
            }

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
        {
            var product = _mapper.Map<Product>(request);
            product.CreatedOn = DateTime.UtcNow;

            await _unitOfWork.Products.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product {ProductName} created successfully with ID {ProductId}.", product.ProductName, product.Id);

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product update failed: ID {ProductId} not found.", id);
                throw new NotFoundException($"Product with ID {id} was not found.");
            }

            // Map standard properties
            product.ProductName = request.ProductName;
            product.ModifiedBy = request.ModifiedBy;
            product.ModifiedOn = DateTime.UtcNow;

            // Clear old items and add new ones (cascade delete handles the database-level deletion)
            product.Items.Clear();
            foreach (var itemReq in request.Items)
            {
                product.Items.Add(new Item
                {
                    Quantity = itemReq.Quantity
                });
            }

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product with ID {ProductId} updated successfully by {ModifiedBy}.", id, request.ModifiedBy);

            return _mapper.Map<ProductDto>(product);
        }

        public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product deletion failed: ID {ProductId} not found.", id);
                throw new NotFoundException($"Product with ID {id} was not found.");
            }

            _unitOfWork.Products.Remove(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product with ID {ProductId} deleted successfully.", id);
        }
    }
}
