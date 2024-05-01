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
                ChatRequestToolMessage toolMessage => toolMessage.Content,
                _ => throw new InvalidOperationException("Unknown message type.")
            };
        }

        public static void AddRange<T>(this IList<T> source, IEnumerable<T> items)
        { 
            foreach (var item in items)
            {
                source.Add(item);
            }
        }
    }
}
