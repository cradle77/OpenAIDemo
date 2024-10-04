#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using EmbeddingsGenerator;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using System.Text.Json;

namespace RealtimeDemo.Functions
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

        public HotelSearchAdapter(AzureConfig config)
        {
            _config = config;

            _searchUrl = new Uri(_config.Search.SearchUrl);
            _indexName = _config.Search.IndexName;
            _searchCredential = new AzureKeyCredential(_config.Search.SearchKey);
            _openAiEndpoint = _config.OpenAi.OpenAiEndpoint;
            _openAiKey = _config.OpenAi.OpenAiKey;
            _engine = _config.OpenAi.EmbedEngine;
        }

        public ConversationFunctionTool GetFunctionDefinition()
        {
            return new ConversationFunctionTool()
            {
                Name = this.FunctionName,
                Description = "This function queries a search engine for hotel reviews. It accepts a full text query string. If the query text is not in English, you need to translate it to English before using it.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        QueryText = new
                        {
                            Type = "string",
                            Description = "The text of the query for the hotels search engine. If the query text is not in English, you need to translate it to English before using it.",
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

        // ...

        public async Task<string> InvokeAsync(string id, string arguments)
        {
            var query = JsonSerializer.Deserialize<HotelQuery>(arguments, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var searchClient = new SearchClient(_searchUrl, _indexName, _searchCredential);

            AzureOpenAIClient openAiclient = new(new Uri(_openAiEndpoint), new ApiKeyCredential(_openAiKey));

            var embeddingClient = openAiclient.GetEmbeddingClient(_engine);

            var openAiResponse = await embeddingClient.GenerateEmbeddingsAsync(new[] { query.QueryText });

            var queryEmbeddings = openAiResponse.Value[0].ToFloats();

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

            return JsonSerializer.Serialize(results);
        }
    }
}
