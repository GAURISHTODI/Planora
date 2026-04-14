using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PlanoraApp.API.Services.VaultService
{
    public interface IBlobStorageService
    {
        Task<string> UploadDocumentAsync(string fileName, Stream fileStream, string contentType);
        Task<bool> DeleteDocumentAsync(string blobUrl);
    }

    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration config)
        {
            var connectionString = config["AzureStorage:ConnectionString"];
            var blobServiceClient = new BlobServiceClient(connectionString);
            
            // Following PROJECT_BIBLE Step 8: Container named "vault-documents"
            _containerClient = blobServiceClient.GetBlobContainerClient("vault-documents");
            
            // Ensure the container exists
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }

        public async Task<string> UploadDocumentAsync(string fileName, Stream fileStream, string contentType)
        {
            var uniqueName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = _containerClient.GetBlobClient(uniqueName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };

            fileStream.Position = 0;
            await blobClient.UploadAsync(fileStream, options);

            return blobClient.Uri.ToString();
        }

        public async Task<bool> DeleteDocumentAsync(string blobUrl)
        {
            try 
            {
                var uri = new Uri(blobUrl);
                var blobName = Path.GetFileName(uri.LocalPath);
                var blobClient = _containerClient.GetBlobClient(blobName);
                
                var response = await blobClient.DeleteIfExistsAsync();
                return response.Value;
            }
            catch
            {
                return false;
            }
        }
    }
}
