using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WMBankPortal.Application.Common.Interfaces;

namespace WMBankPortal.Infrastructure.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly string _connectionString;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private BlobServiceClient? _blobServiceClient;
    private BlobContainerClient? _containerClient;
    private readonly string _fallbackDirectory;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        _connectionString = configuration["AzureBlobStorage:ConnectionString"]
            ?? "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";
        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "kyc-documents";

        _fallbackDirectory = Path.Combine(AppContext.BaseDirectory, "LocalKycStorage");
        if (!Directory.Exists(_fallbackDirectory))
        {
            Directory.CreateDirectory(_fallbackDirectory);
        }
    }

    private async Task<BlobContainerClient?> GetContainerClientAsync()
    {
        if (_containerClient != null) return _containerClient;

        try
        {
            _blobServiceClient = new BlobServiceClient(_connectionString);
            _containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            return _containerClient;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to connect to Azurite/Azure Blob Storage. Using local disk fallback storage.");
            return null;
        }
    }

    public async Task<string> UploadAsync(
        Stream content,
        string blobName,
        string contentType,
        CancellationToken ct = default)
    {
        var container = await GetContainerClientAsync();
        if (container != null)
        {
            try
            {
                var blobClient = container.GetBlobClient(blobName);
                await blobClient.UploadAsync(content, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                }, ct);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload to Azure Blob. Writing to local fallback.");
            }
        }

        // Local fallback
        var localFilePath = Path.Combine(_fallbackDirectory, blobName);
        content.Position = 0;
        using (var fileStream = File.Create(localFilePath))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        return localFilePath;
    }

    public async Task<string> GenerateReadSasUrlAsync(
        string blobName,
        TimeSpan lifetime,
        CancellationToken ct = default)
    {
        var container = await GetContainerClientAsync();
        if (container != null)
        {
            try
            {
                var blobClient = container.GetBlobClient(blobName);
                if (blobClient.CanGenerateSasUri)
                {
                    var sasBuilder = new BlobSasBuilder
                    {
                        BlobContainerName = _containerName,
                        BlobName = blobName,
                        Resource = "b",
                        StartsOn = DateTimeOffset.UtcNow.AddMinutes(-1),
                        ExpiresOn = DateTimeOffset.UtcNow.Add(lifetime)
                    };
                    sasBuilder.SetPermissions(BlobSasPermissions.Read);
                    return blobClient.GenerateSasUri(sasBuilder).ToString();
                }

                // If Azurite without user delegation, return direct URL
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate SAS token on Azurite. Falling back to endpoint URL.");
            }
        }

        // Local fallback direct stream URL / placeholder
        return $"/api/v1/kyc/files/{blobName}";
    }

    public async Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        var container = await GetContainerClientAsync();
        if (container != null)
        {
            try
            {
                var blobClient = container.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete blob from Azure Storage.");
            }
        }

        var localPath = Path.Combine(_fallbackDirectory, blobName);
        if (File.Exists(localPath))
        {
            File.Delete(localPath);
        }
    }
}
