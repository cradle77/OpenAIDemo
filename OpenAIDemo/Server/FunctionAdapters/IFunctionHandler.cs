using OpenAI.Chat;

namespace OpenAIDemo.Server.FunctionAdapters
{
    public interface IFunctionHandler
    {
        Task<ToolChatMessage> ExecuteCallAsync(ChatToolCall request);

        IEnumerable<ChatTool> GetFunctionDefinitions();
    }

    internal class FunctionHandler : IFunctionHandler
    {
        private Dictionary<string, IFunctionAdapter> _adapters;
        
        public FunctionHandler(IEnumerable<IFunctionAdapter> adapters)
        {
            _adapters = adapters.ToDictionary(a => a.FunctionName);
        }

        public async Task<ToolChatMessage> ExecuteCallAsync(ChatToolCall request)
        {
            if (!_adapters.ContainsKey(request.FunctionName))
            {
                throw new ArgumentException($"Function {request.FunctionName} not found");
            }

            Console.WriteLine($"Executing function {request.FunctionName} with arguments {request.FunctionArguments}");

            var result = await _adapters[request.FunctionName].InvokeAsync(request.Id, request.FunctionArguments);

            Console.WriteLine($"Function {request.FunctionName} returned {result.Content[0].Text}");

            return result;
        }

        public IEnumerable<ChatTool> GetFunctionDefinitions()
        {
            return _adapters.Values.Select(a => a.GetFunctionDefinition());
        }
    }
}
