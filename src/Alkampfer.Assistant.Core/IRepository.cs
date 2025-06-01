using System.Threading.Tasks;

namespace Alkampfer.Assistant.Core;

public interface IRepository<T> where T : BaseEntity
{
    Task SaveAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> LoadByIdAsync(string id, CancellationToken cancellationToken = default);
}