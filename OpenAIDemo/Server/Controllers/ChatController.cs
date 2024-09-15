using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAIDemo.Server.FunctionAdapters;
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

            AzureOpenAIClient client = new(new Uri(_config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(_config.OpenAi.OpenAiKey));

            var chat = client.GetChatClient(_config.OpenAi.ChatEngine);

            history.AddMessage(new UserChatMessage(message));

            ChatCompletion completion;
            string result = string.Empty;

            do
            {
                var options = new ChatCompletionOptions()
                {
                    Temperature = 0.7f,
                    MaxTokens = 500,
                };
                options.Tools.AddRange(_functionHandler.GetFunctionDefinitions());

                var response = await chat.CompleteChatAsync(history.Messages, options);

                Console.WriteLine(JsonSerializer.Serialize(response.Value.Usage));

                completion = response.Value;

                if (completion.Content.Any())
                {
                    var responseMessage = completion.Content[0].Text;

                    history.AddMessage(new AssistantChatMessage(responseMessage));

                    result += responseMessage;
                }

                if (completion.ToolCalls.Any())
                {
                    history.AddMessage(new AssistantChatMessage(completion));

                    Console.WriteLine($"Number of tool calls: {completion.ToolCalls.Count}");

                    foreach (var toolCall in completion.ToolCalls)
                    {
                        history.AddMessage(await _functionHandler.ExecuteCallAsync(toolCall));
                    }
                }
            }
            while (completion.FinishReason != ChatFinishReason.Stop);

            Console.WriteLine(history.ToJson());

            return Ok(result);
        }

        [HttpPost("{sessionId}/message-stream")]
        public async IAsyncEnumerable<string> PostMessageStream(Guid sessionId, [FromBody] string message, CancellationToken token)
        {
            var history = _sessions[sessionId];

            AzureOpenAIClient client = new(new Uri(_config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(_config.OpenAi.OpenAiKey));

            var chat = client.GetChatClient(_config.OpenAi.ChatEngine);

            history.AddMessage(new UserChatMessage(message));

            ChatFinishReason? finishReason = null;

            do
            {
                var options = new ChatCompletionOptions()
                {
                    Temperature = 0.7f,
                    MaxTokens = 500,
                };
                options.Tools.AddRange(_functionHandler.GetFunctionDefinitions());

                var response = chat.CompleteChatStreamingAsync(history.Messages, options, token);

                var responseStreamer = new ResponseStreamer(response);

                await foreach (var streamedResponse in responseStreamer.GetPhrases(token))
                {
                    if (streamedResponse.ToolCalls.Any())
                    {
                        Console.WriteLine($"Number of tool calls: {streamedResponse.ToolCalls.Count}");

                        history.AddMessage(streamedResponse);

                        foreach (var toolCall in streamedResponse.ToolCalls)
                        {
                            history.AddMessage(await _functionHandler.ExecuteCallAsync(toolCall));
                        }
                    }
                    else
                    {
                        foreach (ChatMessageContentPart contentPart in streamedResponse.Content)
                        {
                            Console.WriteLine($"Response: {contentPart.Text}");
                            yield return contentPart.Text;
                        }
                    }                    
                }

                if (responseStreamer.Result != null)
                {
                    history.AddMessage(responseStreamer.Result);
                }

                finishReason = responseStreamer.FinishReason;

            }
            while (finishReason != ChatFinishReason.Stop);

            Console.WriteLine(history);
        }
    }
}