---
applyTo: '**/*.es9.cs'
---

## Overview & Context

This guide provides comprehensive instructions for GitHub Copilot to assist with Elasticsearch 9 .NET client (Elastic.Clients.Elasticsearch) development, with special emphasis on:
- Field mapping strategies and patterns
- Dynamic mapping configuration
- Dense vector mapping for vector search and kNN operations
- Modern C# async patterns (async/await)
- FluentAPI descriptor pattern

**Target Library:** `Elastic.Clients.Elasticsearch` (v9.x)  
**Framework:** .NET 10.0+  
**Language:** C# with modern language features

# Serialization 

I like to personally manage the serialization because it allows me to have full control on how data is stored and retrieved. The class that gets saved into elastic should implement a method like this:

```csharp
      public IDictionary<string, object> ToExpandoObjectForIndexing()
      {
          var obj = new ExpandoObject() as IDictionary<string, object>;
          obj[IdFieldName] = Id;
          obj[TitleFieldName] = Title;
```

And a corresponding analog method to deserialize from json element.

```csharp
    static MyClassName FromJsonElement(System.Text.Json.JsonElement source)
    {
        MyClassName item = new MyClassName(source.GetProperty(IdFieldName).GetString());
        item.Title = source.TryGetProperty(TitleFieldName, out var titleProp) ? titleProp.GetString() : null;
```

# Indexing

You should verify if the index exists, if not create and inlude the mapping (see later section). This is an example code on how you create the settings

```csharp
            CreateIndexRequest request = new(indexName);
        request.Settings = new IndexSettings();
        request.Settings.NumberOfReplicas = _config.NumberOfReplicas;
        request.Settings.NumberOfShards = _config.NumberOfShards;
        request.Settings.Analysis = OmniSearchIndexMapper9.CreateAnalysisConfiguration();
        request.Mappings = OmniSearchIndexMapper9.CreateMappingConfiguration();

        var typeMapping = new TypeMapping();
        typeMapping.Source = new SourceField() { Enabled = true };

        var properties = new Properties<OmniSearchItem>();

        typeMapping.Properties = properties;

        return request;
```

Indexing should use previous methods

```csharp
    var obj = myObject.ToExpandoObjectForIndexing();
    var indexResponse = await elasticClient.IndexAsync(obj, "my-index-name");
```

# Mapping 

Elastic needs mapping, here is how you can map property of an index.

## Field Mapping Fundamentals

# Elasticsearch 9 .NET Mapping Guide

Library: Elastic.Clients.Elasticsearch v9.x
Pattern: TypeMapping + Properties with direct property assignment

## Basic Field Mapping

Standard Pattern:

```csharp
using Elastic.Clients.Elasticsearch.Mapping;

TypeMapping mapping = new();
mapping.Properties = new Properties<YourClass>();

mapping.Properties["fieldName"] = new KeywordProperty() { Normalizer = "lowercase" };
mapping.Properties["textField"] = new TextProperty() { Analyzer = "standard" };
```

Common Field Types:

```csharp
// Text
mapping.Properties["description"] = new TextProperty() { Analyzer = "standard" };

// Keyword
mapping.Properties["category"] = new KeywordProperty() { Normalizer = "lowercase" };

// Numbers
mapping.Properties["quantity"] = new IntegerNumberProperty();
mapping.Properties["price"] = new DoubleNumberProperty();

// Boolean and Date
mapping.Properties["isActive"] = new BooleanProperty();
mapping.Properties["createdDate"] = new DateProperty();

// Object
var objectProp = new ObjectProperty();
objectProp.Properties = new Properties<YourClass>();
objectProp.Properties["subField"] = new TextProperty();
mapping.Properties["metadata"] = objectProp;

// Nested
var nestedProp = new NestedProperty();
nestedProp.Properties = new Properties<YourClass>();
nestedProp.Properties["name"] = new TextProperty();
nestedProp.Properties["value"] = new IntegerNumberProperty();
mapping.Properties["items"] = nestedProp;
```

## Dynamic Mapping

Dynamic Behavior:

```csharp
mapping.Dynamic = DynamicType.False;   // Ignore unknown fields
mapping.Dynamic = DynamicType.Strict;  // Reject unknown fields
mapping.Dynamic = DynamicType.True;    // Auto-map unknown fields
```

Dynamic Templates:

