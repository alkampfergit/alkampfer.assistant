# Vector store query patterns and examples ✅

This page documents how to build queries against the vector store using the query abstraction in this repository (`VectorQuery`, `FilterBuilder`, and the various `IQueryFilter` implementations).

## Overview 🔧

- Use `VectorQuery` to build the search payload: text search, paging, and filters.
- Use `FilterBuilder` (or the convenience `Where*` / `With*` methods) to create typed filters.
- Filters are plain objects (e.g., `StringEqualsFilter`, `KeywordEqualsFilter`, `NumericRangeFilter`, `AndFilter`, `OrFilter`, `NotFilter`) and are converted by store-specific code (e.g., `ElasticQueryFilterConverter`) into the concrete backend query.

Files to reference:
- `src/Alkampfer.Assistant.Interfaces/Memories/VectorQuery.cs`
- `src/Alkampfer.Assistant.Interfaces/Memories/FilterBuilder.cs`
- `src/Alkampfer.Assistant.Interfaces/Memories/QueryFilters.cs`
- `src/Alkampfer.Assistant.ElasticSearch/ElasticQueryFilterConverter.es9.cs` (example of mapping semantics)

---

## Building queries — basic patterns 🧩

### 1) Simple equality filters (convenience methods)

These are ergonomics helpers on `VectorQuery`.

```csharp
var q = VectorQuery.Create()
    .WhereEquals("name", "John")        // analyzed string match
    .WhereEquals("age", 30)              // integer equality
    .WhereEquals("active", true);        // boolean equality
```

Notes:
- `WhereKeywordEquals(field, value)` is for keyword/exact match (case-insensitive in our elastic mapping).
- Methods ignore `null` values (no-op) so they chain safely.

### 2) Date/numeric ranges

```csharp
var q = VectorQuery.Create()
    .WhereDateRange("created_at", DateTime.UtcNow.AddYears(-1), DateTime.UtcNow)
    .WhereRange("rating", 4.0, 5.0);  // numeric range
``` 

Range filters accept `null` bounds to express open ranges and optional include/exclude flags.

### 3) Adding raw filters

If you already have a filter instance, add it with:

```csharp
q.WithFilter(new KeywordEqualsFilter("category", "tech"));
```

### 4) Boolean composition: AND / OR / NOT

Use the fluent `WithAndFilter`, `WithOrFilter`, `WithNotFilter` helpers or build nested filters with `FilterBuilder`.

```csharp
q.WithOrFilter(
    new IntegerEqualsFilter("year", 2021),
    new StringEqualsFilter("category", "tech")
);

q.WithAndFilter(
    new StringEqualsFilter("category", "tech"),
    new IntegerEqualsFilter("year", 2022)
);

q.WithNotFilter(new KeywordEqualsFilter("status", "deleted"));
```

Behavior (Elasticsearch example):
- `AndFilter` => `bool.must`
- `OrFilter` => `bool.should` with minimumShouldMatch = 1
- `NotFilter` => `bool.must_not`

---

## Using the `FilterBuilder` for nested/complex queries 🏗️

The `Where(Func<FilterBuilder, IQueryFilter>)` overload is convenient for building deeply nested filters:

```csharp
// (A = 12 OR B = "pippo") AND pluto = "xxx"
var q = VectorQuery.Create()
    .Where(f => f.And(
        f.Or(
            f.Equals("A", 12),
            f.Equals("B", "pippo")
        ),
        f.Equals("pluto", "xxx")
    ));
```

`FilterBuilder` exposes typed constructors for all supported filter types:
- `Equals(field, value)` (overloads for string/int/double/bool/DateTime)
- `KeywordEquals(field, value)`
- `DateRange(field, from, to, includeFrom = true, includeTo = true)`
- `NumericRange` / `IntegerRange`
- `And(...)`, `Or(...)`, `Not(...)`

Builder patterns are expressive and safe: pass `null` to `Where(...)` and nothing changes (no filter added).

---

## Common real-world examples 🔍

1) Find active users who are either premium or have high engagement and created last year:

```csharp
var q = VectorQuery.Create()
    .WhereEquals("active", true)
    .Where(f => f.Or(
        f.Equals("subscription", "premium"),
        f.NumericRange("engagement_score", 80.0, null)
    ))
    .WhereDateRange("created_at", new DateTime(2023,1,1), new DateTime(2023,12,31));
```

2) Exclude items whose status is `inactive` or `deleted`:

```csharp
var q = VectorQuery.Create()
    .Where(f => f.Not(
        f.Or(
            f.Equals("status", "inactive"),
            f.Equals("status", "deleted")
        )
    ));
```

3) Full-text search + filter + paging:

```csharp
var q = VectorQuery.Create()
    .WithSearchText("how to upgrade index")
    .WhereKeywordEquals("category", "docs")
    .WithLimit(20)
    .WithSkip(40);
```

---

## Semantics & tips 💡

- Prefer `KeywordEquals` for exact, case-insensitive matches (IDs, categories, statuses). Use `StringEquals` when you want analyzed, full-text matching.
- `VectorQuery` methods are fluent and ignore null inputs so you can safely conditionally add clauses.
- Composite filters are preserved as objects and converted by the concrete store adapter; check `ElasticQueryFilterConverter` for the canonical mapping used by the Elasticsearch implementation.
- If you need storage-specific constructs (e.g., geo or custom analyzers), extend the `IQueryFilter` set and update the converter for that store.

---

## Vector Search (KNN - K-Nearest Neighbors) 🎯

