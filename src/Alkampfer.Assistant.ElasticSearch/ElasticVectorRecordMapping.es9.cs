using System.Collections.Generic;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Provides mapping configuration for VectorRecord in Elasticsearch.
/// </summary>
public static class ElasticVectorRecordMapping
{
    /// <summary>
    /// Creates the type mapping for VectorRecord with dynamic templates for prefixed metadata fields.
    /// </summary>
    /// <returns>A configured TypeMapping for VectorRecord.</returns>
    public static TypeMapping GetTypeMapping()
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

        return mapping;
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