```csharp
var dynamicTemplates = new List<IDictionary<string, DynamicTemplate>>();

// String fields matching "s_*"
var stringTemplate = DynamicTemplate.Mapping(new TextProperty
{
    Analyzer = "standard",
    Fields = new Properties<YourClass>
    {
        { "raw", new KeywordProperty() },
        { "lowercase", new KeywordProperty { Normalizer = "lowercase" } }
    }
});
stringTemplate.Match = ["s_*"];

var stringDict = new Dictionary<string, DynamicTemplate>();
stringDict["StringProperties"] = stringTemplate;
dynamicTemplates.Add(stringDict);

// Numeric fields matching "n_*"
var numericTemplate = DynamicTemplate.Mapping(new DoubleNumberProperty());
numericTemplate.Match = ["n_*"];

var numericDict = new Dictionary<string, DynamicTemplate>();
numericDict["NumericProperties"] = numericTemplate;
dynamicTemplates.Add(numericDict);

// Date fields matching "d_*"
var dateTemplate = DynamicTemplate.Mapping(new DateProperty());
dateTemplate.Match = ["d_*"];

var dateDict = new Dictionary<string, DynamicTemplate>();
dateDict["DateProperties"] = dateTemplate;
dynamicTemplates.Add(dateDict);

mapping.DynamicTemplates = dynamicTemplates;
```

Path Matching:

```csharp
var template = DynamicTemplate.Mapping(new KeywordProperty());
template.PathMatch = ["metadata.*"];
template.PathUnmatch = ["metadata.internal*"];
```

## Dense Vector Mapping

Basic Dense Vector:

```csharp
mapping.Properties["embedding"] = new DenseVectorProperty();
```

Production Dense Vector:

```csharp
mapping.Properties["embedding"] = new DenseVectorProperty()
{
    Dims = 1536,
    Index = true,
    Similarity = DenseVectorSimilarity.Cosine,
    ElementType = DenseVectorElementType.Float,
    IndexOptions = new HnswIndexOptions()
    {
        Type = HnswAlgorithmType.Hnsw,
        Params = new HnswAlgorithmParams
        {
            EfConstruction = 400,
            M = 16,
        }
    }
};
```

Similarity Options:

```csharp
DenseVectorSimilarity.Cosine      // Recommended
DenseVectorSimilarity.DotProduct  // Fast, requires normalized vectors
DenseVectorSimilarity.L2Norm      // Euclidean distance
```

Byte Quantization:

```csharp
mapping.Properties["embedding"] = new DenseVectorProperty()
{
    Dims = 1536,
    ElementType = DenseVectorElementType.Byte,
    Index = true,
    IndexOptions = new HnswIndexOptions()
    {
        Type = HnswAlgorithmType.BbqHnsw,
        Params = new HnswAlgorithmParams
        {
            EfConstruction = 512,
            M = 32,
        }
    },
    Similarity = DenseVectorSimilarity.Cosine
};
```

Dynamic Dense Vector Template:

```csharp
var vectorTemplate = DynamicTemplate.Mapping(new DenseVectorProperty
{
    Index = true,
    Similarity = DenseVectorSimilarity.Cosine,
    Dims = 1536,
    ElementType = DenseVectorElementType.Float,
    IndexOptions = new HnswIndexOptions()
    {
        Type = HnswAlgorithmType.Hnsw,
        Params = new HnswAlgorithmParams { EfConstruction = 400, M = 16 }
    }
});
vectorTemplate.Match = ["v1536_*"];

var vectorDict = new Dictionary<string, DynamicTemplate>();
vectorDict["DenseVector1536"] = vectorTemplate;
dynamicTemplates.Add(vectorDict);
```

## Complete Example

