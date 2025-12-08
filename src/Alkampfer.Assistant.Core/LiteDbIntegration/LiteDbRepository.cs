using LiteDB;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces;

namespace Alkampfer.Assistant.Core.LiteDbIntegration;

public class LiteDbRepository<T, TId> : IRepository<T, TId> 
    where T : BaseEntity<TId> 
    where TId : Identity
{
    private readonly string _connectionString;
    private readonly string _collectionName;
    private readonly BsonMapper _mapper;

    public LiteDbRepository(string connectionString, string collectionName)
    {
        _connectionString = connectionString;
        _collectionName = collectionName;
        _mapper = new BsonMapper();
        
        // Register Identity serialization: persist as string, deserialize via constructor
        _mapper.RegisterType<TId>(
            serialize: id => id.Value,
            deserialize: bson => (TId)Activator.CreateInstance(typeof(TId), bson.AsString)!
        );
    }

    public IQueryable<T> AsQueryable
    {
        get
        {
            using var db = new LiteDatabase(_connectionString, _mapper);
            var collection = db.GetCollection<T>(_collectionName);

            //really not production ready code. Consider if the 
            //repository could be made disposable and using a factory.
            return collection.FindAll().ToList().AsQueryable();
        }
    }

    public async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (entity.Id == null)
            throw new Exception("Id property must not be null");

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<T>(_collectionName);
        collection.Upsert(entity);
        await Task.FromResult(0);
    }

    public async Task<T> LoadByIdAsync(TId id, CancellationToken cancellationToken = default)
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

    public async Task DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<T>(_collectionName);
        collection.Delete(id);
        await Task.FromResult(0);
    }
}
