using WMBankPortal.Domain.Common;

namespace WMBankPortal.Application.Features.Kyc.Services;

public static class KycFileValidator
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private static readonly byte[] PdfMagic = [0x25, 0x50, 0x44, 0x46]; // %PDF
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47]; // .PNG

    public static Result Validate(string fileName, long length, Stream stream)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Failure(Error.Validation("Kyc.FileNameEmpty", "File name cannot be empty."));
        }

        // 1. File size check
        if (length <= 0)
        {
            return Result.Failure(Error.Validation("Kyc.FileEmpty", "Uploaded file cannot be empty."));
        }

        if (length > MaxFileSizeBytes)
        {
            return Result.Failure(Error.Validation("Kyc.FileSizeExceeded",
                $"File size ({length / (1024 * 1024.0):F2} MB) exceeds the 5 MB limit."));
        }

        // 2. Double-extension defense (e.g. "kyc.pdf.exe" or "id.exe.jpg")
        var withoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (withoutExt.Contains('.'))
        {
            return Result.Failure(Error.Validation("Kyc.DoubleExtensionBlocked",
                "Suspicious multiple file extensions detected. Upload rejected for security."));
        }

        // 3. Extension check
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            return Result.Failure(Error.Validation("Kyc.InvalidExtension",
                $"File extension '{ext}' is not allowed. Only .pdf, .jpg, .jpeg, and .png are permitted."));
        }

        // 4. Magic bytes inspection
        if (!stream.CanSeek)
        {
            return Result.Failure(Error.Failure("Kyc.StreamNotSeekable", "File stream must be seekable for security inspection."));
        }

        var header = new byte[8];
        var originalPosition = stream.Position;
        stream.Position = 0;
        var bytesRead = stream.Read(header, 0, header.Length);
        stream.Position = originalPosition; // Reset position for subsequent consumers

        if (bytesRead < 4)
        {
            return Result.Failure(Error.Validation("Kyc.CorruptHeader", "File is too small to have a valid header."));
        }

        bool magicValid = ext.ToLowerInvariant() switch
        {
            ".pdf" => header.Take(4).SequenceEqual(PdfMagic),
            ".jpg" or ".jpeg" => header.Take(3).SequenceEqual(JpegMagic),
            ".png" => header.Take(4).SequenceEqual(PngMagic),
            _ => false
        };

        if (!magicValid)
        {
            return Result.Failure(Error.Validation("Kyc.MagicBytesMismatch",
                "File contents do not match the expected signature for its extension (possible spoofed extension)."));
        }

        return Result.Success();
    }
}
