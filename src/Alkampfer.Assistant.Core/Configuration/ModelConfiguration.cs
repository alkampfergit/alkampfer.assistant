using Alkampfer.Assistant.Core;

namespace Alkampfer.Assistant.Core.Configuration;

public class ModelConfiguration
{
    public string ModelName { get; set; }
    public string ModelDescription { get; set; }
}

public class ModelDefinition : BaseEntity
{
    public string Url { get; set; }
    public string ApiKey { get; set; } // Secret
    public List<ModelConfiguration> Models { get; set; } = new List<ModelConfiguration>();
}
