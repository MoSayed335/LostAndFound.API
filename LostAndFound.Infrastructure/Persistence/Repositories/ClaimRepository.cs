using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Infrastructure.Persistence.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly ApplicationDbContext _context;

    public ClaimRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Claim?> GetByIdAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? _context.Claims.AsNoTracking() : _context.Claims;
        return await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Claim?> GetByIdWithItemAndClaimantAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Claims
            .Include(c => c.Item)
            .Include(c => c.Claimant)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> HasPendingClaimAsync(int itemId, int claimantId, CancellationToken cancellationToken = default)
    {
        return await _context.Claims.AnyAsync(c =>
            c.ItemId == itemId &&
            c.ClaimantId == claimantId &&
            c.Status == ClaimStatus.Pending,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Claim>> GetPendingClaimsForItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        return await _context.Claims
            .Where(c => c.ItemId == itemId && c.Status == ClaimStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public IQueryable<Claim> GetQueryable(bool asNoTracking = true)
    {
        return asNoTracking ? _context.Claims.AsNoTracking() : _context.Claims;
    }

    public async Task AddAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        await _context.Claims.AddAsync(claim, cancellationToken);
    }

    public void Update(Claim claim)
    {
        _context.Claims.Update(claim);
    }

    public void UpdateRange(IEnumerable<Claim> claims)
    {
        _context.Claims.UpdateRange(claims);
    }
}
