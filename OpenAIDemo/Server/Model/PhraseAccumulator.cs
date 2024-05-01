using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.Model
{
    public class PhraseAccumulator
    {
        string currentPhrase = string.Empty;
        string result = string.Empty;

        public bool HasItem { get; private set; }

        public ChatRequestAssistantMessage CurrentItem { get; private set; }

        public ChatRequestAssistantMessage Result => new ChatRequestAssistantMessage(result);

        public void Append(StreamingChatCompletionsUpdate item)
        {
            if (item.ContentUpdate == null)
            {
                return;
            }

            currentPhrase += item.ContentUpdate;

            if (item.ContentUpdate.Contains("\n"))
            {
                this.Flush();
            }
        }

        public void Flush()
        {
            if (!string.IsNullOrWhiteSpace(currentPhrase))
            {
                this.HasItem = true;
                this.CurrentItem = new ChatRequestAssistantMessage(currentPhrase);
            }

            result += currentPhrase;
            currentPhrase = string.Empty;
        }
    }
}
