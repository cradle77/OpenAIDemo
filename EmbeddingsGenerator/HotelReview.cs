using Azure.Search.Documents.Models;
using SharpToken;
using System.Text.RegularExpressions;

namespace EmbeddingsGenerator
{
    // class with these properties:
    // Property Name,Review Rating,Review Title,Review Text,Location Of The Reviewer,Date Of Review

    public class HotelReview
    {
        public string Name { get; set; }
        public int ReviewRating { get; set; }
        public string ReviewTitle { get; set; }
        public string ReviewText { get; set; }
        public string Location { get; set; }
        public float[] Embedding { get; set; }

        public string GetEmbeddingInput()
        {
            var result =
                // remove multiple spaces from ReviewText with regex
                Regex.Replace(ReviewText, @"\s+", " ")
                // replace newlines with spaces
                .Replace("\n", " ");

            var encoding = GptEncoding.GetEncoding("r50k_base");

            var tokens = encoding.Encode(result)
                .Take(8192);

            result = encoding.Decode(tokens.ToList());

            return result;
        }

        public SearchDocument ToDocument()
        {
            var result = new SearchDocument();

            result["hotelId"] = Guid.NewGuid().ToString();
            result["rating"] = this.ReviewRating;
            result["hotelName"] = this.Name;
            result["reviewText"] = this.ReviewText;
            result["reviewEmbeddings"] = this.Embedding;

            return result;
        }
    }

}
