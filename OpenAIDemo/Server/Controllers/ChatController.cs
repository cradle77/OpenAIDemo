using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAIDemo.Server.Model;
using OpenAIDemo.Shared;
using System.Text.Json;

namespace OpenAIDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private static Dictionary<Guid, ChatHistory> _sessions;
        private AzureConfig _config;

        static ChatController()
        {
            _sessions = new Dictionary<Guid, ChatHistory>();
        }

        public ChatController(IOptions<AzureConfig> config)
        {
            _config = config.Value;
        }

        [HttpPost()]
        public IActionResult Post()
        {
            var sessionId = Guid.NewGuid();

            _sessions.Add(sessionId, new ChatHistory());
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

            history.AddMessage(new ChatMessage(ChatRole.User, message));

            var response = await client.GetChatCompletionsAsync(_config.OpenAi.ChatEngine, new ChatCompletionsOptions(
            history.Messages)
            {
                Temperature = 0.7f,
                MaxTokens = 500,
            });

            Console.WriteLine(JsonSerializer.Serialize(response.Value.Usage));

            var choice = response.Value.Choices.First();

            var responseMessage = choice.Message;

            history.AddMessage(responseMessage);

            Console.WriteLine(history);

            return Ok(responseMessage.Content);
        }

        [HttpPost("{sessionId}/message-stream")]
        public async IAsyncEnumerable<string> PostMessageStream(Guid sessionId, [FromBody] string message, CancellationToken token)
        {
            var history = _sessions[sessionId];

            OpenAIClient client = new(new Uri(_config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(_config.OpenAi.OpenAiKey));

            history.AddMessage(new ChatMessage(ChatRole.User, message));

            var response = await client.GetChatCompletionsStreamingAsync(_config.OpenAi.ChatEngine, new ChatCompletionsOptions(
                history.Messages)
            {
                Temperature = 0.7f,
                MaxTokens = 500,
            }, token);

            StreamingChatChoice choice = await response.Value.GetChoicesStreaming().FirstAsync();

            var responseMessages = choice.GetMessageStreaming();
            var fullResponse = string.Empty;

            await foreach (var responseMessage in responseMessages)
            {
                Console.WriteLine($"Response: {responseMessage.Content}");
                fullResponse += responseMessage.Content;
                yield return responseMessage.Content;
            }

            history.AddMessage(new ChatMessage(ChatRole.Assistant, fullResponse));

            Console.WriteLine(history);
        }
    }
}
