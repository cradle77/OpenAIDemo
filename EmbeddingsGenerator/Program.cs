using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using CsvHelper;
using EmbeddingsGenerator;
using System.Globalization;

var searchUrl = new Uri("https://demo-ai-search.search.windows.net");
var indexName = "embed-idx";
var searchCredential = new AzureKeyCredential("RIJUxTOy0zyRvbJAY2wSUyY543yM1aSrUzeSLO9DupAzSeCTmrsI");
//var searchCredential = new VisualStudioCredential();
const int ModelDimensions = 1536;
const string vectorSearchConfigName = "my-vector-config";
string openAiEndpoint = "https://gptdes.openai.azure.com/";
string openAiKey = "c68d80b3f00a4c719a3687f925b647c8";
// Enter the deployment name you chose when you deployed the model.
string engine = "ada-embed";

await QueryIndexAsync();


async Task QueryIndexAsync()
{
    var searchClient = new SearchClient(searchUrl, indexName, searchCredential);
    OpenAIClient openAiclient = new(new Uri(openAiEndpoint), new AzureKeyCredential(openAiKey));

    while (true)
    {
        Console.WriteLine("Type your query and hit enter");
        var query = Console.ReadLine();

        // calculating embeddings of the query

        var openAiResponse = await openAiclient.GetEmbeddingsAsync(engine,
                new EmbeddingsOptions(query));

        var queryEmbeddings = openAiResponse.Value.Data[0].Embedding;

        // searching the index for the closest embeddings
        var searchOptions = new SearchOptions
        {
            Vectors = { new() { Value = queryEmbeddings.ToArray(), KNearestNeighborsCount = 3, Fields = { "reviewEmbeddings" } } },
            Size = 3,
            Select = { "hotelName", "rating", "reviewText" },
        };

        SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>(null, searchOptions);

        int count = 0;
        await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
        {
            count++;
            Console.WriteLine($"hotelName: {result.Document["hotelName"]}");
            Console.WriteLine($"Score: {result.Score}\r\n");
            Console.WriteLine($"rating: {result.Document["rating"]}");
            Console.WriteLine($"reviewText: {result.Document["reviewText"]}\r\n");
            Console.WriteLine("\r\n\r\n");
        }
        Console.WriteLine($"Total Results: {count}");
        Console.WriteLine("\r\n\r\n\r\n\r\n");
    }
}


async void CreateIndexAsync()
{
    var indexClient = new SearchIndexClient(searchUrl, searchCredential);

    Console.WriteLine("Checking if index exists");

    var indexExists = await indexClient.GetIndexesAsync().AsPages()
        .Where(x => x.Values.Any(idx => idx.Name == indexName))
        .AnyAsync();

    if (indexExists)
    {
        Console.WriteLine("Index exists, deleting it");
        await indexClient.DeleteIndexAsync(indexName);
    }

    // https://github.com/Azure/cognitive-search-vector-pr/blob/main/demo-dotnet/code/Program.cs
    var definition = new SearchIndex(indexName)
    {
        VectorSearch = new()
        {
            AlgorithmConfigurations =
        {
            new HnswVectorSearchAlgorithmConfiguration(vectorSearchConfigName)
        }
        },
        Fields =
        {
        new SimpleField("hotelId", SearchFieldDataType.String) { IsKey = true, IsFilterable = true, IsSortable = true },
        new SimpleField("rating", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
        new SearchableField("hotelName") { IsFilterable = true, IsSortable = true },
        new SearchableField("reviewText") { AnalyzerName = LexicalAnalyzerName.EnLucene },
        new SearchField("reviewEmbeddings", SearchFieldDataType.Collection(SearchFieldDataType.Single))
        {
            IsSearchable = true,
            VectorSearchDimensions = ModelDimensions,
            VectorSearchConfiguration = vectorSearchConfigName
        }
    }
    };

    Console.WriteLine("Creating index");
    await indexClient.CreateIndexAsync(definition);

    var searchClient = new SearchClient(searchUrl, indexName, searchCredential);

    OpenAIClient client = new(new Uri(openAiEndpoint), new AzureKeyCredential(openAiKey));

    Console.WriteLine("Calculating embeddings and populating index");

    using (var reader = new StreamReader("London_hotel_reviews.csv"))
    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    {
        var records = csv.GetRecords<HotelReview>();

        var batches = records
            .Select((record, index) => new { record, index })
            .GroupBy(x => x.index / 16)
            .Select(g => g.Select(x => x.record))
            .Take(100);

        int i = 0;
        foreach (var batch in batches)
        {
            Console.WriteLine($"Calculating batch {++i}");
            var inputs = batch.ToList();

            var result = await client.GetEmbeddingsAsync(engine,
                new EmbeddingsOptions(batch.Select(x => x.GetEmbeddingInput())));

            foreach (var itemResult in result.Value.Data)
            {
                inputs[itemResult.Index].Embedding = itemResult.Embedding.ToArray();
            }

            Console.WriteLine($"Indexing batch {i}");
            // push the data into the azure search index
            var documents = inputs.Select(x => x.ToDocument());

            var indexingResult = await searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(documents));

            if (!indexingResult.Value.Results.All(x => x.Succeeded))
            {
                Console.WriteLine("Indexing failed");
                break;
            }
        }
    }
}

