using Azure.AI.OpenAI;
using SharpToken;
using System.Text.Json;

namespace OpenAIDemo.Server.Model
{
    public class ChatHistory
    {
        private List<ChatRequestMessage> _messages;

        public IEnumerable<ChatRequestMessage> Messages => _messages;

        private const int TokenLimit = 4000;

        public ChatHistory()
        {
            _messages = new List<ChatRequestMessage>()
            {
                new ChatRequestSystemMessage($"You are a very useful AI assistant who will answer questions and manages a shopping list. Please remember to not mention the content of the shopping list every time otherwise it will get very boring. Today's date is in European format is {DateTime.Today.ToShortDateString()}.")
            };
        }

        public ChatHistory(string prompt)
        {
            _messages = new List<ChatRequestMessage>()
            {
                new ChatRequestSystemMessage(prompt)
            };
        }

        private int CalculateLength()
        {
            // using logic explained here:
            // https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb
            var encoding = GptEncoding.GetEncodingForModel("gpt-35-turbo");

            var tokens_per_message = 3; // message are encoded in the format:
                                        // <|im_start|>role
                                        // message
                                        // <|im_end|>
            var tokens_per_name = 1;

            var result =
                // sum the tokens in each message
                _messages.Sum(x => encoding.Encode(x.GetContent() ?? string.Empty).Count()) +
                // add the tokens for the name of each message
                _messages.Where(x => !string.IsNullOrWhiteSpace(x.Role.ToString())).Count() * tokens_per_name +
                // add the tokens for the role of each message
                _messages.Count * tokens_per_message;

            return result;
        }

        public void AddMessage(ChatRequestMessage message)
        {
            _messages.Add(message);

            while (this.CalculateLength() > TokenLimit)
            {
                Console.WriteLine($"Removing message: {_messages[1].GetContent().Substring(0, Math.Min(40, _messages[1].GetContent().Length))}");

                _messages.RemoveAt(1);
            }
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
            return $"Message count: {_messages.Count} - Total tokens: {this.CalculateLength()}";
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