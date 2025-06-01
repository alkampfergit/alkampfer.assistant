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

        public List<ModelDefinition> ModelDefinitions { get; set; } = new();
        public ModelDefinition NewModelDefinition { get; set; } = new() { Url = "", ApiKey = "" };
        public ModelConfiguration NewModelConfig { get; set; } = new() { ModelName = "", ModelDescription = "" };
        public string? EditingModelId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadModelDefinitionsAsync();
        }

        private async Task LoadModelDefinitionsAsync()
        {
            try
            {
                ModelDefinitions = Repository.AsQueryable.ToList();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error loading model definitions: {ex.Message}", Severity.Error);
                ModelDefinitions.Clear();
            }
        }

        public void StartAddModelConfig(string modelDefId)
        {
            EditingModelId = modelDefId;
            NewModelConfig = new ModelConfiguration { ModelName = "", ModelDescription = "" };
        }

        public async Task AddModelConfigAsync()
        {
            var modelDef = ModelDefinitions.Find(m => m.Id == EditingModelId);
            if (modelDef != null && !string.IsNullOrWhiteSpace(NewModelConfig.ModelName))
            {
                modelDef.Models.Add(NewModelConfig);
                try
                {
                    await Repository.SaveAsync(modelDef);
                    Snackbar.Add($"Model '{NewModelConfig.ModelName}' added.", Severity.Success);
                    NewModelConfig = new ModelConfiguration { ModelName = "", ModelDescription = "" };
                    EditingModelId = null;
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error saving model configuration: {ex.Message}", Severity.Error);
                }
            }
            else
            {
                Snackbar.Add("Model name is required.", Severity.Error);
            }
        }

        public void CancelAddModelConfig()
        {
            EditingModelId = null;
            NewModelConfig = new ModelConfiguration { ModelName = "", ModelDescription = "" };
        }

        public async Task RemoveModelConfigAsync(string modelDefId, int modelIndex)
        {
            var modelDef = ModelDefinitions.Find(m => m.Id == modelDefId);
            if (modelDef != null && modelIndex >= 0 && modelIndex < modelDef.Models.Count)
            {
                var modelName = modelDef.Models[modelIndex].ModelName;
                modelDef.Models.RemoveAt(modelIndex);
                try
                {
                    await Repository.SaveAsync(modelDef);
                    Snackbar.Add($"Model '{modelName}' removed.", Severity.Success);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error removing model configuration: {ex.Message}", Severity.Error);
                }
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
            try
            {
                await Repository.SaveAsync(NewModelDefinition);
                ModelDefinitions.Add(NewModelDefinition);
                Snackbar.Add("Model definition added.", Severity.Success);
                NewModelDefinition = new ModelDefinition { Url = "", ApiKey = "" };
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error saving model definition: {ex.Message}", Severity.Error);
            }
        }

        public async Task RemoveModelDefinitionAsync(string id)
        {
            var modelDef = ModelDefinitions.Find(m => m.Id == id);
            if (modelDef != null)
            {
                try
                {
                    await Repository.DeleteAsync(id);
                    ModelDefinitions.Remove(modelDef);
                    Snackbar.Add("Model definition removed.", Severity.Success);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error removing model definition: {ex.Message}", Severity.Error);
                }
            }
        }
    }
}
