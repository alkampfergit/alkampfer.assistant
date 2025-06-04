using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Net.Http;

namespace Alkampfer.Assistant.Core.SemanticKernel
{
    public static class SemanticKernelConfigurator
    {
        public static IKernelBuilder ConfigureKernel(
            string endpointUrl,
            string modelName,
            string apiKey)
        {
            var kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.Services.AddLogging(loggingBuilder =>
            {
                //clear existing providers and add only serilog.
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: false);
            });

            // Create HttpClient with extended timeout
            var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

            // Add OpenAI chat completion with custom endpoint and HttpClient
#pragma warning disable SKEXP0010
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: modelName,
                apiKey: apiKey,
                endpoint: new Uri(endpointUrl),
                httpClient: httpClient
            );
#pragma warning restore SKEXP0010

            return kernelBuilder;
        }
    }
}
