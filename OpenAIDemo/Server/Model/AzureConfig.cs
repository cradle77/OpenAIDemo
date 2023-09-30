namespace OpenAIDemo.Server.Model
{
    public class AzureConfig
    {
        public string TenantId { get; set; }

        public OpenAiConfig OpenAi { get; set; }

        public SearchConfig Search { get; set; }

        public SpeechConfig Speech { get; set; }

        public AdlsConfig Adls { get; set; }

        public SynapseConfig Synapse { get; set; }
    }

    public class AdlsConfig 
    {
        public string ContainerName { get; set; }
        public string StorageEndpoint { get; set; }
        public string AccountName { get; set; }
    }

    public class OpenAiConfig
    {
        public string OpenAiEndpoint { get; set; }
        public string OpenAiKey { get; set; }
        public string EmbedEngine { get; set; }
        public string ChatEngine { get; set; }
    }

    public class SearchConfig
    {
        public string SearchUrl { get; set; }
        public string IndexName { get; set; }
        public string SearchKey { get; set; }
    }

    public class SpeechConfig
    {
        public string SpeechKey { get; set; }
        public string SpeechRegion { get; set; }
    }

    public class SynapseConfig
    {
        public string DbConnectionString { get; set; }
    }
}
