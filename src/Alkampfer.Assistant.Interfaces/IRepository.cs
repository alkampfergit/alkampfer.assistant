using System.Threading.Tasks;

namespace Alkampfer.Assistant.Interfaces;

/// <summary>
/// Repository interface for entities with strongly-typed identities.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TId">The identity type.</typeparam>
public interface IRepository<T, TId> 
    where T : BaseEntity<TId> 
    where TId : Identity
{
    /// <summary>
    /// Saves or updates an entity asynchronously.
    /// </summary>
    /// <param name="entity">The entity to save or update</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SaveAsync(T entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads an entity by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the entity</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T> LoadByIdAsync(TId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads all entities from the repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A collection of all entities</returns>
    Task<IEnumerable<T>> LoadAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a queryable interface for complex queries on the entity collection.
    /// </summary>
    IQueryable<T> AsQueryable { get; }
    
    /// <summary>
    /// Deletes an entity by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
}
