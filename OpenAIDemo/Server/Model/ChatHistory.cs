using Azure.AI.OpenAI;
using System.Text.Json;

namespace OpenAIDemo.Server.Model
{
    public class ChatHistory
    {
        private List<ChatRequestMessage> _messages;

        public IEnumerable<ChatRequestMessage> Messages => _messages;

        public ChatHistory()
        {
            _messages = new List<ChatRequestMessage>()
            {
                new ChatRequestSystemMessage($"You are a very useful AI assistant who will answer questions.")
            };

            this.ShowLog(_messages[0]);
        }

        public ChatHistory(string prompt)
        {
            _messages = new List<ChatRequestMessage>()
            {
                new ChatRequestSystemMessage(prompt)
            };
        }

        public void AddMessage(ChatRequestMessage message)
        {
            _messages.Add(message);

            this.ShowLog(message);
        }

        private void ShowLog(ChatRequestMessage message)
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
                    Content = message.GetContent()
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
                    Content = x.GetContent()
                }), new JsonSerializerOptions() { WriteIndented = true });
        }
    }
}
