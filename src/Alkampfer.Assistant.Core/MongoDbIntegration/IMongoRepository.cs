using System.Threading.Tasks;

namespace Alkampfer.Assistant.Core.MongoDbIntegration;

public interface IMongoRepository<T> where T : MongoBaseClass
{
    Task SaveAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> LoadByIdAsync(string id, CancellationToken cancellationToken = default);
}
