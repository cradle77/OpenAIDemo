using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.Model
{
    public class PhraseStreamer
    {
        private IAsyncEnumerable<ChatMessage> _sourceStream;

        public PhraseStreamer(IAsyncEnumerable<ChatMessage> sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public ChatMessage Result { get; private set; }

        public async IAsyncEnumerable<ChatMessage> GetPhrases(CancellationToken cancellationToken)
        {
            var message = new ChatMessage();
            this.Result = new ChatMessage();

            await foreach (var item in _sourceStream.WithCancellation(cancellationToken))
            {
                if (item.Role != string.Empty)
                {
                    this.Result.Role = item.Role;
                    this.Result.Name = item.Name;
                }

                message.Role = item.Role;
                message.Name = item.Name;
                message.Content += item.Content;

                if (string.IsNullOrEmpty(item.Content) || item.Content.Contains("\n"))
                {
                    if (!string.IsNullOrWhiteSpace(message.Content))
                    {
                        yield return message;
                    }

                    this.Result.Content += message.Content;
                    message = new ChatMessage();
                }
            }

            if (!string.IsNullOrEmpty(message.Name))
            {
                this.Result.Content += message.Content;
                yield return message;
            }
        }
    }
}
