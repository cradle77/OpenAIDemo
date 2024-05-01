using Azure.AI.OpenAI;
using Azure.Identity;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OpenAIDemo.Server.Model;
using OpenAIDemo.Shared;
using System.Text.Json;
using System.Threading;

namespace OpenAIDemo.Server.FunctionAdapters
{
    public class CsvQueryAdapter : IFunctionAdapter
    {
        private AzureConfig _config;

        public CsvQueryAdapter(IOptions<AzureConfig> config)
        {
            _config = config.Value;
        }

        public string FunctionName => "query-file";

        public ChatCompletionsFunctionToolDefinition GetFunctionDefinition()
        {
            return new ChatCompletionsFunctionToolDefinition()
            {
                Name = this.FunctionName,
                Description = "This function allows you to execute a SQL query over a specified file and will return the resultset. The table name is always [TableName]. Do not specify the schema. You also need to pass the filename.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        FileName = new
                        {
                            Type = "string",
                            Description = "The name of the csv file, including the extension",
                        },
                        SqlQuery = new
                        {
                            Type = "string",
                            Description = "The query you want to execute over the file. The table name is always [TableName] without the schema. Important: the query must be in standard T-SQL.",
                            Example = "SELECT TOP 10 * from [TableName] ORDER BY Date DESC",
                        },
                    },
                    Required = new string[] { "FileName", "SqlQuery" },
                }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }

        public async Task<ChatRequestToolMessage> InvokeAsync(string id, string arguments)
        {
            try
            {
                var file = JsonSerializer.Deserialize<FileQuery>(arguments, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                var fileName = file.FileName;

                string filePath = $"abfss://datafiles@desdatademo.dfs.core.windows.net/raw/{fileName}";

                string sp = "sp_describe_first_result_set";

                string query = file.SqlQuery.Replace("[TableName]", $@"OPENROWSET (
                BULK '{filePath}'
               ,FORMAT = 'CSV'
               ,PARSER_VERSION = '2.0'   
	           ,HEADER_ROW = TRUE
            ) AS[r] ");

                using (var connection = new SqlConnection(_config.Synapse.DbConnectionString))
                {
                    var credential = new VisualStudioCredential(
                        new VisualStudioCredentialOptions
                        {
                            TenantId = _config.TenantId
                        });

                    var cancellationToken = new CancellationTokenSource().Token;
                    var token = credential.GetToken(new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" }), cancellationToken);
                    connection.AccessToken = token.Token;

                    var queryResult = await connection.QueryAsync(query, commandTimeout: 60);

                    return new ChatRequestToolMessage(
                        JsonSerializer.Serialize(queryResult, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new ChatRequestToolMessage(
                    "there was an error running the function, try again and make sure you are using valid T-SQL", id);
            }
        }

        public class FileQuery
        {
            public string FileName { get; set; }

            public string SqlQuery { get; set; }
        }
    }
}
