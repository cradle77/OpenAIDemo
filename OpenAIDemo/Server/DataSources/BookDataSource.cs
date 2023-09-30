using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAIDemo.Server.Model;

namespace OpenAIDemo.Server.DataSources
{
    internal class BookDataSource : IOpenAIDataSource
    {
        private AzureConfig _config;

        public BookDataSource(IOptions<AzureConfig> config)
        {
            _config = config.Value;
        }

        public AzureChatExtensionConfiguration GetDataSource()
        {
            return new AzureCognitiveSearchChatExtensionConfiguration()
            {
                SearchEndpoint = new Uri(_config.Search.SearchUrl),
                SearchKey = new AzureKeyCredential(_config.Search.SearchKey),
                IndexName = "grounding-demo",
                SemanticConfiguration = "default",
                QueryType = AzureCognitiveSearchQueryType.Semantic,
                ShouldRestrictResultScope = true
            };
        }
    }
}
