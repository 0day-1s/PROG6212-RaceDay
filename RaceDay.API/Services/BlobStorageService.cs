using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services;

public class BlobStorageService
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration configuration)
    {
        string connectionString = configuration["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Blob connection string missing.");

        string containerName = configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";

        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file was supplied.");

        if (file.Length > MaxBytes)
            throw new ArgumentException("The image must be 5 MB or smaller.");

        if (!AllowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new ArgumentException("Only JPG, PNG, WEBP or GIF images are allowed.");

        string extension = Path.GetExtension(file.FileName);
        string blobName = $"{folder}/{Guid.NewGuid()}{extension}";

        // Private container: images are served through the API, not by public URL.
        await _container.CreateIfNotExistsAsync(PublicAccessType.None);

        BlobClient blob = _container.GetBlobClient(blobName);

        await using Stream stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blob.Uri.ToString();
    }

    public async Task<(Stream Content, string ContentType)?> OpenReadAsync(string blobName)
    {
        BlobClient blob = _container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync())
            return null;

        var download = await blob.DownloadStreamingAsync();
        return (download.Value.Content, download.Value.Details.ContentType);
    }
}