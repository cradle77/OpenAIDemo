using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAIDemo.Server.FunctionAdapters;
using OpenAIDemo.Server.Model;
using OpenAIDemo.Shared;
using System.Text.Json;

namespace OpenAIDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalysisController : ControllerBase
    {
        private AzureConfig _config;
        private static Dictionary<Guid, ChatHistory> _sessions = new Dictionary<Guid, ChatHistory>();
        private IFunctionHandler _functionHandler;
        private string _engine;

        public AnalysisController(IOptions<AzureConfig> config, IFunctionHandler functionHandler)
        {
            _config = config.Value;
            _functionHandler = functionHandler;
            _engine = "gpt4-des";
            //_engine = _config.OpenAi.ChatEngine;
        }

        [HttpPost()]
        public IActionResult Post()
        {
            string prompt = @"You are an AI expert on data analysis. A user has uploaded a file into a secure storage. You can retrieve the schema and execute queries over it. 
 The user will provide you the filename and you must:
 1) read the schema of the file with the get-file-columns function and infer the columns and their meanings
 2) determine 3 insights or comments you would like to retrieve from the data. Important: these insights must not be basic ones anyone can quickly assess. We want interesting facts about the data!

Then, for each of the insights:
 1) generate the corresponding SQL query and run it via the query-file function
 2) interpret the results and generate the comments for the user.

The output must only contain the insights you have generated, in the order you have generated them.

Only return the insight text after you have calculated it, without giving intermediate responses to the user.

The database is SQL Server, so always use standard T-SQL.

Data could potentially contain a big number of rows, so make sure all your queries are properly limited (never exceed 20 rows)";

            var sessionId = Guid.NewGuid();

            _sessions.Add(sessionId, new ChatHistory(prompt, tokenLimit:8000));
            return Ok(new ChatSession() { Id = sessionId });
        }

        [HttpPost("{sessionId}/files/{fileName}")]
        public IActionResult UploadNewFile(Guid sessionId, string fileName)
        {
            var result = new DataFile(Guid.NewGuid());
            result.FileName = fileName;

            var blobServiceClient = new BlobServiceClient(new Uri(_config.Adls.StorageEndpoint), new VisualStudioCredential(
                new VisualStudioCredentialOptions
                {
                    TenantId = _config.TenantId
                }));

            var blobContainerClient = blobServiceClient.GetBlobContainerClient(_config.Adls.ContainerName);

            var blobClient = blobContainerClient.GetBlobClient(result.FilePath);

            var userDelegationKey = blobServiceClient.GetUserDelegationKey(DateTimeOffset.UtcNow.AddSeconds(-30),
                                                                    DateTimeOffset.UtcNow.AddHours(2));

            BlobSasBuilder sas = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b", // b for blob, c for container
                StartsOn = DateTimeOffset.UtcNow.AddSeconds(-30),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(2),
            };

            sas.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

            var sasToken = sas.ToSasQueryParameters(userDelegationKey, _config.Adls.AccountName).ToString();

            result.Url = $"{blobClient.Uri}?{sasToken}";

            return this.Ok(result);
        }

        [HttpPatch("{sessionId}/files/{fileName}")]
        public async Task<IActionResult> UploadCompletedAsync(Guid sessionId, string fileName)
        {
            if (!_sessions.ContainsKey(sessionId))
            {
                return NotFound();
            }
            var history = _sessions[sessionId];

            OpenAIClient client = new(new Uri(_config.OpenAi.OpenAiEndpoint), new AzureKeyCredential(_config.OpenAi.OpenAiKey));

            history.AddMessage(new ChatMessage(ChatRole.User, $"The file name is {fileName}"));

            string result = string.Empty;

            ChatChoice choice;

            do
            {
                var response = await client.GetChatCompletionsAsync(_engine, new ChatCompletionsOptions(
                history.Messages)
                {
                    Temperature = 0f,
                    MaxTokens = 2000,
                    Functions = _functionHandler.GetFunctionDefinitions().ToList()
                });

                Console.WriteLine(JsonSerializer.Serialize(response.Value.Usage));

                choice = response.Value.Choices.First();

                if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
                {
                    history.AddMessage(await _functionHandler.ExecuteFunctionCallAsync(choice.Message.FunctionCall));
                }
                else
                {
                    var responseMessage = choice.Message;

                    history.AddMessage(responseMessage);

                    result += choice.Message.Content;
                }
            }
            while (choice.FinishReason != CompletionsFinishReason.Stopped);

            Console.WriteLine(history);

            return Ok(result);
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

            string result = string.Empty;

            ChatChoice choice;

            do
            {
                var response = await client.GetChatCompletionsAsync(_engine, new ChatCompletionsOptions(
                history.Messages)
                {
                    Temperature = 0f,
                    MaxTokens = 2000,
                    Functions = _functionHandler.GetFunctionDefinitions().ToList()
                });

                Console.WriteLine(JsonSerializer.Serialize(response.Value.Usage));

                choice = response.Value.Choices.First();

                if (choice.FinishReason == CompletionsFinishReason.FunctionCall)
                {
                    history.AddMessage(await _functionHandler.ExecuteFunctionCallAsync(choice.Message.FunctionCall));
                }
                else
                {
                    var responseMessage = choice.Message;

                    history.AddMessage(responseMessage);

                    result += choice.Message.Content;
                }
            }
            while (choice.FinishReason != CompletionsFinishReason.Stopped);

            Console.WriteLine(history);

            return Ok(result);
        }
    }
}
