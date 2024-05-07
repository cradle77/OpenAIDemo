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

                var prompt = @"I'm going to provide you a JSON representation of a chat conversation, between the user and the AI assistant. " + 
                "Impersonate the user and create a short summary of the entire content, which you would expect to be provided to you as a sort of " +
                "recap. It has to be plain text, not JSON. The summary content should be from the user perspective, so something like 'I asked you etc.' ";

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
                    MaxTokens = 200,
                });

                var choice = response.Value.Choices.First();

                var newMessages = new List<ChatRequestMessage>
                {
                    new ChatRequestSystemMessage(_originalPrompt),
                    new ChatRequestUserMessage("Hello, this is a recap of the conversation so far: " + choice.Message.Content)
                };

                newMessages.AddRange(this.MessagesInternal.Skip(messagesToReplace + 1));
                
                this.MessagesInternal = newMessages;

                Console.WriteLine(this.ToJson());
            });
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
