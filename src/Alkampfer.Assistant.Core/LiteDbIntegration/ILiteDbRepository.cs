using System.Threading.Tasks;

namespace Alkampfer.Assistant.Core.LiteDbIntegration;

public interface ILiteDbRepository<T> where T : LiteDbBaseClass
{
    Task SaveAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> LoadByIdAsync(string id, CancellationToken cancellationToken = default);
}
