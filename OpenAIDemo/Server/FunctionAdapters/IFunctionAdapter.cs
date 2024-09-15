using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace OpenAIDemo.Server.FunctionAdapters
{
    public interface IFunctionAdapter
    {
        string FunctionName { get; }

        ChatTool GetFunctionDefinition();

        Task<ToolChatMessage> InvokeAsync(string id, string arguments);
    }
}
