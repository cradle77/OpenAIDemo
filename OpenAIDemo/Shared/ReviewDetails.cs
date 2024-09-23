using System.ComponentModel;
using System.Text.Json.Serialization;

namespace OpenAIDemo.Shared
{
    public class ReviewDetails
    {
        [Description("The name of the hotel of the review, if present in the text")]
        public string HotelName { get; set; }

        [Description("The number of nights spent in the hotel, if present in the text")]
        public int? Duration { get; set; }

        [Description("The sentiment of the review, from 0 to 1. 0 means that the experience was terrible, 0.5 means average, 1 means that the experience was perfect. Can have decimals to represent a spectrum of sentiments")]
        public float SentimentValue { get; set; }

        [Description("Top 3 positive notes of the review, if present in the text. This needs to be translated in English.")]
        public string[] PositiveNotes { get; set; }

        [Description("Top 3 negative notes of the review, if present in the text. This needs to be translated in English.")]
        public string[] NegativeNotes { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Description("The type of customer that wrote the review, if present in the text")]
        public CustomerType? CustomerType { get; set; }
    }

    public enum CustomerType
    {
        Single,
        Couple,
        Family,
        Business,
        Group
    }
}
