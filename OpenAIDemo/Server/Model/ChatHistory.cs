using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAIDemo.Server.Model
{
    public class ChatHistory
    {
        private List<ChatMessage> _messages;

        public IEnumerable<ChatMessage> Messages => _messages;

        public ChatHistory()
        {
            _messages = new List<ChatMessage>()
            {
                new SystemChatMessage($"You are a very useful AI assistant who will answer questions.")
            };

            this.ShowLog(_messages[0]);
        }

        public ChatHistory(string prompt)
        {
            _messages = new List<ChatMessage>()
            {
                new SystemChatMessage(prompt)
            };
        }

        public void AddMessage(ChatMessage message)
        {
            _messages.Add(message);

            this.ShowLog(message);
        }

        private void ShowLog(ChatMessage message)
        {
            var forecolor = Console.ForegroundColor;

            if (message.GetRole() == ChatMessageRole.System)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (message.GetRole() == ChatMessageRole.User)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if (message.GetRole() == ChatMessageRole.Tool)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
            }
            else if (message.GetRole() == ChatMessageRole.Function)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
            }
            else if (message is AssistantChatMessage)
            {
                Console.ForegroundColor = ConsoleColor.White;
            }

            var json = JsonSerializer.Serialize(
                new
                {
                    Role = message.GetRole().ToString(),
                    Content = message.GetContent(),
                }, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                });

            Console.WriteLine(json);

            Console.ForegroundColor = forecolor;
        }

        public override string ToString()
        {
            return $"Message count: {_messages.Count}";
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(
                this.Messages.Select(x => new
                {
                    Role = x.GetRole(),
                    Content = x.GetContent(),
                }), new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                });
        }
    }
}
