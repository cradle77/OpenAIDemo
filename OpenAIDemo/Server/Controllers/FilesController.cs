using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAIDemo.Server.Model;

namespace OpenAIDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private AzureConfig _config;

        public FilesController(IOptions<AzureConfig> config)
        {
            _config = config.Value;
        }

        [HttpPost("{sessionId}/upload")]
        public IActionResult UploadNewFile(Guid sessionId, [FromBody]string[] fileNames)
        {
            var blobServiceClient = new BlobServiceClient(new Uri(_config.Adls.StorageEndpoint), new VisualStudioCredential(
                    new VisualStudioCredentialOptions
                    {
                        TenantId = _config.TenantId
                    }));

            var results = new Dictionary<string, string>();

            foreach (var fileName in fileNames)
            {
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(_config.Adls.ImageContainerName);

                var blobClient = blobContainerClient.GetBlobClient($"{sessionId}/{fileName}");

                var userDelegationKey = blobServiceClient.GetUserDelegationKey(DateTimeOffset.UtcNow.AddSeconds(-30),
                                                                        DateTimeOffset.UtcNow.AddHours(2));

                BlobSasBuilder sas = new BlobSasBuilder()
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b", // b for blob, c for container
                    StartsOn = DateTimeOffset.UtcNow.AddSeconds(-30),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(2),
                };

                sas.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create | BlobSasPermissions.Read);

                var sasToken = sas.ToSasQueryParameters(userDelegationKey, _config.Adls.AccountName).ToString();

                results.Add(fileName, $"{blobClient.Uri}?{sasToken}");
            }

            return this.Ok(results);
        }
    }
}