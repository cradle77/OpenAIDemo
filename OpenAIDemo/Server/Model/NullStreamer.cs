using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.Model
{
    public class NullStreamer
    {
        private IAsyncEnumerable<ChatMessage> _sourceStream;

        public NullStreamer(IAsyncEnumerable<ChatMessage> sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public ChatMessage Result { get; private set; }

        public async IAsyncEnumerable<ChatMessage> GetPhrases(CancellationToken cancellationToken)
        {
            this.Result = new ChatMessage();

            await foreach (var item in _sourceStream.WithCancellation(cancellationToken))
            {
                if (item.Role != string.Empty)
                {
                    this.Result.Role = item.Role;
                    this.Result.Name = item.Name;
                }

                this.Result.Content += item.Content;

                yield return item;
            }
        }
    }
}