```csharp
public static TypeMapping CreateProductMapping()
{
    TypeMapping mapping = new();
    mapping.Dynamic = DynamicType.False;
    mapping.Properties = new Properties<Product>();

    // Basic fields
    mapping.Properties["id"] = new KeywordProperty();
    mapping.Properties["name"] = new TextProperty() { Analyzer = "standard" };
    mapping.Properties["price"] = new DoubleNumberProperty();
    mapping.Properties["category"] = new KeywordProperty() { Normalizer = "lowercase" };
    mapping.Properties["tags"] = new KeywordProperty();
    mapping.Properties["createdDate"] = new DateProperty();

    // Vector field
    mapping.Properties["embedding"] = new DenseVectorProperty()
    {
        Dims = 1536,
        Index = true,
        Similarity = DenseVectorSimilarity.Cosine,
        ElementType = DenseVectorElementType.Float,
    };

    // Nested reviews
    var reviewsNested = new NestedProperty();
    reviewsNested.Properties = new Properties<Product>();
    reviewsNested.Properties["author"] = new KeywordProperty();
    reviewsNested.Properties["rating"] = new IntegerNumberProperty();
    reviewsNested.Properties["comment"] = new TextProperty();
    mapping.Properties["reviews"] = reviewsNested;

    // Dynamic templates
    var dynamicTemplates = new List<IDictionary<string, DynamicTemplate>>();
    
    var stringTemplate = DynamicTemplate.Mapping(new TextProperty
    {
        Fields = new Properties<Product>
        {
            { "raw", new KeywordProperty() },
            { "lowercase", new KeywordProperty { Normalizer = "lowercase" } }
        }
    });
    stringTemplate.Match = ["s_*"];
    var stringDict = new Dictionary<string, DynamicTemplate>();
    stringDict["Strings"] = stringTemplate;
    dynamicTemplates.Add(stringDict);

    var numericTemplate = DynamicTemplate.Mapping(new DoubleNumberProperty());
    numericTemplate.Match = ["n_*"];
    var numericDict = new Dictionary<string, DynamicTemplate>();
    numericDict["Numbers"] = numericTemplate;
    dynamicTemplates.Add(numericDict);

    mapping.DynamicTemplates = dynamicTemplates;
    return mapping;
}
```

## Applying Mappings

```csharp
var response = await client.Indices.PutMappingAsync<Product>(indexName, m =>
{
    m.Properties = mapping.Properties;
    m.DynamicTemplates = mapping.DynamicTemplates;
    m.Dynamic = mapping.Dynamic;
    return m;
});

if (!response.IsValidResponse)
{
    throw new InvalidOperationException($"Mapping failed: {response.DebugInformation}");
}
```

## Common Mistakes

Wrong - Fluent API chains:
```csharp
mapping.Properties(p => p.Text(t => t.Title))
```

Correct - Direct assignment:
```csharp
mapping.Properties["title"] = new TextProperty();
```

Wrong - Not initializing Properties:
```csharp
mapping.Properties["field"] = new TextProperty();
```

Correct - Initialize first:
```csharp
mapping.Properties = new Properties<YourClass>();
mapping.Properties["field"] = new TextProperty();
```

Wrong - Template not wrapped:
```csharp
mapping.DynamicTemplates = new DynamicTemplate { };
```

Correct - Proper structure:
```csharp
var dict = new Dictionary<string, DynamicTemplate>();
dict["TemplateName"] = template;
var list = new List<IDictionary<string, DynamicTemplate>>();
list.Add(dict);
mapping.DynamicTemplates = list;
```

## Quick Reference

Field Types:
- Text: TextProperty (Analyzer, Fields)
- Keyword: KeywordProperty (Normalizer)
- Integer/Long: IntegerNumberProperty, LongNumberProperty
- Float/Double: FloatNumberProperty, DoubleNumberProperty
- Boolean: BooleanProperty
- Date: DateProperty (Format)
- Object: ObjectProperty (Properties)
- Nested: NestedProperty (Properties)
- DenseVector: DenseVectorProperty (Dims, Index, Similarity)

Dense Vector Types:
- HnswAlgorithmType.Hnsw (Standard float)
- HnswAlgorithmType.Int8Hnsw (Int8 quantization)
- HnswAlgorithmType.BbqHnsw (Balanced byte quantization)

Dynamic Template Matching:
- template.Match = ["s_*"] (Name pattern)
- template.PathMatch = ["metadata.*"] (Path pattern)
- template.MatchMappingType = "string" (Type-based)

## Key Rules

1. Always use TypeMapping as root container
2. Initialize Properties before assigning
3. Use string keys for field names: mapping.Properties["fieldName"]
4. Dense vectors need Dims matching embedding model
5. Dynamic templates: wrap in Dictionary then add to List
6. Each template dictionary needs unique name key
7. Nested/Object properties have their own Properties
8. Always check response.IsValidResponse

# Queries

# Elasticsearch Query Guide (Elastic.Clients.Elasticsearch v9)

A focused, implementation-oriented reference for building Elasticsearch queries using the Elastic.Clients.Elasticsearch v9 types. Keep this short and use it as a checklist or snippet bank when translating high-level filter tokens to ES queries.

## Core rules

