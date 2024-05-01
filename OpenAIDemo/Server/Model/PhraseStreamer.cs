using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.Model
{
    public class PhraseStreamer
    {
        private IAsyncEnumerable<StreamingChatCompletionsUpdate> _sourceStream;

        public PhraseStreamer(IAsyncEnumerable<StreamingChatCompletionsUpdate> sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public ChatRequestAssistantMessage Result { get; private set; }

        public async IAsyncEnumerable<ChatRequestAssistantMessage> GetPhrases(CancellationToken cancellationToken)
        {
            string currentPhrase = string.Empty;
            string result = string.Empty;

            await foreach (var item in _sourceStream.WithCancellation(cancellationToken))
            {
                currentPhrase += item.ContentUpdate;

                if (string.IsNullOrEmpty(item.ContentUpdate) || item.ContentUpdate.Contains("\n"))
                {
                    if (!string.IsNullOrWhiteSpace(currentPhrase))
                    {
                        yield return new ChatRequestAssistantMessage(currentPhrase);
                    }

                    result += currentPhrase;
                    currentPhrase = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(currentPhrase))
            {
                result += currentPhrase;
                yield return new ChatRequestAssistantMessage(currentPhrase);
            }

            this.Result = new ChatRequestAssistantMessage(result);
        }
    }
}
