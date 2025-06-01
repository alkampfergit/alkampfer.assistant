using MongoDB.Driver;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Core.MongoDbIntegration;

public class MongoRepository<T> : IMongoRepository<T>
{
    private readonly IMongoCollection<T> _collection;

    public MongoRepository(string connectionString, string databaseName, string collectionName)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<T>(collectionName);
    }

    public async Task SaveAsync(T entity)
    {
        // Assumes entity has an Id property of type string
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null)
            throw new Exception("Entity must have an Id property");
        var id = idProperty.GetValue(entity)?.ToString();
        if (string.IsNullOrEmpty(id))
            throw new Exception("Id property must not be null or empty");
        var filter = Builders<T>.Filter.Eq("Id", id);
        await _collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true });
    }

    public async Task<T> LoadByIdAsync(string id)
    {
        var filter = Builders<T>.Filter.Eq("Id", id);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }
}
