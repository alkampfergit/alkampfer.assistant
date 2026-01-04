using System.Collections.Generic;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Configuration for a vector field in Elasticsearch.
/// </summary>
public class VectorFieldConfiguration
{
    /// <summary>
    /// Gets or sets the name of the vector field.
    /// </summary>
    public required string FieldName { get; set; }

    /// <summary>
    /// Gets or sets the number of dimensions for the vector.
    /// </summary>
    public required int Dimensions { get; set; }

    /// <summary>
    /// Gets or sets the similarity function (cosine, dot_product, l2_norm).
    /// </summary>
    public string Similarity { get; set; } = "cosine";

    /// <summary>
    /// Gets or sets whether to index vectors for kNN search.
    /// </summary>
    public bool Index { get; set; } = true;

    /// <summary>
    /// Gets or sets the HNSW algorithm type (hnsw, int8_hnsw, bbq_hnsw).
    /// </summary>
    public string IndexType { get; set; } = "int8_hnsw";

    /// <summary>
    /// Gets or sets the HNSW M parameter (number of connections).
    /// </summary>
    public int M { get; set; } = 16;

    /// <summary>
    /// Gets or sets the HNSW ef_construction parameter.
    /// </summary>
    public int EfConstruction { get; set; } = 100;
}

/// <summary>
/// Provides mapping configuration for VectorRecord in Elasticsearch.
/// </summary>
public static class ElasticVectorRecordMapping
{
    /// <summary>
    /// Creates the type mapping for VectorRecord with dynamic templates for prefixed metadata fields.
    /// </summary>
    /// <param name="vectorFields">Optional collection of vector field configurations.</param>
    /// <returns>A configured TypeMapping for VectorRecord.</returns>
    public static TypeMapping GetTypeMapping(IEnumerable<VectorFieldConfiguration>? vectorFields = null)
    {
        var mapping = new TypeMapping
        {
            Dynamic = DynamicMapping.True
        };

        // Initialize properties
        mapping.Properties = new Properties<VectorRecord>();

        // Explicit mappings for core fields
        mapping.Properties["id"] = new KeywordProperty();
        mapping.Properties["documentId"] = new KeywordProperty();
        mapping.Properties["text"] = new TextProperty
        {
            Analyzer = "standard"
        };

        // Dynamic templates for prefixed metadata fields
        var dynamicTemplates = new List<IDictionary<string, DynamicTemplate>>();

        // String fields (s_*)
        var stringTemplate = new DynamicTemplate
        {
            Match = ["s_*"],
            Mapping = new TextProperty
            {
                Analyzer = "standard",
                Fields = new Properties<VectorRecord>
                {
                    { "raw", new KeywordProperty() },
                    { "lowercase", new KeywordProperty { Normalizer = "lowercase" } }
                }
            }
        };
        var stringDict = new Dictionary<string, DynamicTemplate>
        {
            ["StringMetadata"] = stringTemplate
        };
        dynamicTemplates.Add(stringDict);

        // Integer fields (i_*)
        var integerTemplate = new DynamicTemplate
        {
            Match = ["i_*"],
            Mapping = new IntegerNumberProperty()
        };
        var integerDict = new Dictionary<string, DynamicTemplate>
        {
            ["IntegerMetadata"] = integerTemplate
        };
        dynamicTemplates.Add(integerDict);

        // Numeric fields (n_*)
        var numericTemplate = new DynamicTemplate
        {
            Match = ["n_*"],
            Mapping = new DoubleNumberProperty()
        };
        var numericDict = new Dictionary<string, DynamicTemplate>
        {
            ["NumericMetadata"] = numericTemplate
        };
        dynamicTemplates.Add(numericDict);

        // Boolean fields (b_*)
        var booleanTemplate = new DynamicTemplate
        {
            Match = ["b_*"],
            Mapping = new BooleanProperty()
        };
        var booleanDict = new Dictionary<string, DynamicTemplate>
        {
            ["BooleanMetadata"] = booleanTemplate
        };
        dynamicTemplates.Add(booleanDict);

        // Date fields (d_*)
        var dateTemplate = new DynamicTemplate
        {
            Match = ["d_*"],
            Mapping = new DateProperty()
        };
        var dateDict = new Dictionary<string, DynamicTemplate>
        {
            ["DateMetadata"] = dateTemplate
        };
        dynamicTemplates.Add(dateDict);

        // Keywords fields (k_*) - lowercase ignore case
        var keywordsTemplate = new DynamicTemplate
        {
            Match = ["k_*"],
            Mapping = new KeywordProperty
            {
                Normalizer = "lowercase"
            }
        };
        var keywordsDict = new Dictionary<string, DynamicTemplate>
        {
            ["KeywordsMetadata"] = keywordsTemplate
        };
        dynamicTemplates.Add(keywordsDict);

        // Assign dynamic templates to mapping
        var templateCollection = new Dictionary<string, DynamicTemplate>();
        foreach (var dict in dynamicTemplates)
        {
            foreach (var (key, value) in dict)
            {
                templateCollection[key] = value;
            }
        }
        mapping.DynamicTemplates = templateCollection;

        // Add explicit vector field mappings
        if (vectorFields != null)
        {
            foreach (var vectorConfig in vectorFields)
            {
                mapping.Properties[vectorConfig.FieldName] = CreateDenseVectorProperty(vectorConfig);
            }
        }

        return mapping;
    }

    /// <summary>
    /// Creates a DenseVectorProperty from a VectorFieldConfiguration.
    /// </summary>
    private static DenseVectorProperty CreateDenseVectorProperty(VectorFieldConfiguration config)
    {
        var property = new DenseVectorProperty
        {
            Dims = config.Dimensions,
            Index = config.Index,
            Similarity = config.Similarity switch
            {
                "cosine" => DenseVectorSimilarity.Cosine,
                "dot_product" => DenseVectorSimilarity.DotProduct,
                "l2_norm" => DenseVectorSimilarity.L2Norm,
                _ => DenseVectorSimilarity.Cosine
            }
        };

        if (config.Index)
        {
            property.IndexOptions = new DenseVectorIndexOptions
            {
                Type = config.IndexType switch
                {
                    "hnsw" => DenseVectorIndexOptionsType.Hnsw,
                    "int8_hnsw" => DenseVectorIndexOptionsType.Int8Hnsw,
                    "bbq_hnsw" => DenseVectorIndexOptionsType.BbqHnsw,
                    _ => DenseVectorIndexOptionsType.Int8Hnsw
                },
                M = config.M,
                EfConstruction = config.EfConstruction
            };
        }

        return property;
    }

    /// <summary>
    /// Creates index settings with the specified shard and replica configuration.
    /// </summary>
    /// <param name="shardNumber">The number of primary shards.</param>
    /// <param name="replicaNumber">The number of replica shards.</param>
    /// <returns>Configured index settings.</returns>
    public static IndexSettings GetIndexSettings(int shardNumber, int replicaNumber)
    {
        return new IndexSettings
        {
            NumberOfShards = shardNumber,
            NumberOfReplicas = replicaNumber
        };
    }
}
