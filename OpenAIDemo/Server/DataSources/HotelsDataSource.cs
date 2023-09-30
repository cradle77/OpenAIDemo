using Azure.AI.OpenAI;
using Azure;
using OpenAIDemo.Server.Model;
using Microsoft.Extensions.Options;

namespace OpenAIDemo.Server.DataSources
{
    internal class HotelsDataSource : IOpenAIDataSource
    {
        private AzureConfig _config;

        public HotelsDataSource(IOptions<AzureConfig> config)
        {
            _config = config.Value;
        }

        public AzureChatExtensionConfiguration GetDataSource()
        {
            return new AzureCognitiveSearchChatExtensionConfiguration()
            {
                SearchEndpoint = new Uri(_config.Search.SearchUrl),
                SearchKey = new AzureKeyCredential(_config.Search.SearchKey),
                IndexName = "embed-idx",
                SemanticConfiguration = "test-semantic",
                QueryType = AzureCognitiveSearchQueryType.Semantic,
                ShouldRestrictResultScope = false
            };
        }
    }
}
