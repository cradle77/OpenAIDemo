using Azure.AI.OpenAI;
using OpenAIDemo.Shared;
using System.Text.Json;

namespace OpenAIDemo.Server.FunctionAdapters
{
    public class WeatherFunctionAdapter : IFunctionAdapter
    {
        public string FunctionName => "get-weather";

        public FunctionDefinition GetFunctionDefinition()
        {
            return new FunctionDefinition()
            {
                Name = this.FunctionName,
                Description = "Gets the weather forecasts for a given city for the specified dates. Ignore the temperatures in Farheneit in your responses, unless explicitly asked",
                Parameters = BinaryData.FromObjectAsJson(new 
                {
                    Type = "object",
                    Properties = new
                    {
                        Location = new
                        {
                            Type = "string",
                            Description = "The city and state, e.g. San Francisco, CA",
                        },
                        StartDate = new
                        {
                            Type = "string",
                            Description = "The start date for the weather forecast, yyyy-MM-dd format",
                            Example = "2023-06-28"
                        },
                        EndDate = new
                        {
                            Type = "string",
                            Description = "The end date for the weather forecast, yyyy-MM-dd format",
                            Example = "2023-06-28"
                        }
                    },
                    Required = new[] { "location", "StartDate", "EndDate" },
                }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }

        private static readonly string[] Summaries = new[]
        {
            "Clear", "Partly Cloudy", "Overcast", "Rainy", "Thunderstorms", "Windy"
        };

        public async Task<ChatMessage> InvokeAsync(string arguments)
        {
            var parameters = JsonSerializer.Deserialize<WeatherQuery>(arguments, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var forecasts = Enumerable.Range(0, parameters.NumberOfDays).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(parameters.StartDate.GetValueOrDefault(DateTime.Today).AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

            return new ChatMessage(ChatRole.Function,JsonSerializer.Serialize(
                    forecasts,
                    new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
                { 
                    Name = this.FunctionName
                };
        }
    }

    public class WeatherQuery
    {
        public string Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int NumberOfDays
        {
            get 
            {
                if (StartDate.HasValue && EndDate.HasValue)
                {
                    return (int)(EndDate.Value - StartDate.Value).TotalDays + 1;
                }
                else
                {
                    return 5;
                }
            }
        }
            
    }   
}
