using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAIDemo.Server.Queuing;
using System.Text.Json;

namespace OpenAIDemo.Server.Model
{
    public class SummarisedChatHistory : ChatHistory
    {
        private string _originalPrompt;
        private IBackgroundTaskQueue _queue;

        public SummarisedChatHistory(IBackgroundTaskQueue queue, string prompt = $"You are a very useful AI assistant who will answer questions.") : base(prompt)
        {
            _originalPrompt = prompt;
            _queue = queue;
        }

        protected override void OnOverflow()
        {
            Console.WriteLine("Overflow detected, compressing history");

            _queue.QueueBackgroundWorkItem(async (serviceProvider, stoppingToken) =>
            {
                var messagesToReplace = this.MessagesInternal.Count; // skip the system message
                
                var config = serviceProvider.GetRequiredService<IOptions<AzureConfig>>().Value;

                OpenAIClient client = new(new Uri(config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(config.OpenAi.OpenAiKey));

                var prompt = @"compress the following text in a way that you (GPT-4) can reconstruct the intention of 
                               the human who wrote text as close as possible to the original intention. This is for yourself. 
                               It does not need to be human readable or understandable. Abuse of language mixing, 
                               abbreviations, symbols (unicode and emoji), or any other encodings or internal 
                               representations is all permissible, as long as it, if pasted in a new inference cycle, 
                               will yield near-identical results as the original text:";

                var sourceMessages = this.Messages.Take(messagesToReplace)
                    .Select(x => new
                    {
                        Role = x.Role.ToString(),
                        Content = x.GetContent()
                    });

                var messages = new List<ChatRequestMessage>
                {
                    new ChatRequestSystemMessage(prompt),
                    new ChatRequestUserMessage(JsonSerializer.Serialize(sourceMessages))
                };

                var response = await client.GetChatCompletionsAsync(new ChatCompletionsOptions(config.OpenAi.ChatEngine,
                                       messages)
                {
                    Temperature = 0.7f,
                    MaxTokens = 500,
                });

                var choice = response.Value.Choices.First();

                var newPrompt = _originalPrompt + "\r\nThis is a summary of the conversation which has happened so far. " + 
                "You have compressed by using language mixing, abbreviations, symbols " + 
                "(unicode and emoji), or any other encodings or internal representations:\r\n" + choice.Message.Content + 
                "\r\nDo not use compression in the future, as it is not human readable or understandable. Be as detailed " + 
                "and comprehensive as you want, without mimicking the style of the compressed summary.";

                Console.WriteLine($"New prompt: \r\n{newPrompt}");

                var newMessages = new List<ChatRequestMessage>
                {
                    new ChatRequestSystemMessage(newPrompt)
                };
                newMessages.AddRange(this.MessagesInternal.Skip(messagesToReplace + 1));
                
                this.MessagesInternal = newMessages;

                Console.WriteLine(this.ToString());
            });
        }
    }
}
