﻿using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAIDemo.Server.Model;
using OpenAIDemo.Server.Queuing;
using OpenAIDemo.Shared;
using System.Net;
using System.Text.Json;

namespace OpenAIDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private static Dictionary<Guid, ChatHistory> _sessions;
        private IBackgroundTaskQueue _queue;
        private AzureConfig _config;

        static ChatController()
        {
            _sessions = new Dictionary<Guid, ChatHistory>();
        }

        public ChatController(IOptions<AzureConfig> config, IBackgroundTaskQueue queue)
        {
            _queue = queue;
            _config = config.Value;
        }

        [HttpPost()]
        public IActionResult Post()
        {
            var sessionId = Guid.NewGuid();

            _sessions.Add(sessionId, new SummarisedChatHistory(_queue));
            return Ok(new ChatSession() { Id = sessionId });
        }

        [HttpPost("{sessionId}/message")]
        public async Task<IActionResult> PostMessage(Guid sessionId, [FromBody] string message)
        {
            if (!_sessions.ContainsKey(sessionId))
            {
                return NotFound();
            }
            var history = _sessions[sessionId];

            OpenAIClient client = new(new Uri(_config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(_config.OpenAi.OpenAiKey));

            history.AddMessage(new ChatRequestUserMessage(message));

            var response = await client.GetChatCompletionsAsync(new ChatCompletionsOptions(_config.OpenAi.ChatEngine,
            history.Messages)
            {
                Temperature = 0.7f,
                MaxTokens = 500,
            });

            Console.WriteLine(JsonSerializer.Serialize(response.Value.Usage));

            var choice = response.Value.Choices.First();

            var responseMessage = choice.Message;

            history.AddMessage(new ChatRequestAssistantMessage(responseMessage.Content));

            Console.WriteLine(history);

            return Ok(responseMessage.Content);
        }

        [HttpPost("{sessionId}/message-stream")]
        public async IAsyncEnumerable<string> PostMessageStream(Guid sessionId, [FromBody] string[] payload, CancellationToken token)
        {
            var history = _sessions[sessionId];

            var content = new List<ChatMessageContentItem>();
            // add the text payload
            content.Add(new ChatMessageTextContentItem(payload.First()));
            // add all the images
            foreach (var image in payload.Skip(1))
            {
                content.Add(new ChatMessageImageContentItem(new Uri(image)));
            }
            
            OpenAIClient client = new(new Uri(_config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(_config.OpenAi.OpenAiKey));
            
            history.AddMessage(new ChatRequestUserMessage(content));
            
            var response = await client.GetChatCompletionsStreamingAsync(new ChatCompletionsOptions(_config.OpenAi.ChatEngine,
                history.Messages)
            {
                Temperature = 0.7f,
                MaxTokens = 500,
            }, token);

            var fullResponse = string.Empty;

            await foreach (StreamingChatCompletionsUpdate responseMessage in response)
            {
                Console.WriteLine($"Response: {responseMessage.ContentUpdate}");
                fullResponse += responseMessage.ContentUpdate;
                yield return responseMessage.ContentUpdate;
            }

            history.AddMessage(new ChatRequestAssistantMessage(fullResponse));

            Console.WriteLine(history);
        }
    }
}
