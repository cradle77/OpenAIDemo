using Azure.AI.OpenAI;
using Azure;
using Microsoft.AspNetCore.Mvc;
using OpenAIDemo.Shared;
using System.Text.Json;
using OpenAIDemo.Server.FunctionAdapters;
using OpenAIDemo.Server.Model;
using Microsoft.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace OpenAIDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private static Dictionary<Guid, ChatHistory> _sessions;
        private IFunctionHandler _functionHandler;
        private AzureConfig _config;

        static ChatController()
        {
            _sessions = new Dictionary<Guid, ChatHistory>();
        }

        public ChatController(IOptions<AzureConfig> config, IFunctionHandler functionHandler)
        {
            _functionHandler = functionHandler;
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

            history.AddMessage(new ChatRequestUserMessage(message));

            ChatChoice choice;
            string result = string.Empty;

            do
            {
                var options = new ChatCompletionsOptions(_config.OpenAi.ChatEngine,
                history.Messages)
                {
                    Temperature = 0.7f,
                    MaxTokens = 500,
                };
                options.Tools.AddRange(_functionHandler.GetFunctionDefinitions());

                var response = await client.GetChatCompletionsAsync(options);

                Console.WriteLine(JsonSerializer.Serialize(response.Value.Usage));

                choice = response.Value.Choices.First();

                if (choice.Message.Content != null)
                {
                    var responseMessage = choice.Message;

                    history.AddMessage(new ChatRequestAssistantMessage(responseMessage.Content));

                    result += choice.Message.Content;
                }

                if (choice.Message.ToolCalls.Any())
                {
                    ChatRequestAssistantMessage toolCallHistoryMessage = new(choice.Message);

                    history.AddMessage(toolCallHistoryMessage);

                    Console.WriteLine($"Number of tool calls: {choice.Message.ToolCalls.Count}");

                    foreach (var toolCall in choice.Message.ToolCalls.OfType<ChatCompletionsFunctionToolCall>())
                    {
                        history.AddMessage(await _functionHandler.ExecuteCallAsync(toolCall));
                    }
                }
            }
            while (choice.FinishReason != CompletionsFinishReason.Stopped);

            Console.WriteLine(history.ToJson());

            return Ok(result);
        }

        [HttpPost("{sessionId}/message-stream")]
        public async IAsyncEnumerable<string> PostMessageStream(Guid sessionId, [FromBody] string message, CancellationToken token)
        {
            var history = _sessions[sessionId];

            OpenAIClient client = new(new Uri(_config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(_config.OpenAi.OpenAiKey));

            history.AddMessage(new ChatRequestUserMessage(message));

            CompletionsFinishReason? finishReason = null;

            do
            {
                var options = new ChatCompletionsOptions(_config.OpenAi.ChatEngine,
                history.Messages)
                {
                    Temperature = 0.7f,
                    MaxTokens = 500,
                };
                options.Tools.AddRange(_functionHandler.GetFunctionDefinitions());

                var response = await client.GetChatCompletionsStreamingAsync(options, token);

                var responseStreamer = new ResponseStreamer(response);

                await foreach (var streamedResponse  in responseStreamer.GetPhrases(token))
                {
                    if (streamedResponse.ToolCalls.Any())
                    {
                        Console.WriteLine($"Number of tool calls: {streamedResponse.ToolCalls.Count}");

                        history.AddMessage(streamedResponse);

                        foreach (var toolCall in streamedResponse.ToolCalls.OfType<ChatCompletionsFunctionToolCall>())
                        {
                            history.AddMessage(await _functionHandler.ExecuteCallAsync(toolCall));
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Response: {streamedResponse.Content}");
                        yield return streamedResponse.Content;
                    }                    
                }

                if (responseStreamer.Result != null)
                {
                    history.AddMessage(responseStreamer.Result);
                }

                finishReason = responseStreamer.FinishReason;

            }
            while (finishReason != CompletionsFinishReason.Stopped);


            Console.WriteLine(history);
        }
    }
}