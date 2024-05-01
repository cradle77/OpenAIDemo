using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.Model
{
    public class FunctionAccumulator : IAccumulator
    {
        private string functionId;
        private string functionName;
        private string arguments;

        public bool HasItem { get; private set; }

        public ChatRequestAssistantMessage CurrentItem { get; private set; }

        public ChatRequestAssistantMessage Result => null;

        public void Append(StreamingChatCompletionsUpdate item)
        {
            if (item.ToolCallUpdate is StreamingFunctionToolCallUpdate functionToolCallUpdate)
            {
                if (functionToolCallUpdate.Id != null)
                {
                    this.functionId = functionToolCallUpdate.Id;
                }
                if (functionToolCallUpdate.Name != null)
                {
                    this.functionName = functionToolCallUpdate.Name;
                }
                if (functionToolCallUpdate.ArgumentsUpdate != null)
                {
                    arguments += functionToolCallUpdate.ArgumentsUpdate;
                }
            }
        }

        public void Flush()
        {
            this.HasItem = true;

            this.CurrentItem = new ChatRequestAssistantMessage(string.Empty)
            {
                ToolCalls =
                {
                    new ChatCompletionsFunctionToolCall(this.functionId, this.functionName, this.arguments)
                }
            };

            this.functionId = null;
            this.functionName = null;
            this.arguments = string.Empty;
        }
    }
}
