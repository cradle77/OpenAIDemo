#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using OpenAI.RealtimeConversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RealtimeDemo.Functions
{
    public class HotelBookingAdapter : IFunctionAdapter
    {
        public string FunctionName => "hotels-book";

        public ConversationFunctionTool GetFunctionDefinition()
        {
            return new ConversationFunctionTool()
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
                            Example = "2023-06-28"
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

        public Task<string> InvokeAsync(string id, string arguments)
        {
            return Task.FromResult(
                JsonSerializer.Serialize(new
                {
                    ConfirmationNumber = "ABC123",
                    Text = "Your booking is confirmed"
                }));
        }
    }
}
