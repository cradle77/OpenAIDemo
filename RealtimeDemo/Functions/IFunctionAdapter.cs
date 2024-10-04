#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using OpenAI.RealtimeConversation;

namespace RealtimeDemo.Functions
{
    public interface IFunctionAdapter
    {
        string FunctionName { get; }

        ConversationFunctionTool GetFunctionDefinition();

        Task<string> InvokeAsync(string id, string arguments);
    }
}
