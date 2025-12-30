---
applyTo: '**/*.openai.cs'
---
This file contains code that interact with Azure OpenAi or OpenAi. The preferred way to interact with the APIs is through the new Response Model, preferrably in streaming if possible.

If you need to set verbosity level of the response you need to use a peculiar approach.

```csharp
var options = new ResponseCreationOptions();
var optionsJsonModel = ((IJsonModel<ResponseCreationOptions>) options;
        
options = optionsJsonModel.Create(BinaryData.FromObjectAsJson(new
{
    reasoning = new { effort = "minimal" },
    text = new { verbosity = "low" },
}), 
ModelReaderWriterOptions.Json);

```

Then continue to populate all the properties that are available in the ResponseCreationOptions class.