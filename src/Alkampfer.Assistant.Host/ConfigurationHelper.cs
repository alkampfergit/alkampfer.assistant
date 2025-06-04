using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Alkampfer.Assistant.Host;

public static class ConfigurationHelper
{
    /// <summary>
    /// Searches for 'alkampfer.assistant.json' in the current and parent directories,
    /// stops at the first found, and adds it to the configuration builder if found.
    /// </summary>
    public static void AddOverrideConfiguration(IConfigurationBuilder builder, string? startDirectory = null)
    {
        var dir = startDirectory ?? Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(dir))
        {
            var configPath = Path.Combine(dir, "alkampfer.assistant.json");
            if (File.Exists(configPath))
            {
                builder.AddJsonFile(configPath, optional: false, reloadOnChange: true);
                break;
            }
            var parent = Directory.GetParent(dir);
            dir = parent?.FullName;
        }
    }
}
