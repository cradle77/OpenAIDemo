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
                ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions
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
                ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions
            }, _kernel);

            while(true)
            {
                var fullResponse = string.Empty;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var functionCallBuilder = new FunctionCallContentBuilder();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                await foreach (var responseMessage in result)
                {
                    if (responseMessage.Content != null)
                    {
                        fullResponse += responseMessage.Content;

                        yield return responseMessage.Content;
                    }
                    
                    functionCallBuilder.Append(responseMessage);
                }

                history.AddAssistantMessage(fullResponse);

                // handling functions
                var functionCalls = functionCallBuilder.Build();

                if (!functionCalls.Any())
                {
                    break; // no function calls, the loop is finished
                }

                Console.WriteLine($"Requested execution of {functionCalls.Count()} functions");

                // step 1: add the request container to the history
                var functionRequests = new ChatMessageContent(
                    role: AuthorRole.Assistant, 
                    content: null);

                foreach (var functionRequest in functionCalls)
                {
                    functionRequests.Items.Add(functionRequest);
                }
                history.Add(functionRequests);

                // Step 2: trigger the execution and await
                var functionExecutions =
                    functionCalls.Select(async f => 
                    {
                        try
                        {
                            return await f.InvokeAsync(_kernel);
                        }
                        catch (Exception ex)
                        {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                            return new FunctionResultContent(f, ex);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                        }
                        
                    });

                var functionResponses = await Task.WhenAll(functionExecutions);

                // step 3: add the requests and responses to the history
                foreach (var functionResponse in functionResponses)
                {
                    history.Add(functionResponse.ToChatMessage());
                }
            }

            history.ShowCount();
        }
    }
}
