using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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
        private Kernel _kernel;

        static ChatController()
        {
            _sessions = new Dictionary<Guid, ChatHistory>();
        }

        public ChatController(IOptions<AzureConfig> config, IChatCompletionService chat, Kernel kernel)
        {
            _config = config.Value;
            _chat = chat;
            _kernel = kernel;
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

            var result = await _chat.GetChatMessageContentAsync(history, new AzureOpenAIPromptExecutionSettings() 
            {
                MaxTokens = 500,
                Temperature = 0.7f,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            }, _kernel);

            string responseMessage = result.ToString();

            history.AddAssistantMessage(responseMessage);

            history.ShowLastLog();

            history.ShowCount();

            return Ok(responseMessage);
        }

        [HttpPost("{sessionId}/message-stream")]
        public async IAsyncEnumerable<string> PostMessageStream(Guid sessionId, [FromBody] string message, CancellationToken token)
        {
            var history = _sessions[sessionId];

            history.AddUserMessage(message);

            history.ShowLastLog();

            var result = _chat.GetStreamingChatMessageContentsAsync(history, new AzureOpenAIPromptExecutionSettings()
            {
                MaxTokens = 500,
                Temperature = 0.7f,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            }, _kernel);

            var fullResponse = string.Empty;

            await foreach (var responseMessage in result)
            {
                fullResponse += responseMessage.Content;

                yield return responseMessage.Content;
            }

            history.AddAssistantMessage(fullResponse);

            history.ShowLastLog();

            history.ShowCount();
        }
    }
}
