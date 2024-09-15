using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SharpToken;
using System.Text.Json;

namespace OpenAIDemo.Server.Model
{
    internal static class ChatHistoryExtensions
    {
        public static ChatHistory ShowLog(this ChatHistory history)
        {
            foreach (var message in history)
            {
                message.ShowLog();
            }

            return history;
        }

        public static ChatHistory ShowLastLog(this ChatHistory history)
        {
            history.Last().ShowLog();

            return history;
        }

        public static void ShowLog(this ChatMessageContent? message)
        {
            var forecolor = Console.ForegroundColor;

            if (message.Role == AuthorRole.System)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (message.Role == AuthorRole.User)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else if (message.Role == AuthorRole.Assistant)
            {
                Console.ForegroundColor = ConsoleColor.White;
            }

            var json = JsonSerializer.Serialize(
                new
                {
                    Role = message.Role.ToString(),
                    Content = message.ToString()
                }, new JsonSerializerOptions() { WriteIndented = true });

            Console.WriteLine(json);

            Console.ForegroundColor = forecolor;
        }
    }

    internal static class ChatHistorySizeControlExtensions 
    {
        private const int TOKEN_LIMIT = 5000;

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

        public static void EnsureChatHistorySize(this ChatHistory history, int tokenLimit = TOKEN_LIMIT)
        {
            while (history.CalculateLength() > tokenLimit)
            {
                Console.WriteLine($"Removing message: {history[1].ToString().Substring(0, Math.Min(40, history[1].ToString().Length))}");

                history.RemoveAt(1);
            }
        }
    }
}
