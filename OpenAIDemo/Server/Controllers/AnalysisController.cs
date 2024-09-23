#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAIDemo.Server.Model;
using OpenAIDemo.Server.Plugins;
using OpenAIDemo.Shared;

namespace OpenAIDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalysisController : ControllerBase
    {
        private static Dictionary<Guid, ChatHistory> _sessions;
        private AzureConfig _config;
        private IChatCompletionService _chat;
        private Kernel _kernel;

        static AnalysisController()
        {
            _sessions = new Dictionary<Guid, ChatHistory>();
        }

        public AnalysisController(IOptions<AzureConfig> config, IChatCompletionService chat, Kernel kernel, DataAnalysisPlugin plugin)
        {
            _config = config.Value;
            _chat = chat;
            _kernel = kernel;

            _kernel.Plugins.Clear();
            _kernel.Plugins.AddFromObject(plugin);
        }

        [HttpPost()]
        public IActionResult Post()
        {
            string prompt = @"You are an AI expert on data analysis. A user has uploaded a file into a secure storage. You can retrieve the schema and execute queries over it. 
 The user will provide you the filename and you must:
 1) read the schema of the file with the get_file_columns function and infer the columns and their meanings
 2) determine 3 insights or comments you would like to retrieve from the data. Important: these insights must not be basic ones anyone can quickly assess. We want interesting facts about the data!

Then, for each of the insights:
 1) generate the corresponding SQL query and run it via the query_file function
 2) interpret the results and generate the comments for the user.

The output must only contain the insights you have generated, in the order you have generated them.

Only return the insight text after you have calculated it, without giving intermediate responses to the user.

The database is SQL Server, so always use standard T-SQL.

Data could potentially contain a big number of rows, so make sure all your queries are properly limited (max 20 rows) with a SELECT TOP ... clause";

            var sessionId = Guid.NewGuid();

            _sessions.Add(sessionId, new ChatHistory(prompt));
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
        public async IAsyncEnumerable<string> UploadCompletedAsync(Guid sessionId, string fileName, CancellationToken token)
        {
            var history = _sessions[sessionId];

            history.AddUserMessage($"The file name is {fileName}");

            history.ShowLastLog();

            var result = _chat.GetStreamingChatMessageContentsAsync(history, new AzureOpenAIPromptExecutionSettings()
            {
                MaxTokens = 500,
                Temperature = 0.7f,
                ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions
            }, _kernel);

            bool someContentReturned = false;

            while (true)
            {
                // if we have returned some content in the previous iteration,
                // we need to send a carriage return to the client
                if (someContentReturned)
                {
                    yield return "\n\n";
                    someContentReturned = false;
                }

                var fullResponse = string.Empty;

                var functionCallBuilder = new FunctionCallContentBuilder();

                await foreach (var responseMessage in result)
                {
                    if (responseMessage.Content != null)
                    {
                        fullResponse += responseMessage.Content;

                        yield return responseMessage.Content;

                        someContentReturned = true;
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
                    functionCalls.Select(async f =>
                    {
                        try
                        {
                            return await f.InvokeAsync(_kernel);
                        }
                        catch (Exception ex)
                        {
                            return new FunctionResultContent(f, ex);
                        }

                    });

                var functionResponses = await Task.WhenAll(functionExecutions);

                // step 3: add the responses to the history
                foreach (var functionResponse in functionResponses)
                {
                    history.Add(functionResponse.ToChatMessage());
                }
            }

            history.ShowCount();
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

            bool someContentReturned = false;

            while (true)
            {
                // if we have returned some content in the previous iteration,
                // we need to send a carriage return to the client
                if (someContentReturned)
                {
                    yield return "\n\n";
                    someContentReturned = false;
                }

                var fullResponse = string.Empty;

                var functionCallBuilder = new FunctionCallContentBuilder();

                await foreach (var responseMessage in result)
                {
                    if (responseMessage.Content != null)
                    {
                        fullResponse += responseMessage.Content;

                        yield return responseMessage.Content;

                        someContentReturned = true;
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
                    functionCalls.Select(async f =>
                    {
                        try
                        {
                            return await f.InvokeAsync(_kernel);
                        }
                        catch (Exception ex)
                        {
                            return new FunctionResultContent(f, ex);
                        }

                    });

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
