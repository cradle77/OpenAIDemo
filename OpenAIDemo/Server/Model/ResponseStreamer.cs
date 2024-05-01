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
            IAccumulator accumulator = new NullAccumulator();

            await foreach (var item in _sourceStream.WithCancellation(cancellationToken))
            {
                if (accumulator is NullAccumulator)
                {
                    if (item.ToolCallUpdate is StreamingFunctionToolCallUpdate functionToolCallUpdate)
                    {
                        accumulator = new FunctionAccumulator();
                    }
                    else if (item.ContentUpdate != null)
                    {
                        accumulator = new PhraseAccumulator();
                    }
                }
                
                accumulator.Append(item);

                if (accumulator.HasItem)
                {
                    yield return accumulator.CurrentItem;
                    accumulator = new NullAccumulator();
                }

                if (item.FinishReason != null)
                {
                    this.FinishReason = item.FinishReason;
                }
            }

            accumulator.Flush();
            
            if (accumulator.HasItem)
            {
                yield return accumulator.CurrentItem;
            }

            this.Result = accumulator.Result;
        }
    }
}
