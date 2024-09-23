#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OpenAIDemo.Server.Plugins
{
    public class HotelPlugin
    {
        private ITextEmbeddingGenerationService _embeddingService;
        private SearchClient _searchClient;

        public HotelPlugin(ITextEmbeddingGenerationService embeddingService, SearchClient searchClient)
        {
            _embeddingService = embeddingService;
            _searchClient = searchClient;
        }

        [KernelFunction("search_hotels")]
        [Description("This function queries a search engine for hotel reviews. It accepts a full text query string.")]
        [return: Description("An array of hotel results")]
        public async Task<IEnumerable<HotelResult>> SearchHotelsAsync(HotelQuery query)
        {
            var embeddingsResponse = await _embeddingService.GenerateEmbeddingsAsync(new[] { query.QueryText });

            var queryEmbeddings = embeddingsResponse[0].ToArray();

            var searchOptions = new SearchOptions
            {
                Vectors = { new() { Value = queryEmbeddings, KNearestNeighborsCount = query.ResultCount, Fields = { "reviewEmbeddings" } } },
                Size = query.ResultCount,
                Select = { "hotelName", "rating", "reviewText" },
            };

            SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(null, searchOptions);

            var results = await response.GetResultsAsync()
                .Select(x => new HotelResult()
                {
                    Name = x.Document["hotelName"] as string,
                    ReviewRating = (int)x.Document["rating"],
                    ReviewText = x.Document["reviewText"] as string
                }).ToListAsync();

            return results;
        }

        [KernelFunction("book_hotel")]
        [Description("This allows you to book a hotel.")]
        [return: Description("A confirmation message and the booking confirmation number")]
        public HotelBookingResponse BookHotel(HotelBookingRequest request)
        {
            return new HotelBookingResponse
            {
                ConfirmationNumber = "ABC123",
                Text = $"You have successfully booked {request.NumberOfPeople} people at {request.HotelName} from {request.CheckInDate} to {request.CheckOutDate}"
            };
        }
    }

    public class HotelQuery
    {
        [Description("The text of the query for the hotels search engine")]
        [Required]
        public string QueryText { get; set; }

        [Description("The number of results you want to get. Default is 3.")]
        public int ResultCount { get; set; } = 3;
    }

    public class HotelResult
    {
        public string Name { get; set; }
        public int ReviewRating { get; set; }
        public string ReviewText { get; set; }
    }

    public class HotelBookingRequest
    {
        [Description("The name of the hotel to be booked")]
        public string HotelName { get; set; }

        [Description("The check-in date for the booking, ISO 8601 format")]
        public string CheckInDate { get; set; }

        [Description("The check-out date for the booking, ISO 8601 format")]
        public string CheckOutDate { get; set; }

        [Description("The number of people for the booking")]
        public int NumberOfPeople { get; set; }
    }

    public class HotelBookingResponse
    {
        public string ConfirmationNumber { get; set; }
        public string Text { get; set; }
    }
}
