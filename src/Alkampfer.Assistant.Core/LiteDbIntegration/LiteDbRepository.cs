using LiteDB;
using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Core.LiteDbIntegration;

public class LiteDbRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly string _connectionString;
    private readonly string _collectionName;

    public LiteDbRepository(string connectionString, string collectionName)
    {
        _connectionString = connectionString;
        _collectionName = collectionName;
    }

    public async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entity.Id))
            throw new Exception("Id property must not be null or empty");

        await Task.Run(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            var collection = db.GetCollection<T>(_collectionName);
            collection.Upsert(entity);
        }, cancellationToken);
    }

    public async Task<T> LoadByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            var collection = db.GetCollection<T>(_collectionName);
            return collection.FindById(id);
        }, cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            var collection = db.GetCollection<T>(_collectionName);
            return collection.FindAll().ToList();
        }, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            var collection = db.GetCollection<T>(_collectionName);
            collection.Delete(id);
        }, cancellationToken);
    }
}
