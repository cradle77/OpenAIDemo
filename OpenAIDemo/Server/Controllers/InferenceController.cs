using Azure.AI.OpenAI;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAIDemo.Server.Model;
using System.Text.Json;
using OpenAIDemo.Server.InferenceProviders;
using OpenAIDemo.Shared;

namespace OpenAIDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InferenceController : ControllerBase
    {
        private AzureConfig _config;
        private IInferenceProvider<ReviewDetails> _provider;

        public InferenceController(IOptions<AzureConfig> config, IInferenceProvider<ReviewDetails> provider)
        {
            _config = config.Value;
            _provider = provider;
        }

        [HttpPost()]
        public async Task<IActionResult> InferReviewAsync([FromBody] string reviewText)
        {
            string prompt = @"You are an AI which helps storing review data. You will be prompted with a review text, between triple backticks.
             For example a prompt could be like:
             This is the review:
             ```Hotel Corinthia was simply stunning```

             You need to invoke the hotel-review function to store the review details.";

            var history = new ChatHistory(prompt);
            history.AddMessage(new ChatMessage(ChatRole.User, $"The review text is: ```{reviewText}```"));

            // Enter the deployment name you chose when you deployed the model.
            string engine = "gpt4-des";

            OpenAIClient client = new(new Uri(_config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(_config.OpenAi.OpenAiKey));

            ChatChoice choice;

            var response = await client.GetChatCompletionsAsync(engine, new ChatCompletionsOptions(
                history.Messages)
            {
                Temperature = 0f,
                MaxTokens = 2000,
                Functions = new List<FunctionDefinition>() { _provider.GetFunctionDefinition() },
                FunctionCall = _provider.GetFunctionDefinition()
            });

            Console.WriteLine(JsonSerializer.Serialize(response.Value.Usage));

            choice = response.Value.Choices.First();

            if (choice.Message.FunctionCall != null)
            {
                var result = _provider.GetResponse(choice.Message.FunctionCall.Arguments);

                return this.Ok(result);
            }

            return this.Ok("No function call detected");
        }
    }
}
