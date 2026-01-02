using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Extension methods for serializing and deserializing VectorRecord to/from Elasticsearch.
/// </summary>
public static class VectorRecordExtensions
{
    /// <summary>
    /// Converts a VectorRecord to an ExpandoObject for Elasticsearch indexing.
    /// Metadata fields are prefixed according to their type:
    /// - s_ for strings
    /// - i_ for integers
    /// - n_ for numeric (double)
    /// - b_ for boolean
    /// - d_ for DateTime (stored as ISO8601 string)
    /// - k_ for keywords (string array, lowercase normalized)
    /// </summary>
    /// <param name="record">The VectorRecord to convert.</param>
    /// <returns>An IDictionary representing the record for Elasticsearch indexing.</returns>
    public static IDictionary<string, object> ToExpandoObjectForIndexing(this VectorRecord record)
    {
        var obj = new ExpandoObject() as IDictionary<string, object>;

        // Core fields
        obj["id"] = record.Id;
        obj["documentId"] = record.DocumentId;

        // Text field (optional)
        if (record.Text != null)
        {
            obj["text"] = record.Text;
        }

        // Metadata with type-based prefixes
        foreach (var (key, value) in record.Metadata)
        {
            var stringValue = value.AsString();
            if (stringValue != null)
            {
                obj[$"s_{key}"] = stringValue;
                continue;
            }

            var intValue = value.AsInt();
            if (intValue.HasValue)
            {
                obj[$"i_{key}"] = intValue.Value;
                continue;
            }

            var doubleValue = value.AsDouble();
            if (doubleValue.HasValue)
            {
                obj[$"n_{key}"] = doubleValue.Value;
                continue;
            }

            var boolValue = value.AsBool();
            if (boolValue.HasValue)
            {
                obj[$"b_{key}"] = boolValue.Value;
                continue;
            }

            var dateValue = value.AsDateTime();
            if (dateValue.HasValue)
            {
                // Store DateTime as ISO8601 string
                obj[$"d_{key}"] = dateValue.Value.ToString("O", CultureInfo.InvariantCulture);
                continue;
            }

            var keywordsValue = value.AsKeywords();
            if (keywordsValue != null)
            {
                obj[$"k_{key}"] = keywordsValue;
                continue;
            }
        }

        // Note: Vector fields are not included yet (future enhancement)

        return obj;
    }

    /// <summary>
    /// Creates a VectorRecord from a JsonElement retrieved from Elasticsearch.
    /// </summary>
    /// <param name="source">The JsonElement containing the indexed document.</param>
    /// <returns>A reconstructed VectorRecord.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
    public static VectorRecord FromJsonElement(JsonElement source)
    {
        // Extract core fields
        if (!source.TryGetProperty("id", out var idProp))
        {
            throw new InvalidOperationException("Missing required 'id' field in Elasticsearch document.");
        }

        if (!source.TryGetProperty("documentId", out var docIdProp))
        {
            throw new InvalidOperationException("Missing required 'documentId' field in Elasticsearch document.");
        }

        var id = idProp.GetString() ?? throw new InvalidOperationException("'id' field cannot be null.");
        var documentId = docIdProp.GetString() ?? throw new InvalidOperationException("'documentId' field cannot be null.");

        var record = VectorRecord.Create(id, documentId);

        // Extract optional text field
        if (source.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
        {
            var text = textProp.GetString();
            if (text != null)
            {
                record.WithText(text);
            }
        }

        // Parse prefixed metadata fields
        foreach (var property in source.EnumerateObject())
        {
            var fieldName = property.Name;

            // Skip core fields
            if (fieldName == "id" || fieldName == "documentId" || fieldName == "text")
            {
                continue;
            }

            // Parse prefixed fields
            if (fieldName.StartsWith("s_") && fieldName.Length > 2)
            {
                var key = fieldName[2..];
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString();
                    if (value != null)
                    {
                        record.WithMetadata(key, value);
                    }
                }
            }
            else if (fieldName.StartsWith("i_") && fieldName.Length > 2)
            {
                var key = fieldName[2..];
                if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var intValue))
                {
                    record.WithMetadata(key, intValue);
                }
            }
            else if (fieldName.StartsWith("n_") && fieldName.Length > 2)
            {
                var key = fieldName[2..];
                if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDouble(out var doubleValue))
                {
                    record.WithMetadata(key, doubleValue);
                }
            }
            else if (fieldName.StartsWith("b_") && fieldName.Length > 2)
            {
                var key = fieldName[2..];
                if (property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False)
                {
                    record.WithMetadata(key, property.Value.GetBoolean());
                }
            }
            else if (fieldName.StartsWith("d_") && fieldName.Length > 2)
            {
                var key = fieldName[2..];
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var dateString = property.Value.GetString();
                    if (dateString != null && DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateValue))
                    {
                        record.WithMetadata(key, dateValue);
                    }
                }
            }
            else if (fieldName.StartsWith("k_") && fieldName.Length > 2)
            {
                var key = fieldName[2..];
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    var keywords = new List<string>();
                    foreach (var element in property.Value.EnumerateArray())
                    {
                        if (element.ValueKind == JsonValueKind.String)
                        {
                            var keyword = element.GetString();
                            if (keyword != null)
                            {
                                keywords.Add(keyword);
                            }
                        }
                    }
                    if (keywords.Count > 0)
                    {
                        record.WithMetadata(key, keywords.ToArray());
                    }
                }
            }
            // Note: Vector fields will be handled in future enhancement
        }

        return record;
    }
}
