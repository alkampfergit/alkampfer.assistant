using Alkampfer.Assistant.Core;

namespace Alkampfer.Assistant.Core.Configuration;

public class ModelConfiguration
{
    public required string ModelName { get; set; }
    public required string ModelDescription { get; set; }
}

public class ModelDefinition : BaseEntity
{
    public required string Url { get; set; }
    public required string ApiKey { get; set; } // Secret
    public List<ModelConfiguration> Models { get; set; } = new List<ModelConfiguration>();
}
