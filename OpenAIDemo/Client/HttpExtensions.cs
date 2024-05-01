using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Text.Json;

namespace OpenAIDemo.Client
{
    public static class HttpExtensions
    {
        public async static IAsyncEnumerable<T> GetStreamAsync<T>(this HttpClient http, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.SetBrowserResponseStreamingEnabled(true); // Enable response streaming

            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();

            IAsyncEnumerable<T> items = JsonSerializer.DeserializeAsyncEnumerable<T>(
            responseStream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultBufferSize = 128
            });

            await foreach (T item in items)
            {
                yield return item;
            }
        }

        public async static IAsyncEnumerable<T> PostAndGetStreamAsync<T>(this HttpClient http, string url, HttpContent content)
        {
            var items = http.SendAndGetStreamAsync<T>(HttpMethod.Post, url, content);

            await foreach (T item in items)
            {
                yield return item;
            }
        }

        public async static IAsyncEnumerable<T> SendAndGetStreamAsync<T>(this HttpClient http, HttpMethod verb, string url, HttpContent content)
        {
            using var request = new HttpRequestMessage(verb, url);
            request.SetBrowserResponseStreamingEnabled(true); // Enable response streaming
            request.Content = content;
            
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();

            IAsyncEnumerable<T> items = JsonSerializer.DeserializeAsyncEnumerable<T>(
            responseStream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultBufferSize = 128
            });

            await foreach (T item in items)
            {
                yield return item;
            }
        }
    }
}
