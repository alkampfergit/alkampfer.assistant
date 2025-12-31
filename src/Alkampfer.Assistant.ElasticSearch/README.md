# Alkampfer.Assistant.ElasticSearch

Elasticsearch 8+ integration for indexing and retrieving `VectorRecord` objects with dynamic metadata mapping.

## Features

- ✅ **Dynamic Metadata Mapping**: Automatic field type mapping based on prefixes
  - `s_*` → String fields (text with raw/lowercase variants)
  - `i_*` → Integer fields
  - `n_*` → Numeric (double) fields
  - `b_*` → Boolean fields
  - `d_*` → DateTime fields (stored as ISO8601 strings)
- ✅ **Resilient Operations**: Automatic retry with exponential backoff for transient failures
- ✅ **Batch Processing**: Configurable batch size (default 200) for bulk indexing
- ✅ **Index Management**: Automatic index creation with proper mapping configuration
- ✅ **Type-Safe Serialization**: Round-trip preservation of all metadata types

## Configuration

```csharp
var config = new ElasticSearchConfiguration
{
    Address = "http://localhost:9200",          // Elasticsearch URL
    Username = null,                             // Optional: basic auth username
    Password = null,                             // Optional: basic auth password
    ShardNumber = 1,                             // Primary shards (default: 1)
    ReplicaNumber = 1,                           // Replica shards (default: 1)
    BulkBatchSize = 200,                         // Records per batch (default: 200)
    MaxRetries = 3,                              // Retry attempts (default: 3)
    InitialRetryDelaySeconds = 1                 // Initial backoff (default: 1s)
};
```

## Usage

### Creating an Indexer

```csharp
var indexer = new ElasticIndexer(config);
```

### Ensuring Index Mapping

```csharp
await indexer.EnsureIndexMappingAsync("my-index");
```

### Indexing Records

```csharp
var records = new[]
{
    VectorRecord.Create("id-1", "doc-1")
        .WithMetadata("title", "Document Title")       // → s_title
        .WithMetadata("count", 42)                     // → i_count
        .WithMetadata("score", 95.5)                   // → n_score
        .WithMetadata("published", true)               // → b_published
        .WithMetadata("created", DateTime.UtcNow)      // → d_created
};

var result = await indexer.IndexRecordsAsync("my-index", records);

if (result.IsSuccess)
{
    Console.WriteLine($"Indexed {result.SuccessfulRecords} records");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Failed: {error.RecordId} - {error.ErrorMessage}");
    }
}
```

### Retrieving Records

```csharp
var record = await indexer.GetRecordAsync("my-index", "id-1");
if (record != null)
{
    var title = record.GetMetadataAsString("title");
    var count = record.GetMetadataAsInt("count");
    var score = record.GetMetadataAsDouble("score");
    var published = record.GetMetadataAsBool("published");
    var created = record.GetMetadataAsDateTime("created");
}
```

### Deleting an Index

```csharp
await indexer.DeleteIndexAsync("my-index");
```

## Testing

Integration tests require an Elasticsearch instance. Set the `ELASTIC_TEST_URL` environment variable:

```bash
export ELASTIC_TEST_URL=http://localhost:9200
dotnet test --filter "FullyQualifiedName~ElasticSearch"
```

Tests will:
- ✅ Validate index names
- ✅ Create and verify index mappings
- ✅ Test single and batch indexing
- ✅ Verify all metadata types round-trip correctly
- ✅ Handle missing documents (404 returns null)
- ✅ Automatically clean up test indexes

## Architecture

### Components

- **ElasticSearchConfiguration**: Configuration record with validation
- **ElasticIndexer**: Main indexer class with resilience pipeline
- **ElasticVectorRecordMapping**: Static mapping configuration
- **VectorRecordExtensions**: Serialization/deserialization extensions
- **BulkIndexResult**: Result tracking for batch operations

### Resilience

Built on `Microsoft.Extensions.Resilience` with exponential backoff retry policy:
- Retries transient HTTP errors (429, 503, timeouts, network failures)
- Configurable max retries and initial delay
- Continues processing batches on partial failures

### Future Enhancements

- [ ] Dense vector field support for kNN search
- [ ] True bulk API usage (currently indexes one document at a time)
- [ ] API key authentication (currently basic auth only)
- [ ] Streaming chunking for very large record sets
- [ ] Custom analyzers and tokenizers
- [ ] IVectorStore interface implementation

## Dependencies

- `Elastic.Clients.Elasticsearch` (v9.2.2)
- `Microsoft.Extensions.Resilience` (v9.0.0)
- `Alkampfer.Assistant.Interfaces`

## License

See repository root for license information.
