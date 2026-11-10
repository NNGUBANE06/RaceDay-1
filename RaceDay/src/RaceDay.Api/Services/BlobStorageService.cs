using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay.Api.Services;

public sealed class BlobStorageService
{
    private readonly string _connectionString;
    private readonly string _eventContainer;
    private readonly string _profileContainer;

    public BlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureBlobStorage:ConnectionString"] ?? "";
        _eventContainer = configuration["AzureBlobStorage:EventContainer"] ?? "event-banners";
        _profileContainer = configuration["AzureBlobStorage:ProfileContainer"] ?? "profile-images";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<string> UploadAsync(IFormFile file, string kind, CancellationToken cancellationToken)
    {
        if (!IsConfigured) throw new InvalidOperationException("Azure Blob Storage is not configured. Set AzureBlobStorage:ConnectionString.");
        var containerName = kind == "event" ? _eventContainer : _profileContainer;
        var container = new BlobContainerClient(_connectionString, containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType }, cancellationToken: cancellationToken);
        return blobName;
    }

    public string? GetReadUrl(string? blobName, string kind)
    {
        if (string.IsNullOrWhiteSpace(blobName) || !IsConfigured) return null;
        var containerName = kind == "event" ? _eventContainer : _profileContainer;
        var service = new BlobServiceClient(_connectionString);
        var blob = service.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        var credential = new Azure.Storage.StorageSharedKeyCredential(
            BlobConnectionStringValue("AccountName"), BlobConnectionStringValue("AccountKey"));
        if (!blob.CanGenerateSasUri) return blob.Uri.ToString();
        var sas = new BlobSasBuilder { BlobContainerName = containerName, BlobName = blobName, Resource = "b", ExpiresOn = DateTimeOffset.UtcNow.AddHours(2) };
        sas.SetPermissions(BlobSasPermissions.Read);
        return blob.GenerateSasUri(sas).ToString();
    }

    private string BlobConnectionStringValue(string key)
    {
        var pair = _connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => x.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
        return pair is null ? "" : pair[(pair.IndexOf('=') + 1)..];
    }
}