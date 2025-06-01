using System.Threading.Tasks;

namespace Alkampfer.Assistant.Core.MongoDbIntegration;

public interface IMongoRepository<T>
{
    Task SaveAsync(T entity);
    Task<T> LoadByIdAsync(string id);
}
