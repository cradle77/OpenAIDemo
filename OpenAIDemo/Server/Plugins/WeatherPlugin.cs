using Microsoft.SemanticKernel;
using OpenAIDemo.Shared;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OpenAIDemo.Server.Plugins
{
    public class WeatherPlugin
    {
        private static readonly string[] Summaries = new[]
        {
            "Clear", "Partly Cloudy", "Overcast", "Rainy", "Thunderstorms", "Windy"
        };

        [KernelFunction("get_weather")]
        [Description("Gets the weather forecasts for a given city for the specified dates. Ignore the temperatures in Farheneit in your responses, unless explicitly asked")]
        [return: Description("An array of weather forecasts")]
        public async Task<IEnumerable<WeatherForecast>> GetWeatherAsync(WeatherQuery query)
        {
            try
            {
                await Task.Delay(1000);

                var forecasts = Enumerable.Range(0, query.GetNumberOfDays()).Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(query.StartDate.GetValueOrDefault(DateTime.Today)).AddDays(index),
                    TemperatureC = Random.Shared.Next(15, 25),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();

                return forecasts;
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Sorry, I couldn't get the weather for you. Check if the parameters are correct and try again if they aren't.");
            }
        }
    }

    public class WeatherQuery
    {
        [Description("The city and state, e.g. San Francisco, CA")]
        [Required]
        public string Location { get; set; }

        [Description("The start date for the weather forecast, yyyy-MM-dd format. Example: 2024-02-28")]
        [Required]
        public DateTime? StartDate { get; set; }

        [Description("The end date for the weather forecast, yyyy-MM-dd format. Example: 2024-02-28")]
        public DateTime? EndDate { get; set; }

        public int GetNumberOfDays()
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
