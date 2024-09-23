using Azure.Identity;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OpenAIDemo.Server.Model;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OpenAIDemo.Server.Plugins
{
    public class DataAnalysisPlugin
    {
        private AzureConfig _config;

        public DataAnalysisPlugin(IOptions<AzureConfig> config)
        {
            _config = config.Value;
        }

        [KernelFunction("get_file_columns")]
        [Description("This function returns the column names and their types of the file to analyse. For each column, also the data type is returned.")]
        [return: Description("An array of column metadata for the file")]
        public async Task<IEnumerable<ColumnMetadata>> GetFileColumnsAsync(FileMetadataQuery file)
        {
            try
            {
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

                    var queryResult = await connection.QueryAsync(sp, param, transaction: null, commandTimeout: 60, commandType: System.Data.CommandType.StoredProcedure);

                    List<ColumnMetadata> result = queryResult
                        .Select(x => new ColumnMetadata
                        {
                            ColumnName = x.name.ToString(),
                            Type = x.system_type_name.ToString()
                        }).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception("there was an error running the function, feel free to try again another couple of times or to stop here");
            }
        }

        [KernelFunction("query_file")]
        [Description("This function allows you to execute a SQL query over a specified file and will return the resultset. The table name is always [TableName]. Do not specify the schema. You also need to pass the filename.")]
        [return: Description("The result of the query")]
        public async Task<IEnumerable<dynamic>> QueryFileAsync(FileQuery file)
        {
            try
            {
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

                    var queryResult = (await connection.QueryAsync(query, commandTimeout: 60))
                        .Take(20); // enforce max 20 rows

                    return queryResult;
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"there was an error running the function: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception($"there was an error running the function of type {ex.GetType().Name}, try again and make sure you are using valid T-SQL. Also make sure the table name is always [TableName], with square brackets");
            }
        }
    }

    public class FileMetadataQuery
    {
        [Description("The name of the csv file, including the extension")]
        [Required]
        public string FileName { get; set; }
    }

    public class ColumnMetadata
    {
        public string ColumnName { get; set; }

        public string Type { get; set; }
    }

    public class FileQuery
    {
        [Description("The name of the csv file, including the extension")]
        [Required]
        public string FileName { get; set; }

        [Description("The query you want to execute over the file. The table name is always [TableName] without the schema. Important: the query must be in standard T-SQL. Example: SELECT TOP 10 * from [TableName] ORDER BY Date DESC")]
        [Required]
        public string SqlQuery { get; set; }
    }
}
