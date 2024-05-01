using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.FunctionAdapters
{
    public interface IFunctionAdapter
    {
        string FunctionName { get; }

        ChatCompletionsFunctionToolDefinition GetFunctionDefinition();

        Task<ChatRequestToolMessage> InvokeAsync(string id, string arguments);
    }
}
