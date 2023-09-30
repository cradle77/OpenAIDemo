using Azure.AI.OpenAI;

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
        }

        public override string ToString()
        {
            return $"Message count: {_messages.Count}";
        }
    }
}
