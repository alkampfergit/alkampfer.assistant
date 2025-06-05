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

    public IQueryable<T> AsQueryable
    {
        get
        {
            using var db = new LiteDatabase(_connectionString);
            var collection = db.GetCollection<T>(_collectionName);

            //really not production ready code. Consider if the 
            //repository could be made disposable and using a factory.
            return collection.FindAll().ToList().AsQueryable();
        }
    }

    public async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(entity.Id))
            throw new Exception("Id property must not be null or empty");

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<T>(_collectionName);
        collection.Upsert(entity);
        await Task.FromResult(0);
    }

    public async Task<T> LoadByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<T>(_collectionName);
        var result = collection.FindById(id);
        return await Task.FromResult(result);
    }

    public async Task<IEnumerable<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<T>(_collectionName);
        var result = collection.FindAll().ToList();
        return await Task.FromResult(result);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<T>(_collectionName);
        collection.Delete(id);
        await Task.FromResult(0);
    }
}
