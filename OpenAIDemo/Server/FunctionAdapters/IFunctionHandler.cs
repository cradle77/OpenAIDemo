using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.FunctionAdapters
{
    public interface IFunctionHandler
    {
        Task<ChatRequestToolMessage> ExecuteCallAsync(ChatCompletionsFunctionToolCall request);

        IEnumerable<ChatCompletionsFunctionToolDefinition> GetFunctionDefinitions();
    }

    internal class FunctionHandler : IFunctionHandler
    {
        private Dictionary<string, IFunctionAdapter> _adapters;
        
        public FunctionHandler(IEnumerable<IFunctionAdapter> adapters)
        {
            _adapters = adapters.ToDictionary(a => a.FunctionName);
        }

        public async Task<ChatRequestToolMessage> ExecuteCallAsync(ChatCompletionsFunctionToolCall request)
        {
            if (!_adapters.ContainsKey(request.Name))
            {
                throw new ArgumentException($"Function {request.Name} not found");
            }

            Console.WriteLine($"Executing function {request.Name} with arguments {request.Arguments}");

            var result = await _adapters[request.Name].InvokeAsync(request.Id, request.Arguments);

            Console.WriteLine($"Function {request.Name} returned {result.Content}");

            return result;
        }

        public IEnumerable<ChatCompletionsFunctionToolDefinition> GetFunctionDefinitions()
        {
            return _adapters.Values.Select(a => a.GetFunctionDefinition());
        }
    }
}
