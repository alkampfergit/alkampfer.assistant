## How to init an index

This code ensures that a specific index exists.

    internal async Task EnsureIndexAsync(
        string indexName,
        int vectorDimension,
        CancellationToken cancellationToken)
    {
        // step1: verify if the index exists
        var exists = await _client.Indices.ExistsAsync(indexName, cancellationToken);
        if (!exists.Exists)
        {
            // index does not exists we neeed to create.
            var createIdxResponse = await _client.Indices.CreateAsync(indexName,
               cfg =>
               {
                   cfg.Settings(settings =>
                   {
                       settings.NumberOfShards(_kernelMemoryElasticSearchConfig.ShardNumber);
                       settings.NumberOfReplicas(_kernelMemoryElasticSearchConfig.ReplicaCount);

                       settings.Analysis(analysis =>
                       {
                           analysis.Analyzers(analyzer =>
                           {
                               analyzer.Custom("nalc", custom => custom
                                    .Tokenizer("keyword")
                                    .Filter(["lowercase"]));
                           });
                       });
                   });

                   cfg.Mappings(mappings =>
                   {
                       mappings.Properties<object>(pm =>
                       {
                           pm.DenseVector("vector", dv => dv.Dims(vectorDimension));
                           pm.Text("payload", pd => pd.Index(false));
                       })
                       .DynamicTemplates(GetDynamicTemplates());
                   });
               },
               cancellationToken).ConfigureAwait(false);

            if (!createIdxResponse.IsValidResponse)
            {
                throw new Exception($"Failed to create index {indexName} - {createIdxResponse.GetErrorFromElasticResponse()}");
            }

            CreatedIndices.Add(indexName);
        }
    }


     private ICollection<IDictionary<string, DynamicTemplate>> GetDynamicTemplates()
 {
     var dt = new List<IDictionary<string, DynamicTemplate>>();
     var tags = new Dictionary<string, DynamicTemplate>();
     var tagsDynamicMapping = DynamicTemplate.Mapping(new TextProperty
     {
         Analyzer = "standard",
         Index = true,
         Store = true,
         Fields = new Properties() {
                 { "keyword", new KeywordProperty() },
                 //{ "na", new TextProperty()
                 //    {
                 //        Analyzer = "nalc"
                 //    }
                 //},
                 //{ "english", new TextProperty()
                 //    {
                 //        Analyzer = "english"
                 //    }
                 //}
             }
     });
     tagsDynamicMapping.Match = ["tag_*"];
     tags["tags"] = tagsDynamicMapping;
     dt.Add(tags);

     var txtProp = new Dictionary<string, DynamicTemplate>();
     var txtDynamicMapping = DynamicTemplate.Mapping(new TextProperty
     {
         Analyzer = "standard",
         Index = true,
         Store = true,
         Fields = new Properties() {
                 //{ "keyword", new KeywordProperty() },
                 { "english", new TextProperty() {
                         Analyzer = "english"
                     }
                 }
             }
     });
     txtDynamicMapping.Match = ["txt_*"];
     txtProp["txt"] = txtDynamicMapping;
     dt.Add(txtProp);
     return dt;
 }
