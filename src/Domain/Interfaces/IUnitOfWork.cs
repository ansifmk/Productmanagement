using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProductManagement.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        IUserRepository Users { get; }
        IItemRepository Items { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
