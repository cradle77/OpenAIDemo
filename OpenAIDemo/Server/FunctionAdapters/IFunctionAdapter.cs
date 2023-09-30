using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.FunctionAdapters
{
    public interface IFunctionAdapter
    {
        string FunctionName { get; }

        FunctionDefinition GetFunctionDefinition();

        Task<ChatMessage> InvokeAsync(string arguments);
    }
}
