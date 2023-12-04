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

            this.ShowLog(_messages[0]);
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

            this.ShowLog(message);
        }

        private void ShowLog(ChatMessage message)
        {
            var forecolor = Console.ForegroundColor;

            if (message.Role == ChatRole.System)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (message.Role == ChatRole.User)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else if (message.Role == ChatRole.Assistant)
            {
                Console.ForegroundColor = ConsoleColor.White;
            }

            var json = JsonSerializer.Serialize(
                new
                {
                    Role = message.Role.ToString(), 
                    Content = message.Content
                }, new JsonSerializerOptions() { WriteIndented = true });

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
                    Role = x.Role.ToString(), 
                    Content = x.Content
                }), new JsonSerializerOptions() { WriteIndented = true });
        }
    }
}
