using System;
using System.Threading;
using System.Threading.Tasks;
using ProductManagement.Domain.Interfaces;
using ProductManagement.Infrastructure.Data;

namespace ProductManagement.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly IItemRepository _itemRepository;

        public UnitOfWork(
            ApplicationDbContext context,
            IProductRepository productRepository,
            IUserRepository userRepository,
            IItemRepository itemRepository)
        {
            _context = context;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _itemRepository = itemRepository;
        }

        public IProductRepository Products => _productRepository;
        public IUserRepository Users => _userRepository;
        public IItemRepository Items => _itemRepository;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
