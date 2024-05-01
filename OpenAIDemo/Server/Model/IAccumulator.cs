using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.Model
{
    public interface IAccumulator
    {
        bool HasItem { get; }
        ChatRequestAssistantMessage CurrentItem { get; }
        ChatRequestAssistantMessage Result { get; }

        void Append(StreamingChatCompletionsUpdate item);
        void Flush();
    }
}
