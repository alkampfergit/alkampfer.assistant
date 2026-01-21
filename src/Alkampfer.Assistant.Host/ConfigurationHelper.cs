using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Alkampfer.Assistant.Host;

public static class ConfigurationHelper
{
    /// <summary>
    /// Searches for 'alkampfer.assistant.json' in the current and parent directories,
    /// stops at the first found, and adds it to the configuration builder if found.
    /// This override file takes precedence over appsettings.json but is overridden by environment variables.
    /// </summary>
    /// <remarks>
    /// Configuration priority (highest to lowest):
    /// 1. Environment variables with ALKASS_ prefix
    /// 2. alkampfer.assistant.json (this file)
    /// 3. appsettings.json
    /// </remarks>
    public static void AddOverrideConfiguration(IConfigurationBuilder builder, string? startDirectory = null)
    {
        var dir = startDirectory ?? Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(dir))
        {
            var configPath = Path.Combine(dir, "alkampfer.assistant.json");
            if (File.Exists(configPath))
            {
                builder.AddJsonFile(configPath, optional: false, reloadOnChange: true);
                Console.WriteLine($"Loaded configuration override from: {configPath}");
                break;
            }
            var parent = Directory.GetParent(dir);
            dir = parent?.FullName;
        }
    }
}
