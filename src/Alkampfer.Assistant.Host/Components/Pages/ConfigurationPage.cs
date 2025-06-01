using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.Configuration;
using Alkampfer.Assistant.Core.LiteDbIntegration;
using Alkampfer.Assistant.Host.Components.Pages;
using MudBlazor;
using Alkampfer.Assistant.Core;

namespace Alkampfer.Assistant.Host.Components.Pages
{
    public partial class ConfigurationPage : ComponentBase
    {
        [Inject] public IRepository<ModelDefinition> Repository { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;

        public List<ModelDefinitionDto> ModelDefinitions { get; set; } = new();
        public ModelDefinitionDto NewModelDefinition { get; set; } = new();
        public ModelConfigurationDto NewModelConfig { get; set; } = new();
        public string? EditingModelId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            // TODO: Load all ModelDefinitions from repository (implement a method for all)
            // For now, just clear
            ModelDefinitions.Clear();
        }

        public void StartAddModelConfig(string modelDefId)
        {
            EditingModelId = modelDefId;
            NewModelConfig = new ModelConfigurationDto();
        }

        public void AddModelConfig()
        {
            var modelDef = ModelDefinitions.Find(m => m.Id == EditingModelId);
            if (modelDef != null && !string.IsNullOrWhiteSpace(NewModelConfig.ModelName))
            {
                modelDef.Models.Add(NewModelConfig);
                Snackbar.Add($"Model '{NewModelConfig.ModelName}' added.", Severity.Success);
                NewModelConfig = new ModelConfigurationDto();
            }
            else
            {
                Snackbar.Add("Model name is required.", Severity.Error);
            }
        }

        public async Task AddModelDefinitionAsync()
        {
            if (string.IsNullOrWhiteSpace(NewModelDefinition.Url) || string.IsNullOrWhiteSpace(NewModelDefinition.ApiKey))
            {
                Snackbar.Add("URL and API Key are required.", Severity.Error);
                return;
            }
            NewModelDefinition.Id = Guid.NewGuid().ToString();
            ModelDefinitions.Add(NewModelDefinition);
            // Save to repository
            var entity = new ModelDefinition
            {
                Id = NewModelDefinition.Id,
                Url = NewModelDefinition.Url!,
                ApiKey = NewModelDefinition.ApiKey!,
                Models = NewModelDefinition.Models.ConvertAll(m => new ModelConfiguration
                {
                    ModelName = m.ModelName!,
                    ModelDescription = m.ModelDescription!
                })
            };
            await Repository.SaveAsync(entity);
            Snackbar.Add("Model definition added.", Severity.Success);
            NewModelDefinition = new ModelDefinitionDto();
        }

        public async Task RemoveModelDefinitionAsync(string id)
        {
            var modelDef = ModelDefinitions.Find(m => m.Id == id);
            if (modelDef != null)
            {
                ModelDefinitions.Remove(modelDef);
                // TODO: Remove from repository (implement a delete method)
                Snackbar.Add("Model definition removed.", Severity.Success);
            }
        }
    }
}
