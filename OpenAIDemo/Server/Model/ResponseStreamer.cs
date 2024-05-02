using Azure.AI.OpenAI;
using System.Text;

namespace OpenAIDemo.Server.Model
{
    public class ResponseStreamer
    {
        private IAsyncEnumerable<StreamingChatCompletionsUpdate> _sourceStream;

        public ResponseStreamer(IAsyncEnumerable<StreamingChatCompletionsUpdate> sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public ChatRequestAssistantMessage Result { get; private set; }
        public CompletionsFinishReason? FinishReason { get; internal set; }

        public async IAsyncEnumerable<ChatRequestAssistantMessage> GetPhrases(CancellationToken cancellationToken)
        {
            FunctionAccumulator functionAccumulator = new();
            PhraseAccumulator phraseAccumulator = new();

            await foreach (var item in _sourceStream.WithCancellation(cancellationToken))
            {
                functionAccumulator.Append(item);
                phraseAccumulator.Append(item);

                if (phraseAccumulator.HasItem) // auto flushed
                {
                    yield return phraseAccumulator.CurrentItem;
                }

                if (item.FinishReason != null)
                {
                    this.FinishReason = item.FinishReason;
                }
            }

            phraseAccumulator.Flush();
            functionAccumulator.Flush();
            
            if (phraseAccumulator.HasItem)
            {
                yield return phraseAccumulator.CurrentItem;
            }

            if (functionAccumulator.HasItem)
            {
                yield return functionAccumulator.CurrentItem;
            }

            this.Result = phraseAccumulator.Result;
        }
    }
}
