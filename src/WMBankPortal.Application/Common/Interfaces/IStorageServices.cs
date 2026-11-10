using WMBankPortal.Domain.Common;

namespace WMBankPortal.Application.Common.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(
        Stream content,
        string blobName,
        string contentType,
        CancellationToken ct = default);

    Task<string> GenerateReadSasUrlAsync(
        string blobName,
        TimeSpan lifetime,
        CancellationToken ct = default);

    Task DeleteAsync(string blobName, CancellationToken ct = default);
}

public interface IMalwareScanner
{
    Task<Result> ScanAsync(Stream content, string fileName, CancellationToken ct = default);
}
