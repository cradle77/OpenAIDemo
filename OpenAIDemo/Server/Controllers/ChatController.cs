﻿using Azure.AI.OpenAI;
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

            history.AddMessage(new ChatMessage(ChatRole.User, message));

            ChatChoice choice;

            do
            {
                var response = await client.GetChatCompletionsAsync(_config.OpenAi.ChatEngine, new ChatCompletionsOptions(
                history.Messages)
                {
                    Temperature = 0.7f,
                    MaxTokens = 500,
                    Functions = _functionHandler.GetFunctionDefinitions().ToList()
                });

                Console.WriteLine(JsonSerializer.Serialize(response.Value.Usage));

                choice = response.Value.Choices.First();

                if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
                {
                    history.AddMessage(await _functionHandler.ExecuteFunctionCallAsync(choice.Message.FunctionCall));
                }
            }
            while (choice.FinishReason == CompletionsFinishReason.FunctionCall);

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

            StreamingChatChoice choice;

            do
            {
                var response = await client.GetChatCompletionsStreamingAsync(_config.OpenAi.ChatEngine, new ChatCompletionsOptions(
                history.Messages)
                {
                    Temperature = 0.7f,
                    MaxTokens = 500,
                    Functions = _functionHandler.GetFunctionDefinitions().ToList()
                }, token);

                choice = await response.Value.GetChoicesStreaming().FirstAsync();

                Console.WriteLine(await choice.GetFinishReasonAsync() ?? "reason was empty");
                if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
                {
                    // concatenate all the messages in the asyncenumerable
                    var messages = new FunctionStreamer(choice.GetMessageStreaming());
                    var functionMessage = await messages.GetFunctions(token).SingleAsync();

                    history.AddMessage(await _functionHandler.ExecuteFunctionCallAsync(functionMessage.FunctionCall));
                }
            }
            while (choice.FinishReason == CompletionsFinishReason.FunctionCall);

            //var responseMessages = new NullStreamer(choice.GetMessageStreaming());
            var responseMessages = new PhraseStreamer(choice.GetMessageStreaming());

            await foreach (var responseMessage in responseMessages.GetPhrases(token))
            {
                Console.WriteLine($"Response: {responseMessage.Content}");
                yield return responseMessage.Content;
            }

            history.AddMessage(responseMessages.Result);

            Console.WriteLine(history);
        }
    }

    public static class Extensions
    {
        public static async Task<CompletionsFinishReason?> GetFinishReasonAsync(this StreamingChatChoice choice)
        {
            await choice.GetMessageStreaming().FirstOrDefaultAwaitAsync(async x => !string.IsNullOrEmpty(x.Content));

            return choice.FinishReason;
        }
    }
}
