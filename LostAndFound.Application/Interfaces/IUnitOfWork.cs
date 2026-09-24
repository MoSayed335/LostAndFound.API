namespace LostAndFound.Application.Interfaces;

public interface IUnitOfWork
{
    IItemRepository Items { get; }
    ICategoryRepository Categories { get; }
    IClaimRepository Claims { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
