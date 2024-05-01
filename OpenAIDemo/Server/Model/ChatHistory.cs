using Azure.AI.OpenAI;
using SharpToken;

namespace OpenAIDemo.Server.Model
{
    public class ChatHistory
    {
        protected List<ChatRequestMessage> MessagesInternal;

        public IEnumerable<ChatRequestMessage> Messages => MessagesInternal;

        private const int TokenLimit = 500;

        public ChatHistory()
        {
            MessagesInternal = new List<ChatRequestMessage>()
            {
                new ChatRequestSystemMessage($"You are a very useful AI assistant who will answer questions.")
            };
        }

        public ChatHistory(string prompt)
        {
            MessagesInternal = new List<ChatRequestMessage>()
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
                MessagesInternal.Sum(x => encoding.Encode(x.GetContent()).Count()) + 
                // add the tokens for the name of each message
                MessagesInternal.Where(x => !string.IsNullOrWhiteSpace(x.Role.ToString())).Count() * tokens_per_name +
                // add the tokens for the role of each message
                MessagesInternal.Count * tokens_per_message;

            return result;
        }

        public void AddMessage(ChatRequestMessage message)
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
    }
}
