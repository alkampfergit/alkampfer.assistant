using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Alkampfer.Assistant.Host.Components.Pages
{
    public partial class ConfigurationPage : ComponentBase
    {
        public List<string> Urls { get; set; } = new();
        public string NewUrl { get; set; } = string.Empty;

        public void AddUrl()
        {
            if (!string.IsNullOrWhiteSpace(NewUrl))
            {
                Urls.Add(NewUrl);
                NewUrl = string.Empty;
            }
        }

        public void RemoveUrl(string url)
        {
            Urls.Remove(url);
        }
    }
}
