using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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
}
