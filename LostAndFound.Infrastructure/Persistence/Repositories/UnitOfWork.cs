using LostAndFound.Application.Interfaces;

namespace LostAndFound.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IItemRepository? _items;
    private ICategoryRepository? _categories;
    private IClaimRepository? _claims;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IItemRepository Items => _items ??= new ItemRepository(_context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public IClaimRepository Claims => _claims ??= new ClaimRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
