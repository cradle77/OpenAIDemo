using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.Model
{
    internal class NullAccumulator : IAccumulator
    {
        public bool HasItem => false;

        public ChatRequestAssistantMessage CurrentItem => null;

        public ChatRequestAssistantMessage Result => null;

        public void Append(StreamingChatCompletionsUpdate update)
        {
        }

        public void Flush()
        {
        }
    }
}
