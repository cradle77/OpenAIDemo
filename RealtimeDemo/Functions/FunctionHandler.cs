#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using OpenAI.RealtimeConversation;
using System.Text.Json;

namespace RealtimeDemo.Functions
{
    internal class FunctionHandler
    {
        private Dictionary<string, IFunctionAdapter> _adapters;

        public FunctionHandler(IEnumerable<IFunctionAdapter> adapters)
        {
            _adapters = adapters.ToDictionary(a => a.FunctionName);
        }

        public async Task ExecuteCallAsync(RealtimeConversationSession session, ConversationItemFinishedUpdate request)
        {
            if (!_adapters.ContainsKey(request.FunctionName))
            {
                throw new ArgumentException($"Function {request.FunctionName} not found");
            }

            var functionRequest = ConversationItem.CreateFunctionCall(request.FunctionName, request.FunctionCallId, request.FunctionCallArguments);
            await session.AddItemAsync(functionRequest);

            Console.WriteLine($"request added for function {request.FunctionName}");

            var result = await _adapters[request.FunctionName].InvokeAsync(request.FunctionCallId, request.FunctionCallArguments);

            Console.WriteLine(result);

            var item = ConversationItem.CreateFunctionCallOutput(request.FunctionCallId, result);
            await session.AddItemAsync(item);

            await session.StartResponseTurnAsync();
        }

        public IEnumerable<ConversationTool> GetFunctionDefinitions()
        {
            return _adapters.Values.Select(a => a.GetFunctionDefinition());
        }
    }
}