- Use the `Query` factory helpers (e.g. `Query.Term`, `Query.Range`, `Query.Match`, `Query.Bool`, `Query.Prefix`, `Query.Wildcard`, `Query.Terms`).
- Prefer `BoolQuery` with `Must`/`Should`/`MustNot` for logical composition (AND/OR/NOT).
- Respect field analyzer suffixes in mappings:
  - `.raw` — not analyzed, preserves case (use for exact, case-sensitive matches)
  - `.lowercase` (or similar) — not analyzed, lowercased for case-insensitive exact matches
  - no suffix — analyzed (full-text)
- For nested arrays/objects use `NestedQuery` and set `Path` to the nested field.
- For Terms queries use `FieldValue` wrappers if needed (e.g. `FieldValue.String(value)`).

## Patterns & Examples

Note: replace `fieldName` / `nested.path` / `value` with your concrete names.

### Exact (term) match — case sensitive

```csharp
return Query.Term(new TermQuery("myField.raw") { Value = value });
```

### Exact (term) match — case insensitive

```csharp
return Query.Match(new MatchQuery("myField.lowercase") { Query = value.ToLower() });
```

### Full-text match (analyzed)

```csharp
return Query.Match(new MatchQuery("myField") { Query = value });
```

### Range queries (numbers / dates)

```csharp
// numeric
return Query.Range(new NumberRangeQuery("price") { Gte = 10, Lt = 100 });

// date
return Query.Range(new DateRangeQuery("createdAt") { Gte = start, Lte = end });
```

### Prefix / StartsWith

```csharp
return Query.Prefix(new PrefixQuery("name.lowercase") { Value = prefix.ToLower() });
```

### Wildcard / Contains / EndsWith

```csharp
// contains
return Query.Wildcard(new WildcardQuery("description.lowercase") { Value = "*" + term.ToLower() + "*" });

// ends with
return Query.Wildcard(new WildcardQuery("tag.raw") { Value = "*" + suffix });
```

### OneOf (terms) / NotOneOf

```csharp
// one of (case sensitive on raw)
return Query.Terms(new TermsQuery()
{
    Field = "status.raw",
    Terms = new TermsQueryField(values.Select(v => FieldValue.String(v)).ToArray())
});

// not one of
return Query.Bool(new BoolQuery() { MustNot = new Query[] { /* the Terms query above */ } });
```

### Exists / IsNull / IsNotNull

```csharp
// exists
return Query.Exists(new ExistsQuery { Field = "someField" });

// is null (no value)
return Query.Bool(new BoolQuery() { MustNot = new Query[] { Query.Exists(new ExistsQuery { Field = "someField" }) } });
```

### Boolean composition (AND / OR / NOT)

```csharp
// AND
return Query.Bool(new BoolQuery() { Must = new Query[] { q1, q2 } });

// OR
return Query.Bool(new BoolQuery() { Should = new Query[] { q1, q2 } });

// NOT
return Query.Bool(new BoolQuery() { MustNot = new Query[] { qNot } });
```

Helper pattern when combining many clauses:

```csharp
public static Query And(IReadOnlyCollection<Query> queries)
{
    if (queries.Count == 0) return new MatchAllQuery();
    if (queries.Count == 1) return queries.Single();
    return Query.Bool(new BoolQuery { Must = queries.ToArray() });
}
```

## Nested queries

- Build nested inner conditions into a Query (or Bool), then wrap with `NestedQuery` and set `Path`.

```csharp
var inner = Query.Bool(new BoolQuery { Must = new Query[] { /* inner conditions */ } });
var nested = new NestedQuery { Path = "items", Query = inner };
return nested;
```

## Dual-path (direct field + nested) — combine with OR

When a property can exist either as a top-level field or inside a nested array, build both queries and `Should` them:

```csharp
var direct = Query.Term(new TermQuery("field.raw") { Value = v });
var nested = new NestedQuery { Path = "nested", Query = Query.Bool(new BoolQuery { Must = new [] { /* path filter, name, value */ } }) };
return Query.Bool(new BoolQuery { Should = new Query[] { direct, nested } });
```

## Implementation tips

- Prefer building small `Query` pieces and combine them — easier to test and reason about.
- Normalize values for case-insensitive checks (e.g. `.ToLower()` when querying `.lowercase` fields).
- Use `Match` for analyzed / full-text search and `Term` for exact matches.
- When building `TermsQuery`, prefer `FieldValue.String(...)` to avoid type ambiguity.
- For complex nested filters, ensure the nested `Path` exactly matches the mapping.
- Use `BoolQuery` arrays (Must/Should/MustNot) and return a `MatchAllQuery` for empty conjunctions.


---

