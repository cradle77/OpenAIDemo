using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAIDemo.Server.Model;
using OpenAIDemo.Shared;

namespace OpenAIDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private static Dictionary<Guid, ChatHistory> _sessions;
        private AzureConfig _config;
        private IChatCompletionService _chat;

        static ChatController()
        {
            _sessions = new Dictionary<Guid, ChatHistory>();
        }

        public ChatController(IOptions<AzureConfig> config, IChatCompletionService chat)
        {
            _config = config.Value;
            _chat = chat;
        }

        [HttpPost()]
        public IActionResult Post()
        {
            var sessionId = Guid.NewGuid();

            _sessions.Add(sessionId, new ChatHistory($"You are a very useful AI assistant who will answer questions.").ShowLog());
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

            history.AddUserMessage(message);

            history.ShowLastLog();

            var result = await _chat.GetChatMessageContentAsync(history);

            string responseMessage = result.ToString();

            history.AddAssistantMessage(responseMessage);

            history.ShowLastLog();

            return Ok(responseMessage);
        }

        [HttpPost("{sessionId}/message-stream")]
        public async IAsyncEnumerable<string> PostMessageStream(Guid sessionId, [FromBody] string message, CancellationToken token)
        {
            var history = _sessions[sessionId];

            history.AddUserMessage(message);

            history.ShowLastLog();

            var result = _chat.GetStreamingChatMessageContentsAsync(history);

            var fullResponse = string.Empty;

            await foreach (var responseMessage in result)
            {
                fullResponse += responseMessage.Content;

                Console.WriteLine(responseMessage.Content);

                yield return responseMessage.Content;
            }

            history.AddAssistantMessage(fullResponse);

            history.ShowLastLog();
        }
    }
}
