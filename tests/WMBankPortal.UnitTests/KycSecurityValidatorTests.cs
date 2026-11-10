using WMBankPortal.Application.Features.Kyc.Services;
using WMBankPortal.Domain.Common;
using Xunit;

namespace WMBankPortal.UnitTests;

public class KycSecurityValidatorTests
{
    private static MemoryStream CreateStreamWithMagic(byte[] magic, int totalLength = 1024)
    {
        var ms = new MemoryStream();
        ms.Write(magic, 0, magic.Length);
        var remaining = new byte[totalLength - magic.Length];
        ms.Write(remaining, 0, remaining.Length);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Validate_ValidPdfWithMagicBytes_ReturnsSuccess()
    {
        byte[] pdfMagic = [0x25, 0x50, 0x44, 0x46]; // %PDF
        using var stream = CreateStreamWithMagic(pdfMagic);

        var result = KycFileValidator.Validate("passport.pdf", stream.Length, stream);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_ValidJpegWithMagicBytes_ReturnsSuccess()
    {
        byte[] jpegMagic = [0xFF, 0xD8, 0xFF];
        using var stream = CreateStreamWithMagic(jpegMagic);

        var result = KycFileValidator.Validate("photo.jpg", stream.Length, stream);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_ValidPngWithMagicBytes_ReturnsSuccess()
    {
        byte[] pngMagic = [0x89, 0x50, 0x4E, 0x47]; // .PNG
        using var stream = CreateStreamWithMagic(pngMagic);

        var result = KycFileValidator.Validate("address_proof.png", stream.Length, stream);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_SpoofedPdfWithFakeMagic_ReturnsFailure()
    {
        byte[] fakeMagic = [0x4D, 0x5A, 0x90, 0x00]; // Windows MZ executable signature disguised as PDF!
        using var stream = CreateStreamWithMagic(fakeMagic);

        var result = KycFileValidator.Validate("malicious.pdf", stream.Length, stream);

        Assert.True(result.IsFailure);
        Assert.Equal("Kyc.MagicBytesMismatch", result.Error.Code);
    }

    [Theory]
    [InlineData("document.exe")]
    [InlineData("script.bat")]
    [InlineData("exploit.sh")]
    [InlineData("invoice.docx")]
    public void Validate_DisallowedExtensions_ReturnsFailure(string fileName)
    {
        byte[] dummy = [0x00, 0x01, 0x02, 0x03];
        using var stream = CreateStreamWithMagic(dummy);

        var result = KycFileValidator.Validate(fileName, stream.Length, stream);

        Assert.True(result.IsFailure);
        Assert.Equal("Kyc.InvalidExtension", result.Error.Code);
    }

    [Theory]
    [InlineData("document.pdf.exe")]
    [InlineData("id_card.bat.png")]
    [InlineData("photo.jpg.sh")]
    public void Validate_DoubleExtensions_ReturnsFailure(string fileName)
    {
        byte[] pdfMagic = [0x25, 0x50, 0x44, 0x46];
        using var stream = CreateStreamWithMagic(pdfMagic);

        var result = KycFileValidator.Validate(fileName, stream.Length, stream);

        Assert.True(result.IsFailure);
        Assert.Equal("Kyc.DoubleExtensionBlocked", result.Error.Code);
    }

    [Fact]
    public void Validate_FileExceeding5Mb_ReturnsFailure()
    {
        byte[] pdfMagic = [0x25, 0x50, 0x44, 0x46];
        using var stream = CreateStreamWithMagic(pdfMagic, 100);

        long sixMb = 6 * 1024 * 1024;
        var result = KycFileValidator.Validate("huge_file.pdf", sixMb, stream);

        Assert.True(result.IsFailure);
        Assert.Equal("Kyc.FileSizeExceeded", result.Error.Code);
    }

    [Fact]
    public void Validate_EmptyFile_ReturnsFailure()
    {
        using var emptyStream = new MemoryStream();

        var result = KycFileValidator.Validate("empty.pdf", 0, emptyStream);

        Assert.True(result.IsFailure);
        Assert.Equal("Kyc.FileEmpty", result.Error.Code);
    }
}
