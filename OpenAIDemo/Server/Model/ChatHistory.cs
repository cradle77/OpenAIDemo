using OpenAI.Chat;
using SharpToken;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAIDemo.Server.Model
{
    public class ChatHistory
    {
        protected List<ChatMessage> MessagesInternal;

        public IEnumerable<ChatMessage> Messages => MessagesInternal;

        private const int TokenLimit = 4000;

        public ChatHistory()
        {
            MessagesInternal = new List<ChatMessage>()
            {
                new SystemChatMessage($"You are a very useful AI assistant who will answer questions.")
            };

            this.ShowLog(MessagesInternal[0]);
        }

        public ChatHistory(string prompt)
        {
            MessagesInternal = new List<ChatMessage>()
            {
                new SystemChatMessage(prompt)
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
                MessagesInternal.Sum(x => encoding.Encode(x.GetContent()).Count()) +
                // add the tokens for the name of each message
                MessagesInternal.Where(x => !string.IsNullOrWhiteSpace(x.GetRole().ToString())).Count() * tokens_per_name +
                // add the tokens for the role of each message
                MessagesInternal.Count * tokens_per_message;

            return result;
        }

        public void AddMessage(ChatMessage message)
        {
            MessagesInternal.Add(message);

            if (this.CalculateLength() > TokenLimit)
            {
                this.OnOverflow();
            }
        }

        protected virtual void OnOverflow()
        {
            while (this.CalculateLength() > TokenLimit)
            {
                Console.WriteLine($"Removing message: {MessagesInternal[1].GetContent().Substring(0, Math.Min(40, MessagesInternal[1].GetContent().Length))}");

                MessagesInternal.RemoveAt(1);
            }
        }

        public override string ToString()
        {
            return $"Message count: {MessagesInternal.Count} - Total tokens: {this.CalculateLength()}";
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