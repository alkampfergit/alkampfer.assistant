using LiteDB;
using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Core.LiteDbIntegration;

public class LiteDbCounterManager : ICounterManager
{
    private readonly string _connectionString;

    public LiteDbCounterManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<long> GenerateNewCounterAsync(string counterName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            var collection = db.GetCollection<CounterDocument>("counters");
            
            var counter = collection.FindOne(x => x.Name == counterName);
            if (counter == null)
                throw new InvalidOperationException($"Counter '{counterName}' is not initialized. Call InitSeedAsync first.");

            counter.Value++;
            collection.Update(counter);
            
            return counter.Value;
        }, cancellationToken);
    }

    public async Task InitSeedAsync(string counterName, long seed = 0, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            var collection = db.GetCollection<CounterDocument>("counters");
            
            var existing = collection.FindOne(x => x.Name == counterName);
            if (existing == null)
            {
                var doc = new CounterDocument { Name = counterName, Value = seed };
                collection.Insert(doc);
            }
        }, cancellationToken);
    }

    private class CounterDocument
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public long Value { get; set; }
    }
}
