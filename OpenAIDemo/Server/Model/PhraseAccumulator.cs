using OpenAI.Chat;

namespace OpenAIDemo.Server.Model
{
    public class PhraseAccumulator
    {
        string currentPhrase = string.Empty;
        string result = string.Empty;

        public bool HasItem { get; private set; }

        public AssistantChatMessage CurrentItem { get; private set; }

        public AssistantChatMessage Result => new AssistantChatMessage(result);

        public void Append(StreamingChatCompletionUpdate item)
        {
            this.HasItem = false;

            if (item.ContentUpdate == null || !item.ContentUpdate.Any())
            {
                return;
            }

            currentPhrase += item.ContentUpdate[0].Text;

            if (item.ContentUpdate[0].Text.Contains("\n"))
            {
                this.Flush();
            }
        }

        public void Flush()
        {
            if (!string.IsNullOrWhiteSpace(currentPhrase))
            {
                this.HasItem = true;
                this.CurrentItem = new AssistantChatMessage(currentPhrase);
            }

            result += currentPhrase;
            currentPhrase = string.Empty;
        }
    }
}
