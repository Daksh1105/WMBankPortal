using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WMBankPortal.Application.Common.Interfaces;
using WMBankPortal.Application.Features.Applications.DTOs;
using WMBankPortal.Application.Features.Auth.DTOs;
using WMBankPortal.Application.Features.Kyc.DTOs;
using WMBankPortal.Application.Features.Products.DTOs;
using WMBankPortal.Domain.Entities;
using WMBankPortal.Domain.Enums;
using Xunit;

namespace WMBankPortal.IntegrationTests;

public class Phase3IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Phase3IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    private async Task<(HttpClient Client, string Token, UserDto User)> LoginUserAsync(string email, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
        return (client, authResponse.AccessToken, authResponse.User);
    }

    private static ByteArrayContent CreatePdfFileContent(string fileName)
    {
        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37, 0x0A, 0x25, 0xE2, 0xE3, 0xCF, 0xD3];
        var content = new ByteArrayContent(pdfBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        return content;
    }

    [Fact]
    public async Task UploadKycDocument_WithValidPdf_PersistsMetadataAndAllowsOfficerVerification()
    {
        var (client, _, _) = await LoginUserAsync("customer@wmportal.com", "BankDemo123!Secure#");

        // 1. Get savings product and create an application
        var productsRes = await client.GetAsync("/api/v1/products");
        var products = await productsRes.Content.ReadFromJsonAsync<List<BankingProductDto>>();
        Assert.NotNull(products);
        var product = products.First();

        var appRes = await client.PostAsJsonAsync("/api/v1/applications", new SubmitApplicationRequest(
            product.Id,
            product.MinAmount,
            "{\"testField\": \"value\"}"));
        var app = await appRes.Content.ReadFromJsonAsync<BankingApplicationDto>();
        Assert.NotNull(app);

        // 2. Upload ID Proof PDF
        using var form = new MultipartFormDataContent();
        form.Add(CreatePdfFileContent("government_id.pdf"), "file", "government_id.pdf");
        form.Add(new StringContent(app.Id.ToString()), "applicationId");
        form.Add(new StringContent("IdProof"), "documentType");

        var uploadRes = await client.PostAsync("/api/v1/kyc/upload", form);
        Assert.Equal(HttpStatusCode.OK, uploadRes.StatusCode);

        var docDto = await uploadRes.Content.ReadFromJsonAsync<KycDocumentDto>();
        Assert.NotNull(docDto);
        Assert.Equal("IdProof", docDto.DocumentType);
        Assert.Equal("Pending", docDto.Status);
        Assert.Equal("Clean", docDto.ScanStatus);

        // 3. Customer retrieves download SAS token
        var sasRes = await client.GetAsync($"/api/v1/kyc/{docDto.Id}/download-token");
        Assert.Equal(HttpStatusCode.OK, sasRes.StatusCode);
        var sasDto = await sasRes.Content.ReadFromJsonAsync<KycDownloadTokenDto>();
        Assert.NotNull(sasDto);
        Assert.NotEmpty(sasDto.DownloadUrl);

        // 4. Officer verifies document
        var (officerClient, _, _) = await LoginUserAsync("officer@wmportal.com", "BankDemo123!Secure#");
        var verifyRes = await officerClient.PostAsJsonAsync($"/api/v1/kyc/{docDto.Id}/verify",
            new VerifyKycDocumentRequest("Verified", null));
        Assert.Equal(HttpStatusCode.OK, verifyRes.StatusCode);

        var verifiedDoc = await verifyRes.Content.ReadFromJsonAsync<KycDocumentDto>();
        Assert.NotNull(verifiedDoc);
        Assert.Equal("Verified", verifiedDoc.Status);
    }

    [Fact]
    public async Task UploadKyc_WhenApplicationIsKycPending_AutoTransitionsToUnderReview()
    {
        var (client, _, _) = await LoginUserAsync("customer@wmportal.com", "BankDemo123!Secure#");

        // 1. Submit application
        var productsRes = await client.GetAsync("/api/v1/products");
        var products = await productsRes.Content.ReadFromJsonAsync<List<BankingProductDto>>();
        Assert.NotNull(products);

        var appRes = await client.PostAsJsonAsync("/api/v1/applications", new SubmitApplicationRequest(
            products.First().Id,
            products.First().MinAmount,
            "{}"));
        var app = await appRes.Content.ReadFromJsonAsync<BankingApplicationDto>();
        Assert.NotNull(app);

        // 2. Put application into KycPending using EF direct access to simulate officer underwriting step
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<WMBankPortal.Infrastructure.Identity.ApplicationUser>>();
            var officer = await userManager.FindByEmailAsync("officer@wmportal.com");
            Assert.NotNull(officer);

            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var dbApp = await db.BankingApplications.FirstAsync(a => a.Id == app.Id);
            dbApp.Claim(officer.Id);
            await db.SaveChangesAsync();
            dbApp.RequestKyc(officer.Id, "Need address proof re-upload");
            await db.SaveChangesAsync();
        }

        // Verify status is KycPending
        var checkAppRes = await client.GetAsync($"/api/v1/applications/{app.Id}");
        var checkApp = await checkAppRes.Content.ReadFromJsonAsync<BankingApplicationDto>();
        Assert.NotNull(checkApp);
        Assert.Equal("KycPending", checkApp.Status);

        // 3. Customer uploads address proof
        using var form = new MultipartFormDataContent();
        form.Add(CreatePdfFileContent("utility_bill.pdf"), "file", "utility_bill.pdf");
        form.Add(new StringContent(app.Id.ToString()), "applicationId");
        form.Add(new StringContent("AddressProof"), "documentType");

        var uploadRes = await client.PostAsync("/api/v1/kyc/upload", form);
        Assert.Equal(HttpStatusCode.OK, uploadRes.StatusCode);

        // 4. Verify application has auto-transitioned back to UnderReview!
        var afterUploadAppRes = await client.GetAsync($"/api/v1/applications/{app.Id}");
        var afterUploadApp = await afterUploadAppRes.Content.ReadFromJsonAsync<BankingApplicationDto>();
        Assert.NotNull(afterUploadApp);
        Assert.Equal("UnderReview", afterUploadApp.Status);
    }

    [Fact]
    public async Task IDOR_Protection_CustomerBCannotAccessCustomerAKycDocuments()
    {
        // 1. Customer A uploads KYC file
        var (clientA, _, _) = await LoginUserAsync("customer@wmportal.com", "BankDemo123!Secure#");

        var productsRes = await clientA.GetAsync("/api/v1/products");
        var products = await productsRes.Content.ReadFromJsonAsync<List<BankingProductDto>>();
        Assert.NotNull(products);

        var appRes = await clientA.PostAsJsonAsync("/api/v1/applications", new SubmitApplicationRequest(
            products.First().Id,
            products.First().MinAmount,
            "{}"));
        var appA = await appRes.Content.ReadFromJsonAsync<BankingApplicationDto>();
        Assert.NotNull(appA);

        using var form = new MultipartFormDataContent();
        form.Add(CreatePdfFileContent("pan_card.pdf"), "file", "pan_card.pdf");
        form.Add(new StringContent(appA.Id.ToString()), "applicationId");
        form.Add(new StringContent("IdProof"), "documentType");

        var uploadRes = await clientA.PostAsync("/api/v1/kyc/upload", form);
        Assert.Equal(HttpStatusCode.OK, uploadRes.StatusCode);
        var docA = await uploadRes.Content.ReadFromJsonAsync<KycDocumentDto>();
        Assert.NotNull(docA);

        // 2. Register Customer B
        var randomId = Guid.NewGuid().ToString("N")[..8];
        var customerBEmail = $"kyc_attacker_{randomId}@test.com";
        var clientB = CreateClient();

        await clientB.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(customerBEmail, "SecurePass123!#", "SecurePass123!#"));

        var loginB = await clientB.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(customerBEmail, "SecurePass123!#"));
        var authB = await loginB.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authB);
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authB.AccessToken);

        // 3. IDOR Attack: Customer B tries to view Customer A's application documents -> 403 Forbidden!
        var idorListRes = await clientB.GetAsync($"/api/v1/kyc/application/{appA.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, idorListRes.StatusCode);

        // 4. IDOR Attack: Customer B tries to generate download token for Customer A's document -> 403 Forbidden!
        var idorTokenRes = await clientB.GetAsync($"/api/v1/kyc/{docA.Id}/download-token");
        Assert.Equal(HttpStatusCode.Forbidden, idorTokenRes.StatusCode);
    }
}
