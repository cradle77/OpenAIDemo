using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
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

            AzureOpenAIClient client = new(new Uri(_config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(_config.OpenAi.OpenAiKey));

            var chat = client.GetChatClient(_config.OpenAi.ChatEngine);

            history.AddMessage(new UserChatMessage(message));

            var response = await chat.CompleteChatAsync(history.Messages, new ChatCompletionOptions()
            {
                Temperature = 0.7f,
                MaxTokens = 500,
            });

            Console.WriteLine(JsonSerializer.Serialize(response.Value.Usage));

            var responseMessage = response.Value.Content;

            history.AddMessage(new AssistantChatMessage(responseMessage));

            Console.WriteLine(history);

            return Ok(responseMessage);
        }

        [HttpPost("{sessionId}/message-stream")]
        public async IAsyncEnumerable<string> PostMessageStream(Guid sessionId, [FromBody] string message, CancellationToken token)
        {
            var history = _sessions[sessionId];

            AzureOpenAIClient client = new(new Uri(_config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(_config.OpenAi.OpenAiKey));

            var chat = client.GetChatClient(_config.OpenAi.ChatEngine);

            history.AddMessage(new UserChatMessage(message));

            var response = chat.CompleteChatStreamingAsync(history.Messages, new ChatCompletionOptions()
            {
                Temperature = 0.7f,
                MaxTokens = 500,
            }, token);

            var fullResponse = string.Empty;

            await foreach (StreamingChatCompletionUpdate completionUpdate in response)
            {
                foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                {
                    Console.WriteLine($"Response: {contentPart.Text}");
                    fullResponse += contentPart.Text;
                    yield return contentPart.Text;
                }
            }

            history.AddMessage(new AssistantChatMessage(fullResponse));

            Console.WriteLine(history);
        }
    }
}
