using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Infrastructure.Persistence.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly ApplicationDbContext _context;

    public ItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Item?> GetByIdAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? _context.Items.AsNoTracking() : _context.Items;
        return await query.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public Task<Item?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        GetByIdWithDetailsAsync(id, asNoTracking: false, cancellationToken);

    public async Task<Item?> GetByIdWithDetailsAsync(int id, bool asNoTracking, CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? _context.Items.AsNoTracking() : _context.Items;
        return await query
            .Include(i => i.Category)
            .Include(i => i.User)
            .Include(i => i.Claims)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public IQueryable<Item> GetQueryable(bool asNoTracking = true)
    {
        return asNoTracking ? _context.Items.AsNoTracking() : _context.Items;
    }

    public async Task<List<Item>> GetActiveItemsByTypeAsync(ItemType type, int excludeItemId, CancellationToken cancellationToken = default)
    {
        return await _context.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .Where(i => i.Status == ItemStatus.Active && i.Type == type && i.Id != excludeItemId)
            .ToListAsync(cancellationToken);
    }


    public async Task AddAsync(Item item, CancellationToken cancellationToken = default)
    {
        await _context.Items.AddAsync(item, cancellationToken);
    }

    public void Update(Item item)
    {
        _context.Items.Update(item);
    }

    public void Delete(Item item)
    {
        _context.Items.Remove(item);
    }

    public async Task<bool> HasClaimsAsync(int itemId, CancellationToken cancellationToken = default)
    {
        return await _context.Claims.AnyAsync(c => c.ItemId == itemId, cancellationToken);
    }

    public void RemoveClaims(IEnumerable<Claim> claims)
    {
        _context.Claims.RemoveRange(claims);
    }
}
