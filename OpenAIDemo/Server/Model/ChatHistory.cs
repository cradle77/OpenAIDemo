using Azure.AI.OpenAI;
using System.Text.Json;

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
                new ChatMessage(ChatRole.System, $"You are a very useful AI assistant who will answer questions.")
            };
        }

        public ChatHistory(string prompt)
        {
            _messages = new List<ChatMessage>()
            {
                new ChatMessage(ChatRole.System, prompt)
            };
        }

        public void AddMessage(ChatMessage message)
        {
            _messages.Add(message);

            if (message.Role == ChatRole.Assistant)
            { 
                var forecolor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\r\n\r\n\r\n{this.ToJson()}");

                Console.ForegroundColor = forecolor;
            }
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
                    Role = x.Role.ToString(), 
                    Content = x.Content
                }), new JsonSerializerOptions() { WriteIndented = true });
        }
    }
}
