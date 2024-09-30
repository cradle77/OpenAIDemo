#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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

            _sessions.Add(sessionId, new ChatHistory($"You are a very useful AI assistant who will answer questions and manages a shopping list. Please remember to not mention the content of the shopping list every time otherwise it will get very boring. Today's date is in European format is {DateTime.Today.ToShortDateString()}.").ShowLog());
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

            string responseMessage = string.Empty;

            while (true)
            {
                var result = await _chat.GetChatMessageContentAsync(history, new AzureOpenAIPromptExecutionSettings()
                {
                    MaxTokens = 500,
                    Temperature = 0.7f,
                    ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions
                }, _kernel);

                if (!string.IsNullOrEmpty(result.Content))
                {
                    responseMessage += result.Content;
                }

                // Step 1: add to the history, including the possible function calls
                history.Add(result);
                history.ShowLastLog();

                IEnumerable<FunctionCallContent> functionCalls = FunctionCallContent.GetFunctionCalls(result);
                if (!functionCalls.Any())
                {
                    break;
                }

                Console.WriteLine($"Requested execution of {functionCalls.Count()} functions");

                // Step 2: trigger the execution and await
                var functionExecutions =
                    functionCalls.Select(f => f.InvokeAsync(_kernel));
                
                var functionResponses = await Task.WhenAll(functionExecutions);

                // Step 3: add the responses to the history
                foreach (var functionResponse in functionResponses)
                {
                    history.Add(functionResponse.ToChatMessage());
                }
            }

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

                var functionCallBuilder = new FunctionCallContentBuilder();

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
                history.ShowLastLog();

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
                    functionCalls.Select(f => f.InvokeAsync(_kernel));

                var functionResponses = await Task.WhenAll(functionExecutions);

                // step 3: add the responses to the history
                foreach (var functionResponse in functionResponses)
                {
                    history.Add(functionResponse.ToChatMessage());
                }
            }

            history.ShowCount();
        }
    }
}
