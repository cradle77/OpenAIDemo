using Azure.AI.OpenAI;

namespace OpenAIDemo.Server.InferenceProviders
{
    public interface IInferenceProvider<T>
    {
        string FunctionName { get; }

        ChatCompletionsFunctionToolDefinition GetFunctionDefinition();

        T GetResponse(string arguments);
    }
}
