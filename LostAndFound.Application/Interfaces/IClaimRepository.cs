using LostAndFound.Domain.Entities;

namespace LostAndFound.Application.Interfaces;

public interface IClaimRepository
{
    Task<Claim?> GetByIdAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default);
    Task<Claim?> GetByIdWithItemAndClaimantAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasPendingClaimAsync(int itemId, int claimantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Claim>> GetPendingClaimsForItemAsync(int itemId, CancellationToken cancellationToken = default);
    IQueryable<Claim> GetQueryable(bool asNoTracking = true);
    Task AddAsync(Claim claim, CancellationToken cancellationToken = default);
    void Update(Claim claim);
    void UpdateRange(IEnumerable<Claim> claims);
}
