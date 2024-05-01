using Azure.AI.OpenAI;
using OpenAIDemo.Shared;
using System.Text.Json;

namespace OpenAIDemo.Server.InferenceProviders
{
    public class ReviewInferenceProvider : IInferenceProvider<ReviewDetails>
    {
        public string FunctionName => "hotel-review";

        public ChatCompletionsFunctionToolDefinition GetFunctionDefinition()
        {
            return new ChatCompletionsFunctionToolDefinition()
            {
                Name = this.FunctionName,
                Description = "This function is used to store details of a review. Positive and Negative notes must have at most 3 elements each and must be in English.",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        HotelName = new
                        {
                            Type = "string",
                            Description = "The name of the hotel of the review, if present in the text",
                        },
                        Duration = new
                        {
                            Type = "number",
                            Description = "The number of nights spent in the hotel, if present in the text",
                            Example = "3"
                        },
                        CustomerType = new
                        {
                            Type = "string",
                            Description = "The type of customer that wrote the review, if present in the text",
                            Enum = Enum.GetNames(typeof(CustomerType)),
                            Example = "Single"
                        },
                        SentimentValue = new
                        {
                            Type = "number",
                            Description = "The sentiment of the review, from 0 to 1. 0 means that the experience was terrible, 0.5 means average, 1 means that the experience was perfect. Can have decimals to represent a spectrum of sentiments",
                            Example = "0.5"
                        },
                        PositiveNotes = new
                        {
                            Type = "array",
                            Description = "Top 3 positive notes of the review, if present in the text. This needs to be translated in English.",
                            Items = new
                            {
                                Type = "string"
                            }
                        },
                        NegativeNotes = new
                        {
                            Type = "array",
                            Description = "Top 3 negative notes of the review, if present in the text. This needs to be translated in English.",
                            Items = new
                            {
                                Type = "string"
                            }
                        }
                    },
                    Required = new string[] { },
                }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
        }

        public ReviewDetails GetResponse(string arguments)
        {
            return JsonSerializer.Deserialize<ReviewDetails>(arguments, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}
