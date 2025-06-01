using System.Collections.Generic;

namespace Alkampfer.Assistant.Host.Components.Pages
{
    public class ModelConfigurationDto
    {
        public string? ModelName { get; set; }
        public string? ModelDescription { get; set; }
    }

    public class ModelDefinitionDto
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        public string? ApiKey { get; set; }
        public List<ModelConfigurationDto> Models { get; set; } = new List<ModelConfigurationDto>();
    }
}
