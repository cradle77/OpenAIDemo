using Azure.AI.OpenAI;
using System.Text.Json;

namespace OpenAIDemo.Server.FunctionAdapters
{
    public class HotelBookingAdapter : IFunctionAdapter
    {
        public string FunctionName => "hotels-book";

        public ChatCompletionsFunctionToolDefinition GetFunctionDefinition()
        {
            return new ChatCompletionsFunctionToolDefinition()
            {
                Name = this.FunctionName,
                Description = "This allows you to book a hotel. It returns a JSON containing the booking confirmation number.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        HotelName = new
                        {
                            Type = "string",
                            Description = "The name of the hotel to be booked",
                        },
                        CheckInDate = new 
                        {
                            Type = "string",
                            Description = "The check-in date for the booking, ISO 8601 format",
                            Example =  "2023-06-28"
                        },
                        CheckOutDate = new
                        {
                            Type = "string",
                            Description = "The check-out date for the booking, ISO 8601 format",
                            Example = "2023-06-28"
                        },
                        NumberOfPeople = new 
                        {
                            Type = "number",
                            Description = "The number of people for the booking"
                        }
                    },
                    Required = new[] { "HotelName", "CheckInDate", "CheckOutDate", "NumberOfPeople" },
                }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }

        public Task<ChatRequestToolMessage> InvokeAsync(string id, string arguments)
        {
            return Task.FromResult(new ChatRequestToolMessage(
                JsonSerializer.Serialize(new
                {
                    ConfirmationNumber = "ABC123",
                    Text = "Your booking is confirmed"
                }), id));
        }
    }
}
