using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.Model
{
    public static class ChatRequestMessageExtensions
    {
        public static string GetContent(this ChatRequestMessage message)
        {
            return message switch
            {
                ChatRequestSystemMessage systemMessage => systemMessage.Content,
                ChatRequestUserMessage userMessage => userMessage.Content,
                ChatRequestAssistantMessage assistantMessage => assistantMessage.Content,
                _ => throw new InvalidOperationException("Unknown message type.")
            };
        }
    }
}
