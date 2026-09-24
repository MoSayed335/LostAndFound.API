using LostAndFound.Domain.Entities;

namespace LostAndFound.Application.Interfaces;

public interface ICategoryRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
}
