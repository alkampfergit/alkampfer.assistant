using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

[Trait("Category", "Integration")]
public class TestIndexUtilsTests : IClassFixture<ElasticServerAvailabilityFixture>
{
    private readonly ElasticServerAvailabilityFixture _availability;

    public TestIndexUtilsTests(ElasticServerAvailabilityFixture availability)
    {
        _availability = availability;
    }

    [Fact]
    public void AssertIsTestIndexName_Throws_OnInvalidName()
    {
        Assert.Throws<ArgumentException>(() => TestIndexUtils.AssertIsTestIndexName("test-foo"));
        Assert.Throws<ArgumentException>(() => TestIndexUtils.AssertIsTestIndexName(""));
    }

    [Fact]
    public async Task CleanupAllIndicesAsync_Deletes_AatestIndex()
    {
        var client = TestIndexUtils.CreateClientFromEnv();
        var indexName = $"aatest-temp-{Guid.NewGuid():N}";

        try
        {
            // Create index
            await client.Indices.CreateAsync(indexName);

            var all = await TestIndexUtils.EnumerateIndicesAsync(client);
            Assert.Contains(indexName, all);

            // Run cleanup (force=false so only aatest- indices will be deleted)
            var deleted = await TestIndexUtils.CleanupAllIndicesAsync(client, force: false, skipPredicate: name => name.StartsWith('.'));
            Assert.Contains(indexName, deleted);

            var remaining = await TestIndexUtils.EnumerateIndicesAsync(client);
            Assert.DoesNotContain(indexName, remaining);
        }
        finally
        {
            try { await client.Indices.DeleteAsync(indexName); } catch { }
        }
    }
}
