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
                ChatRequestUserMessage userMessage => userMessage.GetContent(),
                ChatRequestAssistantMessage assistantMessage => assistantMessage.Content,
                _ => throw new InvalidOperationException("Unknown message type.")
            };
        }

        public static string GetContent(this ChatRequestUserMessage message)
        {
            return message.Content ?? string.Join(" ", message.MultimodalContentItems.Select(x => x.GetContent()));
        }

        public static string GetContent(this ChatMessageContentItem item)
        {
            return item switch
            {
                ChatMessageTextContentItem textItem => textItem.Text,
                ChatMessageImageContentItem imageItem => imageItem.ImageUrl.Url.ToString(),
                _ => throw new InvalidOperationException("Unknown content item type.")
            };
        }
    }
}
