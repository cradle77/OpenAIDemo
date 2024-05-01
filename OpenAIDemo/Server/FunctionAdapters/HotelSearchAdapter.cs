using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using OpenAIDemo.Server.Model;
using System.Text.Json;

namespace OpenAIDemo.Server.FunctionAdapters
{
    public class HotelQuery
    {
        public string QueryText { get; set; }
        public int ResultCount { get; set; } = 3;
    }

    public class HotelResult
    {
        public string Name { get; set; }
        public int ReviewRating { get; set; }
        public string ReviewText { get; set; }
    }

    public class HotelSearchAdapter : IFunctionAdapter
    {
        private Uri _searchUrl;
        private string _indexName;
        private AzureKeyCredential _searchCredential;
        private string _openAiEndpoint;
        private string _openAiKey;
        private string _engine;
        private AzureConfig _config;

        public string FunctionName => "hotels-search";

        public HotelSearchAdapter(IOptions<AzureConfig> config)
        {
            _config = config.Value;

            _searchUrl = new Uri(_config.Search.SearchUrl);
            _indexName = _config.Search.IndexName;
            _searchCredential = new AzureKeyCredential(_config.Search.SearchKey);
            _openAiEndpoint = _config.OpenAi.OpenAiEndpoint;
            _openAiKey = _config.OpenAi.OpenAiKey;
            _engine = _config.OpenAi.EmbedEngine;
        }

        public ChatCompletionsFunctionToolDefinition GetFunctionDefinition()
        {
            return new ChatCompletionsFunctionToolDefinition()
            {
                Name = this.FunctionName,
                Description = "This function queries a search engine for hotel reviews. It accepts a full text query string.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        QueryText = new
                        {
                            Type = "string",
                            Description = "The text of the query for the hotels search engine",
                        },
                        ResultCount = new
                        {
                            Type = "number",
                            Description = "The number of results you want to get. Default is 3."
                        }
                    },
                    Required = new[] { "QueryText" },
                }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }

        public async Task<ChatRequestToolMessage> InvokeAsync(string id, string arguments)
        {
            var query = JsonSerializer.Deserialize<HotelQuery>(arguments, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var searchClient = new SearchClient(_searchUrl, _indexName, _searchCredential);
            OpenAIClient openAiclient = new(new Uri(_openAiEndpoint), new AzureKeyCredential(_openAiKey));

            var openAiResponse = await openAiclient.GetEmbeddingsAsync(
                new EmbeddingsOptions(_engine, new[] { query.QueryText }));

            var queryEmbeddings = openAiResponse.Value.Data[0].Embedding;

            // searching the index for the closest embeddings
            var searchOptions = new SearchOptions
            {
                Vectors = { new() { Value = queryEmbeddings.ToArray(), KNearestNeighborsCount = query.ResultCount, Fields = { "reviewEmbeddings" } } },
                Size = query.ResultCount,
                Select = { "hotelName", "rating", "reviewText" },
            };

            SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>(null, searchOptions);

            var results = await response.GetResultsAsync()
                .Select(x => new HotelResult()
                {
                    Name = x.Document["hotelName"] as string,
                    ReviewRating = (int)x.Document["rating"],
                    ReviewText = x.Document["reviewText"] as string
                }).ToListAsync();

            return new ChatRequestToolMessage(
                JsonSerializer.Serialize(results, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                , id);
        }
    }
}
