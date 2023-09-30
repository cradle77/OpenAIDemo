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
    public class CsvGetColumnsAdapter : IFunctionAdapter
    {
        private AzureConfig _config;

        public CsvGetColumnsAdapter(IOptions<AzureConfig> config)
        {
            _config = config.Value;
        }

        public string FunctionName => "get-file-columns";

        public FunctionDefinition GetFunctionDefinition()
        {
            return new FunctionDefinition()
            {
                Name = this.FunctionName,
                Description = "This function returns the column names and their types of the file to analyse. For each column, also the data type is returned.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        FileName = new
                        {
                            Type = "string",
                            Description = "The name of the csv file, including the extension",
                        }
                    },
                    Required = new string[] { "FileName" },
                }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }

        public async Task<ChatMessage> InvokeAsync(string arguments)
        {
            var file = JsonSerializer.Deserialize<FileQuery>(arguments, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var fileName = file.FileName;

            string filePath = $"abfss://datafiles@desdatademo.dfs.core.windows.net/raw/{fileName}";

            string sp = "sp_describe_first_result_set";

            string query = $@"SELECT TOP 100 * FROM OPENROWSET (
                BULK '{filePath}'
               ,FORMAT = 'CSV'
               ,PARSER_VERSION = '2.0'   
	           ,HEADER_ROW = TRUE
            ) AS[r]; ";

            var param = new
            {
                tsql = query
            };

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

                var queryResult = await connection.QueryAsync(sp, param, transaction:null, commandTimeout:60, commandType:System.Data.CommandType.StoredProcedure);

                List<FileMetadataEntity> result = queryResult
                    .Select(x => new FileMetadataEntity
                {
                    ColumnName = x.name.ToString(),
                    Type = x.system_type_name.ToString()
                        //Order = (int)x.column_ordinal
                    }).ToList();

                return new ChatMessage()
                {
                    Role = ChatRole.Function,
                    Name = this.FunctionName,
                    Content = JsonSerializer.Serialize(result, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                };
            }
        }

        public class FileQuery
        {
            public string FileName { get; set; }
        }

        public class FileMetadataEntity
        {
            public string ColumnName { get; set; }

            public string Type { get; set; }
        }
    }
}
