using System.Threading.Tasks;

namespace Alkampfer.Assistant.Core;

/// <summary>
/// Generic repository interface for data access operations on entities derived from BaseEntity.
/// </summary>
/// <typeparam name="T">The entity type that must inherit from BaseEntity</typeparam>
public interface IRepository<T> where T : BaseEntity
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
    Task<T> LoadByIdAsync(string id, CancellationToken cancellationToken = default);
    
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
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}