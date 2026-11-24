using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, string publicBaseUrl, CancellationToken ct);
}

/// <summary>Stores files in an Azure Blob Storage container. Used when AzureBlob:ConnectionString is configured.</summary>
public class AzureBlobFileStorage : IFileStorage
{
    private readonly IConfiguration _config;
    private readonly ILogger<AzureBlobFileStorage> _logger;

    public AzureBlobFileStorage(IConfiguration config, ILogger<AzureBlobFileStorage> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, string publicBaseUrl, CancellationToken ct)
    {
        var container = new BlobContainerClient(
            _config["AzureBlob:ConnectionString"],
            _config["AzureBlob:ContainerName"] ?? "raceday-images");

        try
        {
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        }
        catch (RequestFailedException ex)
        {
            // Some storage accounts block public access. Fall back to a private container.
            _logger.LogWarning("Could not create a public container ({Message}). Using a private container; images will not be publicly viewable.", ex.Message);
            await container.CreateIfNotExistsAsync(cancellationToken: ct);
        }

        var blob = container.GetBlobClient(fileName);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blob.Uri.ToString();
    }
}

/// <summary>Fallback used when Azure Blob Storage is not configured: saves to wwwroot/uploads.</summary>
public class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;
    public LocalFileStorage(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, string publicBaseUrl, CancellationToken ct)
    {
        var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(root, "uploads");
        Directory.CreateDirectory(folder);
        await using var fs = File.Create(Path.Combine(folder, fileName));
        await content.CopyToAsync(fs, ct);
        return $"{publicBaseUrl.TrimEnd('/')}/uploads/{fileName}";
    }
}
