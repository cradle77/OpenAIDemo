using Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.Chat;
using OpenAI;
using SharpToken;
using System.Text.Json;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel;

namespace OpenAIDemo.Server.Model
{
    internal static class ChatHistoryTrimExtensions 
    {
        private const int TOKEN_LIMIT = 700;

        public static void ShowCount(this ChatHistory history)
        {
            Console.WriteLine($"Message count: {history.Count} - Total tokens: {history.CalculateLength()}");
        }

        private static int CalculateLength(this ChatHistory history)
        {
            // using logic explained here:
            // https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb
            var encoding = GptEncoding.GetEncodingForModel("gpt-4");

            var tokens_per_message = 3; // message are encoded in the format:
                                        // <|im_start|>role
                                        // message
                                        // <|im_end|>
            var tokens_per_name = 1;

            var result =
                // sum the tokens in each message
                history.Sum(x => encoding.Encode(x.ToString()).Count()) +
                // add the tokens for the name of each message
                history.Where(x => !string.IsNullOrWhiteSpace(x.Role.ToString())).Count() * tokens_per_name +
                // add the tokens for the role of each message
                history.Count * tokens_per_message;

            return result;
        }

        public static void TrimToMaxSize(this ChatHistory history, int tokenLimit = TOKEN_LIMIT)
        {
            while (history.CalculateLength() > tokenLimit)
            {
                Console.WriteLine($"Removing message: {history[1].ToString().Substring(0, Math.Min(40, history[1].ToString().Length))}");

                history.RemoveAt(1);
            }
        }

        public static async Task CompressAsync(this ChatHistory history, IChatCompletionService chat, int tokenLimit = TOKEN_LIMIT)
        {
            if (history.CalculateLength() <= tokenLimit)
            {
                Console.WriteLine("No need to compress.");
                return;
            }

            var messagesToReplace = history.Count - 1;

            var prompt = @"I'm going to provide you a JSON representation of a chat conversation, between the user and the AI assistant. " +
            "Impersonate the user and create a short summary of the entire content, which you would expect to be provided to you as a sort of " +
            "recap. It has to be plain text, not JSON. The summary content should be from the user perspective, so something like 'I asked you etc.':" +
            "" +
            "Here is the JSON representation of the conversation:" +
            "{0} ";

            var sourceMessages = history.Skip(1).Take(messagesToReplace)
                .Select(x => new
                {
                    Role = x.Role.ToString(),
                    Content = x.ToString()
                });

            var response = await chat.GetChatMessageContentAsync(
                string.Format(prompt, JsonSerializer.Serialize(sourceMessages)),
                new AzureOpenAIPromptExecutionSettings()
                {
                    MaxTokens = 500,
                    Temperature = 0f // no randomness
                });

            string responseMessage = "We have been having a conversation which I have summarised for you. " +
                "From now on, please resume the normal conversation tone, and use the summary below if relevant to " +
                "my questions. \r\n" +
                $"Here is the summary: \r\n\r\n{response.ToString()}";

            history.RemoveRange(1, messagesToReplace);

            history.Insert(1, new ChatMessageContent(AuthorRole.User, responseMessage));

            Console.WriteLine("New compressed history:");

            history.ShowLog();
        }
    }
}