The vector query language now supports **semantic vector search** using K-Nearest Neighbors (KNN) algorithm. When a vector search is specified, it replaces text search as the primary ranking mechanism, with metadata filters executing **inside** the KNN algorithm for maximum efficiency.

### Key Features

- **Efficient filtering**: Filters run inside the HNSW graph traversal, not as post-processing
- **Semantic search**: Find similar documents using vector embeddings
- **Combined with metadata**: Apply any metadata filters alongside vector similarity
- **Simple API**: Natural extension of the existing `VectorQuery` fluent builder
- **No interface changes**: Uses the existing `IVectorStore.QueryAsync(IVectorQuery query)` method

### Basic Vector Search

```csharp
// Simple semantic search - find top 10 most similar documents
var queryVector = new float[] { 0.1f, 0.2f, 0.3f, ... };  // Your embedding vector

var query = VectorQuery.Create()
    .WithVectorSearch("embedding", queryVector, topK: 10);

var results = await vectorStore.QueryAsync(query);
```

**Parameters:**
- `vectorKey`: Name of the vector field (e.g., "embedding", "title_embedding")
- `queryVector`: The query embedding to find similar vectors for
- `topK`: Maximum number of results to return (default: 10)

### Vector Search with Filters

Combine semantic search with metadata filters for powerful filtered similarity search:

```csharp
// Find similar technology documents with high ratings
var queryVector = GetEmbedding("artificial intelligence machine learning");

var query = VectorQuery.Create()
    .WithVectorSearch("embedding", queryVector, topK: 10)
    .WhereKeywordEquals("category", "technology")
    .WhereRange("rating", 4.0, 5.0);

var results = await vectorStore.QueryAsync(query);
```

### Complex Filtered Vector Search

Use the full power of `FilterBuilder` with vector search:

```csharp
// Production example: semantic search with multiple filters
var queryVector = GetEmbedding("latest tech trends");
var fromDate = DateTime.Now.AddDays(-30);

var query = VectorQuery.Create()
    .WithVectorSearch("embedding", queryVector, topK: 10)
    .Where(f => f.And(
        f.KeywordEquals("category", "technology"),
        f.NumericRange("rating", 4.0, 5.0),
        f.DateRange("publishedDate", fromDate, DateTime.Now)
    ));

var results = await vectorStore.QueryAsync(query);
```

**All filter types work with vector search:**
- Equality filters: `WhereEquals`, `WhereKeywordEquals`
- Range filters: `WhereRange`, `WhereDateRange`
- Composite filters: `And`, `Or`, `Not` (using `FilterBuilder`)

### Advanced: Custom NumCandidates

For power users who need to tune recall vs performance:

```csharp
// Specify custom NumCandidates for better recall with selective filters
var query = VectorQuery.Create()
    .WithVectorSearch(
        vectorKey: "embedding",
        queryVector: queryVector,
        topK: 10,
        numCandidates: 50  // Consider more candidates when filters are selective
    );
```

**NumCandidates** controls how many candidates the KNN algorithm considers before applying filters:
- **Default**: `max(100, topK * 10)` - sensible for most use cases
- **Higher values**: Better recall but slower queries
- **Rule of thumb**: Increase if filters eliminate many candidates

### Important Notes

⚠️ **Mutually Exclusive**: Vector search and text search cannot be combined in the same query
```csharp
// ❌ This will throw InvalidOperationException
var query = VectorQuery.Create()
    .WithSearchText("elasticsearch")  // Text search
    .WithVectorSearch("embedding", queryVector);  // Vector search - ERROR!
```

✅ **Text search still works unchanged**:
```csharp
// Traditional text search continues to work as before
var textQuery = VectorQuery.Create()
    .WithSearchText("elasticsearch tutorial")
    .WhereKeywordEquals("category", "technology")
    .WithLimit(20);

var textResults = await vectorStore.QueryAsync(textQuery);
```

### Performance Considerations

1. **Filters inside KNN**: Filters execute during HNSW graph traversal, not as post-processing
2. **Selective filters**: Very selective filters (matching few documents) may need higher `numCandidates`
3. **Vector exclusion**: Vectors are not returned in results by default (reduces network transfer)
4. **Index configuration**: Ensure vector fields are indexed with appropriate settings:
   ```csharp
   await indexer.EnsureVectorFieldMappingAsync(
       indexName,
       "embedding",
       dimensions: 384,
       similarity: "cosine",  // or "dot_product", "l2_norm"
       indexVectors: true
   );
   ```

### Examples from Tests

See `ElasticKnnFilteredQueryTests.cs` for comprehensive examples:
- Basic KNN without filters
- KNN with single/multiple filters
- Complex nested filters with KNN
- Date range filtering
- OR/NOT logic with KNN
- Edge cases and validation

---

## Quick reference (cheat sheet) 🧾

- **Vector search**: `WithVectorSearch("embedding", queryVector, topK: 10)`
- Simple equals: `WhereEquals("field", value)`
- Keyword exact: `WhereKeywordEquals("field", value)`
- Range: `WhereRange("field", from, to)` or `WhereDateRange(...)`
- Compose: `WithAndFilter`, `WithOrFilter`, `WithNotFilter`
- Nested: `Where(f => f.And(...))` using `FilterBuilder`
- Search text + paging: `WithSearchText(...)`, `WithLimit(...)`, `WithSkip(...)`
- **Note**: Vector search and text search are mutually exclusive

---

If you'd like, I can add a live example that runs an integration test or add short how-to snippets to the `README` for the `Alkampfer.Assistant.ElasticSearch` project. ✨
