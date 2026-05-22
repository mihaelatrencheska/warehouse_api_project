namespace BoutiqueInventory.Application.Interfaces;

/// <summary>
/// Coordinates a single transactional save across one or more
/// repositories. Repositories only mutate the change tracker; services
/// call <see cref="SaveChangesAsync"/> once at the end of an operation.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
