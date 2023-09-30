using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAIDemo.Server.Model;
using OpenAIDemo.Shared;

namespace OpenAIDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpeechToTextController : ControllerBase
    {
        private IHttpClientFactory _httpClientFactory;
        private AzureConfig _config;

        public SpeechToTextController(IHttpClientFactory httpClientFactory, IOptions<AzureConfig> config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config.Value;
        }

        [HttpGet("token")]
        public async Task<SpeechToken> Get()
        {
            var token = new SpeechToken
            {
                AuthToken = await FetchTokenAsync(_config.Speech.SpeechKey, _config.Speech.SpeechRegion),
                Region = _config.Speech.SpeechRegion
            };

            return token;
        }

        private async Task<string> FetchTokenAsync(string subscriptionKey, string region)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var uriBuilder = new UriBuilder($"https://{region}.api.cognitive.microsoft.com/sts/v1.0/issueToken");
            var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
            return await result.Content.ReadAsStringAsync();
        }
    }
}
