using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;

namespace LostAndFound.Application.Interfaces;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default);
    Task<Item?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<Item?> GetByIdWithDetailsAsync(int id, bool asNoTracking, CancellationToken cancellationToken = default);
    IQueryable<Item> GetQueryable(bool asNoTracking = true);
    Task<List<Item>> GetActiveItemsByTypeAsync(ItemType type, int excludeItemId, CancellationToken cancellationToken = default);
    Task AddAsync(Item item, CancellationToken cancellationToken = default);
    void Update(Item item);
    void Delete(Item item);
    Task<bool> HasClaimsAsync(int itemId, CancellationToken cancellationToken = default);
    void RemoveClaims(IEnumerable<Claim> claims);
}

