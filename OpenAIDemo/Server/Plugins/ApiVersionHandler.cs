using System.Web;

namespace OpenAIDemo.Server.Plugins
{
    // thanks to https://www.clearpeople.com/blog/overwriting-azure-openai-api-api-version-property-using-semantic-kernel
    public class ApiVersionHandler : DelegatingHandler
    {
        private const string ApiVersionKey = "api-version";

        private const string ApiVersionValue = "2024-08-01-preview";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uriBuilder = new UriBuilder(request.RequestUri);

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            if (query[ApiVersionKey] != null)
            {
                query[ApiVersionKey] = ApiVersionValue;
                uriBuilder.Query = query.ToString();

                request.RequestUri = uriBuilder.Uri;
            }

            var result = await base.SendAsync(request, cancellationToken);

            return result;
        }

    }
}
