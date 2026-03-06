using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FinanceTracker.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;

    public AzureBlobStorageService(IConfiguration config)
    {
        var connectionString = config["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "AzureStorage:ConnectionString is not configured.");
        _client = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream, string fileName, string containerName)
    {
        var container = _client.GetBlobContainerClient(containerName);

        // Create container if it doesn't exist (public blob read access)
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blob = container.GetBlobClient(fileName);
        await blob.UploadAsync(fileStream, overwrite: true);

        return blob.Uri.ToString();
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        var uri = new Uri(fileUrl);
        var blobName = string.Join("/", uri.Segments[2..]);   // skip host + container
        var container = uri.Segments[1].TrimEnd('/');

        var containerClient = _client.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }
}