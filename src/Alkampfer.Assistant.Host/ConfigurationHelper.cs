using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace Alkampfer.Assistant.Host
{
    public static class ConfigurationHelper
    {
        public static void ConfigureLogging()
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "Logs", "app-.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 10,
                    fileSizeLimitBytes: 20 * 1024 * 1024) // 20MB
                .CreateLogger();
        }
    }
}
