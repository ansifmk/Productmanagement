using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Domain.Interfaces
{
    public interface IProductRepository : IRepository<Product, int>
    {
        Task<IEnumerable<Product>> GetPagedProductsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<long> CountAsync(CancellationToken cancellationToken = default);
    }
}
