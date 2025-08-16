# NEST 7 Driver Elasticsearch Mapping Comprehensive Guide

This guide provides complete reference code for using NEST 7 driver in .NET to create standard and dynamic mappings with Elasticsearch, demonstrating all basic data types and advanced mapping techniques.

## Table of Contents

1. [Basic Setup](#basic-setup)
2. [Standard Mapping Examples](#standard-mapping-examples)
3. [AutoMap with Attributes](#automap-with-attributes)
4. [Dynamic Mapping](#dynamic-mapping)
5. [Complex Mapping Examples](#complex-mapping-examples)
6. [Best Practices](#best-practices)
7. [Common Data Types Reference](#common-data-types-reference)

---

## Basic Setup

### NuGet Package Installation
```bash
Install-Package NEST -Version 7.17.5
```

### Connection Setup
```csharp
using Nest;
using System;
using System.Collections.Generic;

// Basic NEST 7 Connection Setup
var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
    .DefaultIndex("my-index")
    .DefaultMappingFor<MyDocument>(m => m
        .IndexName("my-index")
        .IdProperty(p => p.Id)
    );

var client = new ElasticClient(settings);
```

---

## Standard Mapping Examples

### 1. Text and Keyword Mapping

```csharp
// Text and Keyword Mapping
client.Indices.Create("my-index", c => c
    .Map<Product>(m => m
        .Properties(p => p
            .Text(t => t
                .Name(n => n.Title)
                .Analyzer("standard")
                .Fields(f => f
                    .Keyword(k => k.Name("keyword"))
                )
            )
            .Keyword(k => k
                .Name(n => n.Category)
                .IgnoreAbove(256)
            )
        )
    )
);
```

### 2. Numeric Data Types Mapping

```csharp
// Numeric Data Types Mapping
client.Indices.Create("my-index", c => c
    .Map<Product>(m => m
        .Properties(p => p
            .Number(n => n
                .Name(x => x.Price)
                .Type(NumberType.Double)
            )
            .Number(n => n
                .Name(x => x.Quantity)
                .Type(NumberType.Integer)
            )
            .Number(n => n
                .Name(x => x.Rating)
                .Type(NumberType.Float)
            )
            .Number(n => n
                .Name(x => x.Id)
                .Type(NumberType.Long)
            )
            .Number(n => n
                .Name(x => x.SmallNumber)
                .Type(NumberType.Short)
            )
            .Number(n => n
                .Name(x => x.TinyNumber)
                .Type(NumberType.Byte)
            )
        )
    )
);
```

### 3. Date Mapping

```csharp
// Date Mapping
client.Indices.Create("my-index", c => c
    .Map<Product>(m => m
        .Properties(p => p
            .Date(d => d
                .Name(x => x.CreatedDate)
                .Format("yyyy-MM-dd||yyyy-MM-dd'T'HH:mm:ss||strict_date_optional_time")
            )
            .Date(d => d
                .Name(x => x.UpdatedAt)
                .Format("epoch_millis")
            )
        )
    )
);
```

### 4. Boolean Mapping

```csharp
// Boolean Mapping
client.Indices.Create("my-index", c => c
    .Map<Product>(m => m
        .Properties(p => p
            .Boolean(b => b
                .Name(x => x.IsActive)
            )
            .Boolean(b => b
                .Name(x => x.InStock)
            )
        )
    )
);
```

### 5. Geo Point Mapping

```csharp
// Geo Point Mapping
client.Indices.Create("my-index", c => c
    .Map<Location>(m => m
        .Properties(p => p
            .GeoPoint(g => g
                .Name(x => x.Coordinates)
            )
            .Text(t => t
                .Name(x => x.Address)
            )
        )
    )
);

// Location class example
public class Location
{
    public GeoLocation Coordinates { get; set; }
    public string Address { get; set; }
}
```

### 6. Nested Object Mapping

```csharp
// Nested Object Mapping
client.Indices.Create("my-index", c => c
    .Map<Order>(m => m
        .Properties(p => p
            .Keyword(k => k.Name(x => x.OrderId))
            .Nested<OrderItem>(n => n
                .Name(x => x.Items)
                .Properties(np => np
                    .Text(t => t.Name(ni => ni.ProductName))
                    .Number(num => num
                        .Name(ni => ni.Quantity)
                        .Type(NumberType.Integer)
                    )
                    .Number(num => num
                        .Name(ni => ni.Price)
                        .Type(NumberType.Double)
                    )
                )
            )
        )
    )
);

// Order and OrderItem classes
public class Order
{
    public string OrderId { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class OrderItem
{
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public double Price { get; set; }
}
```

---

## AutoMap with Attributes

```csharp
// AutoMap with Attributes
[ElasticsearchType(IdProperty = nameof(Id))]
public class Product
{
    public long Id { get; set; }
    
    [Text(Analyzer = "standard")]
    [Keyword(Name = "title_keyword", IgnoreAbove = 256)]
    public string Title { get; set; }
    
    [Keyword]
    public string Category { get; set; }
    
    [Number(Type = NumberType.Double)]
    public decimal Price { get; set; }
    
    [Date(Format = "yyyy-MM-dd")]
    public DateTime CreatedDate { get; set; }
    
    [Boolean]
    public bool IsActive { get; set; }
    
    [Nested]
    public List<ProductAttribute> Attributes { get; set; }
    
    [GeoPoint]
    public GeoLocation Location { get; set; }
    
    [Ignore]
    public string InternalNotes { get; set; }
}

[ElasticsearchType]
public class ProductAttribute
{
    [Keyword]
    public string Name { get; set; }
    
    [Text]
    public string Value { get; set; }
}

// Using AutoMap
client.Indices.Create("products", c => c
    .Map<Product>(m => m
        .AutoMap()
        .Properties(p => p
            // Override any specific mappings if needed
            .Text(t => t
                .Name(n => n.Title)
                .Fields(f => f
                    .Keyword(k => k.Name("sort").Normalizer("lowercase"))
                )
            )
        )
    )
);
```

---

## Dynamic Mapping

### 1. Basic Dynamic Mapping

```csharp
// Basic Dynamic Mapping
client.Indices.Create("my-dynamic-index", c => c
    .Map<object>(m => m
        .Dynamic(true)  // Allow new fields to be added dynamically
        .Properties(p => p
            .Text(t => t.Name("title"))
            .Keyword(k => k.Name("category"))
        )
    )
);
```

### 2. Dynamic Templates

```csharp
// Dynamic Templates
client.Indices.Create("my-index", c => c
    .Map<object>(m => m
        .DynamicTemplates(dt => dt
            .DynamicTemplate("strings_as_keywords", t => t
                .MatchMappingType("string")
                .Mapping(tm => tm
                    .Keyword(k => k
                        .IgnoreAbove(256)
                    )
                )
            )
            .DynamicTemplate("integers_as_longs", t => t
                .MatchMappingType("long")
                .Mapping(tm => tm
                    .Number(n => n
                        .Type(NumberType.Long)
                    )
                )
            )
            .DynamicTemplate("dates_detection", t => t
                .Match("*_date")
                .Mapping(tm => tm
                    .Date(d => d
                        .Format("yyyy-MM-dd||epoch_millis")
                    )
                )
            )
            .DynamicTemplate("geo_fields", t => t
                .Match("*_location")
                .Mapping(tm => tm
                    .GeoPoint()
                )
            )
        )
    )
);
```

### 3. Copy To Field Example

```csharp
// Copy To Field Example
client.Indices.Create("my-index", c => c
    .Map<Product>(m => m
        .Properties(p => p
            .Text(t => t
                .Name(n => n.Title)
                .CopyTo(ct => ct.Field("search_all"))
            )
            .Text(t => t
                .Name(n => n.Description)
                .CopyTo(ct => ct.Field("search_all"))
            )
            .Text(t => t
                .Name("search_all")
                .Store(false)
            )
        )
    )
);
```

### 4. Path Match Dynamic Template

```csharp
// Path Match Dynamic Template
client.Indices.Create("my-index", c => c
    .Map<object>(m => m
        .DynamicTemplates(dt => dt
            .DynamicTemplate("nested_strings", t => t
                .PathMatch("metadata.*")
                .MatchMappingType("string")
                .Mapping(tm => tm
                    .Text(txt => txt
                        .Fields(f => f
                            .Keyword(k => k.Name("keyword"))
                        )
                    )
                )
            )
        )
    )
);
```

---

## Complex Mapping Examples

### 1. Multi-Field Mapping

```csharp
// Multi-Field Mapping
client.Indices.Create("my-index", c => c
    .Map<Document>(m => m
        .Properties(p => p
            .Text(t => t
                .Name(n => n.Content)
                .Analyzer("standard")
                .Fields(f => f
                    .Text(ft => ft
                        .Name("english")
                        .Analyzer("english")
                    )
                    .Text(ft => ft
                        .Name("shingles") 
                        .Analyzer("shingle_analyzer")
                    )
                    .Keyword(k => k
                        .Name("keyword")
                        .IgnoreAbove(256)
                    )
                )
            )
        )
    )
);
```

### 2. Custom Analyzer with Mapping

```csharp
// Custom Analyzer with Mapping
client.Indices.Create("my-index", c => c
    .Settings(s => s
        .Analysis(a => a
            .Analyzers(an => an
                .Custom("custom_analyzer", ca => ca
                    .Tokenizer("standard")
                    .Filters("lowercase", "stop", "stemmer")
                )
            )
            .TokenFilters(tf => tf
                .Stemmer("stemmer", st => st
                    .Language("english")
                )
            )
        )
    )
    .Map<Document>(m => m
        .Properties(p => p
            .Text(t => t
                .Name(n => n.Content)
                .Analyzer("custom_analyzer")
            )
        )
    )
);
```

### 3. Complete Product Example with All Data Types

```csharp
// Complete Product Example with All Data Types
public class Product
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public GeoLocation Location { get; set; }
    public List<ProductAttribute> Attributes { get; set; }
    public List<string> Tags { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

client.Indices.Create("products", c => c
    .Settings(s => s
        .NumberOfShards(2)
        .NumberOfReplicas(1)
        .Analysis(a => a
            .Analyzers(an => an
                .Standard("standard_analyzer")
                .Custom("product_analyzer", ca => ca
                    .Tokenizer("standard")
                    .Filters("lowercase", "stop")
                )
            )
        )
    )
    .Map<Product>(m => m
        .Dynamic(false)  // Strict mapping - only defined fields
        .Properties(p => p
            .Number(n => n
                .Name(x => x.Id)
                .Type(NumberType.Long)
            )
            .Text(t => t
                .Name(x => x.Title)
                .Analyzer("product_analyzer")
                .Fields(f => f
                    .Keyword(k => k
                        .Name("keyword")
                        .IgnoreAbove(256)
                    )
                    .Text(ft => ft
                        .Name("suggest")
                        .Analyzer("simple")
                    )
                )
                .CopyTo(ct => ct.Field("search_all"))
            )
            .Text(t => t
                .Name(x => x.Description)
                .Analyzer("standard")
                .CopyTo(ct => ct.Field("search_all"))
            )
            .Keyword(k => k
                .Name(x => x.Category)
                .IgnoreAbove(64)
            )
            .Number(n => n
                .Name(x => x.Price)
                .Type(NumberType.ScaledFloat)
                .ScalingFactor(100)  // For currency precision
            )
            .Number(n => n
                .Name(x => x.Stock)
                .Type(NumberType.Integer)
            )
            .Boolean(b => b
                .Name(x => x.IsActive)
            )
            .Date(d => d
                .Name(x => x.CreatedDate)
                .Format("yyyy-MM-dd'T'HH:mm:ss||epoch_millis")
            )
            .GeoPoint(g => g
                .Name(x => x.Location)
            )
            .Nested<ProductAttribute>(n => n
                .Name(x => x.Attributes)
                .Properties(np => np
                    .Keyword(k => k.Name(a => a.Name))
                    .Text(t => t.Name(a => a.Value))
                )
            )
            .Keyword(k => k
                .Name(x => x.Tags)
            )
            .Object<Dictionary<string, object>>(o => o
                .Name(x => x.Metadata)
                .Dynamic(true)
            )
            .Text(t => t
                .Name("search_all")
                .Store(false)
                .Analyzer("standard")
            )
        )
    )
);
```

---

## Best Practices

### 1. Mapping Validation

```csharp
// Validate Mapping Before Creating
var mappingValidation = client.Indices.ValidateQuery<Product>(v => v
    .Index("products")
    .Query(q => q.MatchAll())
);

if (!mappingValidation.IsValid)
{
    // Handle mapping validation errors
    Console.WriteLine($"Mapping validation failed: {mappingValidation.ServerError}");
}
```

### 2. Index Template for Consistent Mapping

```csharp
// Index Template for Consistent Mapping
client.Indices.PutTemplate("product_template", t => t
    .IndexPatterns("products-*")
    .Settings(s => s
        .NumberOfShards(2)
        .NumberOfReplicas(1)
    )
    .Map<Product>(m => m
        .AutoMap()
        .Properties(p => p
            .Text(txt => txt
                .Name(n => n.Title)
                .Analyzer("standard")
                .Fields(f => f
                    .Keyword(k => k.Name("keyword"))
                )
            )
        )
    )
);
```

### 3. Update Existing Mapping

```csharp
// Update Existing Mapping (Add New Fields)
client.Map<Product>(m => m
    .Index("products")
    .Properties(p => p
        .Text(t => t
            .Name("new_field")
            .Analyzer("standard")
        )
        .Keyword(k => k
            .Name("another_new_field")
        )
    )
);
```

---

## Common Data Types Reference

### Core Data Types

| NEST Type | Elasticsearch Type | C# Type | Example |
|-----------|-------------------|---------|---------|
| `.Text()` | `text` | `string` | Full-text search |
| `.Keyword()` | `keyword` | `string` | Exact match, sorting |
| `.Number(NumberType.Long)` | `long` | `long` | Large integers |
| `.Number(NumberType.Integer)` | `integer` | `int` | Standard integers |
| `.Number(NumberType.Short)` | `short` | `short` | Small integers |
| `.Number(NumberType.Byte)` | `byte` | `byte` | Tiny integers |
| `.Number(NumberType.Double)` | `double` | `double` | Large decimals |
| `.Number(NumberType.Float)` | `float` | `float` | Standard decimals |
| `.Boolean()` | `boolean` | `bool` | True/false values |
| `.Date()` | `date` | `DateTime` | Date/time values |
| `.Binary()` | `binary` | `byte[]` | Binary data |

### Complex Data Types

| NEST Type | Elasticsearch Type | Usage |
|-----------|-------------------|--------|
| `.Object<T>()` | `object` | Single JSON object |
| `.Nested<T>()` | `nested` | Array of objects |
| `.GeoPoint()` | `geo_point` | Latitude/longitude |
| `.GeoShape()` | `geo_shape` | Complex geographic shapes |
| `.Ip()` | `ip` | IP addresses |
| `.Completion()` | `completion` | Auto-complete suggestions |

### Advanced Features

- **Multi-fields**: Index same field multiple ways
- **Copy To**: Combine multiple fields into one search field
- **Dynamic Templates**: Rules for dynamically added fields
- **Analyzers**: Custom text processing
- **Normalizers**: Keyword field preprocessing

---

## Usage Examples

### Basic Document Indexing

```csharp
// Index a document
var product = new Product
{
    Id = 1,
    Title = "Sample Product",
    Category = "Electronics",
    Price = 99.99m,
    IsActive = true,
    CreatedDate = DateTime.Now
};

var response = await client.IndexDocumentAsync(product);
```

### Search with Mapping

```csharp
// Search using mapped fields
var searchResponse = await client.SearchAsync<Product>(s => s
    .Index("products")
    .Query(q => q
        .Bool(b => b
            .Must(m => m
                .Match(mt => mt
                    .Field(f => f.Title)
                    .Query("sample")
                )
            )
            .Filter(f => f
                .Term(t => t
                    .Field(field => field.IsActive)
                    .Value(true)
                )
            )
        )
    )
);
```