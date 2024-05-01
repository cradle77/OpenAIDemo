using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.Model
{
    public class FunctionAccumulator
    {
        private record FunctionCallDetails
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Arguments { get; set; }
        }

        private Dictionary<int, FunctionCallDetails> functions = new Dictionary<int, FunctionCallDetails>();

        public bool HasItem { get; private set; }

        public ChatRequestAssistantMessage CurrentItem { get; private set; }

        public void Append(StreamingChatCompletionsUpdate item)
        {
            if (item.ToolCallUpdate is StreamingFunctionToolCallUpdate functionToolCallUpdate)
            {
                if (!functions.ContainsKey(functionToolCallUpdate.ToolCallIndex))
                {
                    functions[functionToolCallUpdate.ToolCallIndex] = new FunctionCallDetails();
                }
                
                if (functionToolCallUpdate.Id != null)
                {
                    functions[functionToolCallUpdate.ToolCallIndex].Id = functionToolCallUpdate.Id;
                }
                if (functionToolCallUpdate.Name != null)
                {
                    functions[functionToolCallUpdate.ToolCallIndex].Name = functionToolCallUpdate.Name;
                }
                if (functionToolCallUpdate.ArgumentsUpdate != null)
                {
                    functions[functionToolCallUpdate.ToolCallIndex].Arguments += functionToolCallUpdate.ArgumentsUpdate;
                }
            }
        }

        public void Flush()
        {
            if (functions.Count == 0)
            {
                this.HasItem = false;
                return;
            }

            this.HasItem = true;

            this.CurrentItem = new ChatRequestAssistantMessage(string.Empty);
            foreach (var function in functions.Values)
            {
                this.CurrentItem.ToolCalls.Add(new ChatCompletionsFunctionToolCall(function.Id, function.Name, function.Arguments));
            }
            
            this.functions = new Dictionary<int, FunctionCallDetails>();
        }
    }
}
