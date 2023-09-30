using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.DataSources
{
    public interface IOpenAIDataSource
    {
        AzureChatExtensionConfiguration GetDataSource();
    }
}
