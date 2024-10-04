#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using OpenAI.RealtimeConversation;

namespace RealtimeDemo.Functions
{
    public class FinishConversationTool : IFunctionAdapter
    {
        public string FunctionName => throw new NotImplementedException();

        public ConversationFunctionTool GetFunctionDefinition()
        {
            return new()
            {
                Name = "user_wants_to_finish_conversation",
                Description = "Invoked when the user says goodbye, expresses being finished, or otherwise seems to want to stop the interaction.",
                Parameters = BinaryData.FromString("{}")
            };
        }

        public async Task<string> InvokeAsync(string id, string arguments)
        {
            Environment.Exit(0);

            return null;
        }
    }
}
