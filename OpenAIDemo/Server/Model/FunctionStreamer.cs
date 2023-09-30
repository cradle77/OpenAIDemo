using Azure.AI.OpenAI;
using System.Diagnostics;

namespace OpenAIDemo.Server.Model
{
    public class FunctionStreamer
    {
        private IAsyncEnumerable<ChatMessage> _sourceStream;

        public FunctionStreamer(IAsyncEnumerable<ChatMessage> sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public async IAsyncEnumerable<ChatMessage> GetFunctions(CancellationToken cancellationToken)
        {
            var message = new ChatMessage();

            var sourceMessages = await _sourceStream.ToListAsync();

            if (!sourceMessages.Any()) 
            {
                yield break;
            }

            Debug.Assert(sourceMessages.Select(m => m.FunctionCall?.Name)
                .Where(x => x != null).Distinct().Count() == 1);

            message.Name = sourceMessages[0].Name;
            message.Role = sourceMessages[0].Role;
            message.FunctionCall = new FunctionCall(sourceMessages[0].FunctionCall.Name, string.Empty);

            message.FunctionCall.Arguments = string.Join(string.Empty, sourceMessages.Select(x => x.FunctionCall?.Arguments));

            yield return message;
        }
    }
}
