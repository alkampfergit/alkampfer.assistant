using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Alkampfer.Assistant.Core.MongoDbIntegration;

public class MongoCounterManager : ICounterManager
{
    private readonly IMongoCollection<CounterDocument> _collection;

    public MongoCounterManager(IMongoDatabase database)
    {
        _collection = database.GetCollection<CounterDocument>("counters");
    }

    public async Task<long> GenerateNewCounterAsync(string counterName, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CounterDocument>.Filter.Eq(x => x.Name, counterName);
        var update = Builders<CounterDocument>.Update.Inc(x => x.Value, 1);
        var options = new FindOneAndUpdateOptions<CounterDocument>
        {
            IsUpsert = false, // Now we assume the record exists
            ReturnDocument = ReturnDocument.After
        };

        var result = await _collection.FindOneAndUpdateAsync(
            filter,
            update,
            options,
            cancellationToken
        );

        if (result == null)
            throw new InvalidOperationException($"Counter '{counterName}' is not initialized. Call InitSeedAsync first.");

        return result.Value;
    }

    public async Task InitSeedAsync(string counterName, long seed = 0, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CounterDocument>.Filter.Eq(x => x.Name, counterName);
        var existing = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (existing == null)
        {
            var doc = new CounterDocument { Name = counterName, Value = seed };
            await _collection.InsertOneAsync(doc, null, cancellationToken);
        }
    }

    private class CounterDocument
    {
        public string Name { get; set; } = null!;
        public long Value { get; set; }
    }
}
