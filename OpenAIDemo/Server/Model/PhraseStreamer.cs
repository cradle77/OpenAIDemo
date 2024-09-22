using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Chat;

namespace OpenAIDemo.Server.Model
{
    public class PhraseStreamer
    {
        private IAsyncEnumerable<StreamingChatMessageContent> _sourceStream;

        public PhraseStreamer(IAsyncEnumerable<StreamingChatMessageContent> sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public string Result { get; private set; }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetPhrases(CancellationToken cancellationToken)
        {
            string currentPhrase = string.Empty;
            string result = string.Empty;

            await foreach (var item in _sourceStream.WithCancellation(cancellationToken))
            {
                currentPhrase += item.Content;

                if (string.IsNullOrEmpty(item.Content) || item.Content.Contains("\n"))
                {
                    if (!string.IsNullOrWhiteSpace(currentPhrase))
                    {
                        yield return new StreamingChatMessageContent(AuthorRole.Assistant, currentPhrase);
                    }

                    result += currentPhrase;
                    currentPhrase = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(currentPhrase))
            {
                result += currentPhrase;
                yield return new StreamingChatMessageContent(AuthorRole.Assistant, currentPhrase);
            }

            this.Result = result;
        }
    }
}
