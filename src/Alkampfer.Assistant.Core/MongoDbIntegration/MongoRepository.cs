using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces;

namespace Alkampfer.Assistant.Core.MongoDbIntegration;

public class MongoRepository<T, TId> : IRepository<T, TId> 
    where T : BaseEntity<TId> 
    where TId : Identity
{
    private readonly IMongoCollection<T> _collection;

    static MongoRepository()
    {
        // Register Identity serializer to persist as string
        if (!BsonClassMap.IsClassMapRegistered(typeof(TId)))
        {
            BsonSerializer.RegisterSerializer(typeof(TId), new IdentitySerializer<TId>());
        }
    }

    public MongoRepository(string connectionString, string databaseName, string collectionName)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<T>(collectionName);
    }

    public IQueryable<T> AsQueryable => _collection.AsQueryable();

    public async Task SaveAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == null)
            throw new Exception("Id property must not be null");
        var filter = Builders<T>.Filter.Eq(x => x.Id, entity.Id);
        await _collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }

    public async Task<T> LoadByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(x => x.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.Find(Builders<T>.Filter.Empty).ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(x => x.Id, id);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }
}

/// <summary>
/// MongoDB BSON serializer for Identity types - persists as string.
/// </summary>
internal class IdentitySerializer<TId> : SerializerBase<TId> where TId : Identity
{
    public override TId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();
        return (TId)Activator.CreateInstance(typeof(TId), value)!;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TId value)
    {
        context.Writer.WriteString(value.Value);
    }
}
