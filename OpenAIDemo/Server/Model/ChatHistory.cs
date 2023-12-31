﻿using Azure.AI.OpenAI;
using SharpToken;

namespace OpenAIDemo.Server.Model
{
    public class ChatHistory
    {
        private List<ChatMessage> _messages;

        public IEnumerable<ChatMessage> Messages => _messages;

        private const int TokenLimit = 4000;

        public ChatHistory()
        {
            _messages = new List<ChatMessage>()
            {
                new ChatMessage(ChatRole.System, $"You are a very useful AI assistant who will answer questions and manages a shopping list. Please remember to not mention the content of the shopping list every time otherwise it will get very boring. Today's date is in European format is {DateTime.Today.ToShortDateString()}.")
            };
        }

        public ChatHistory(string prompt)
        {
            _messages = new List<ChatMessage>()
            {
                new ChatMessage(ChatRole.System, prompt)
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
                _messages.Sum(x => encoding.Encode(x.Content).Count()) + 
                // add the tokens for the name of each message
                _messages.Where(x => !string.IsNullOrWhiteSpace(x.Name)).Count() * tokens_per_name +
                // add the tokens for the role of each message
                _messages.Count * tokens_per_message;

            return result;
        }

        public void AddMessage(ChatMessage message)
        {
            _messages.Add(message);

            while (this.CalculateLength() > TokenLimit)
            {
                _messages.RemoveAt(1);
            }
        }

        public override string ToString()
        {
            return $"Message count: {_messages.Count} - Total tokens: {this.CalculateLength()}";
        }
    }
}
