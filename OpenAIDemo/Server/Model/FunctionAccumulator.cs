using OpenAI.Chat;

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

        public AssistantChatMessage CurrentItem { get; private set; }

        public void Append(StreamingChatCompletionUpdate item)
        {
            foreach (var functionToolCallUpdate in item.ToolCallUpdates)
            {
                if (!functions.ContainsKey(functionToolCallUpdate.Index))
                {
                    functions[functionToolCallUpdate.Index] = new FunctionCallDetails();
                }

                if (functionToolCallUpdate.Id != null)
                {
                    functions[functionToolCallUpdate.Index].Id = functionToolCallUpdate.Id;
                }
                if (functionToolCallUpdate.FunctionName != null)
                {
                    functions[functionToolCallUpdate.Index].Name = functionToolCallUpdate.FunctionName;
                }
                if (functionToolCallUpdate.FunctionArgumentsUpdate != null)
                {
                    functions[functionToolCallUpdate.Index].Arguments += functionToolCallUpdate.FunctionArgumentsUpdate;
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

            this.CurrentItem = new AssistantChatMessage(string.Empty);
            foreach (var function in functions.Values)
            {
                this.CurrentItem.ToolCalls.Add(ChatToolCall.CreateFunctionToolCall(function.Id, function.Name, function.Arguments));
            }
            
            this.functions = new Dictionary<int, FunctionCallDetails>();
        }
    }
}
