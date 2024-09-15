using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace OpenAIDemo.Server.Model
{
    public static class ChatMessageExtensions
    {
        public static ChatMessageRole GetRole(this ChatMessage message)
        {
            return message switch
            {
                SystemChatMessage systemMessage => ChatMessageRole.System,
                UserChatMessage userMessage => ChatMessageRole.User,
                AssistantChatMessage assistantMessage => ChatMessageRole.Assistant,
                ToolChatMessage toolMessage => ChatMessageRole.Tool,
                FunctionChatMessage functionMessage => ChatMessageRole.Function,
                _ => throw new InvalidOperationException("Unknown message type.")
            };
        }

        public static string GetContent(this ChatMessage message)
        {
            return message
                .Content
                .Select(x => x.Text)
                .FirstOrDefault();
        }
    }
}
