using System.Text.Json.Serialization;

namespace OpenAIDemo.Shared
{
    public class ReviewDetails
    {
        public string HotelName { get; set; }

        public int? Duration { get; set; }

        public float SentimentValue { get; set; }

        public string[] PositiveNotes { get; set; }

        public string[] NegativeNotes { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
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
