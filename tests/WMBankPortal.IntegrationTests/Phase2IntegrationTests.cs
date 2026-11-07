using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WMBankPortal.Application.Features.Applications.DTOs;
using WMBankPortal.Application.Features.Auth.DTOs;
using WMBankPortal.Application.Features.Products.DTOs;
using WMBankPortal.Application.Features.Profile.DTOs;
using WMBankPortal.Domain.Common;
using Xunit;

namespace WMBankPortal.IntegrationTests;

public class Phase2IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Phase2IntegrationTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task GetProducts_WhenAuthenticated_ReturnsSeededBankingProducts()
    {
        var (client, _, _) = await LoginUserAsync("customer@wmportal.com", "BankDemo123!Secure#");

        var response = await client.GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<BankingProductDto>>();
        Assert.NotNull(products);
        Assert.True(products.Count >= 4);
        Assert.Contains(products, p => p.Code == "SAV-01");
        Assert.Contains(products, p => p.Code == "CC-PLAT");
        Assert.Contains(products, p => p.Code == "LOAN-PERS");
        Assert.Contains(products, p => p.Code == "FD-GROWTH");
    }

    [Fact]
    public async Task CustomerProfile_GetAndPut_UpdatesDemographicsSuccessfully()
    {
        var (client, _, _) = await LoginUserAsync("customer@wmportal.com", "BankDemo123!Secure#");

        // 1. Get current profile
        var getResponse = await client.GetAsync("/api/v1/profile/me");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // 2. Update profile
        var updateRequest = new UpdateProfileRequest(
            FirstName: "Daksh",
            LastName: "Gupta",
            PhoneNumber: "+91 91234 56789",
            DateOfBirth: new DateTime(1995, 5, 11),
            AddressLine1: "45 Marine Boulevard",
            City: "Mumbai",
            PostalCode: "400021",
            Country: "India");

        var putResponse = await client.PutAsJsonAsync("/api/v1/profile/me", updateRequest);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updatedProfile = await putResponse.Content.ReadFromJsonAsync<CustomerProfileDto>();
        Assert.NotNull(updatedProfile);
        Assert.Equal("+91 91234 56789", updatedProfile.PhoneNumber);
        Assert.Equal("45 Marine Boulevard", updatedProfile.AddressLine1);
    }

    [Fact]
    public async Task SubmitApplication_And_IdorProtection_EnforcesStrictCustomerBoundary()
    {
        // 1. Customer A submits an application for Savings Account
        var (clientA, _, userA) = await LoginUserAsync("customer@wmportal.com", "BankDemo123!Secure#");

        var productsResponse = await clientA.GetAsync("/api/v1/products");
        var products = await productsResponse.Content.ReadFromJsonAsync<List<BankingProductDto>>();
        Assert.NotNull(products);
        var savingsProduct = products.First(p => p.Code == "SAV-01");

        var submitRequest = new SubmitApplicationRequest(
            ProductId: savingsProduct.Id,
            RequestedAmount: 25000m,
            FormValuesJson: "{\"initialDeposit\": 25000, \"sourceOfFunds\": \"Salary\", \"debitCardTier\": \"Platinum Visa\", \"nomineeName\": \"Pooja Gupta\", \"nomineeRelationship\": \"Spouse\"}");

        var submitResponse = await clientA.PostAsJsonAsync("/api/v1/applications", submitRequest);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var appDto = await submitResponse.Content.ReadFromJsonAsync<BankingApplicationDto>();
        Assert.NotNull(appDto);
        Assert.StartsWith("APP-", appDto.ApplicationNumber);
        Assert.Equal("Submitted", appDto.Status);

        // 2. Customer A can view their own application
        var viewResponseA = await clientA.GetAsync($"/api/v1/applications/{appDto.Id}");
        Assert.Equal(HttpStatusCode.OK, viewResponseA.StatusCode);

        // 3. Register Customer B
        var randomId = Guid.NewGuid().ToString("N")[..8];
        var customerBEmail = $"customer_b_{randomId}@test.com";
        var clientB = CreateClient();

        var regResponse = await clientB.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(customerBEmail, "SecurePass123!#", "SecurePass123!#"));
        Assert.Equal(HttpStatusCode.OK, regResponse.StatusCode);

        var loginBResponse = await clientB.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(customerBEmail, "SecurePass123!#"));
        var authB = await loginBResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authB);
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authB.AccessToken);

        // 4. IDOR Defense Verification: Customer B attempts to access Customer A's application
        var idorAttackResponse = await clientB.GetAsync($"/api/v1/applications/{appDto.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, idorAttackResponse.StatusCode);

        // Customer B also cannot access Customer A's timeline
        var idorTimelineResponse = await clientB.GetAsync($"/api/v1/applications/{appDto.Id}/timeline");
        Assert.Equal(HttpStatusCode.Forbidden, idorTimelineResponse.StatusCode);

        // Customer B also cannot withdraw Customer A's application
        var idorWithdrawResponse = await clientB.PostAsJsonAsync($"/api/v1/applications/{appDto.Id}/withdraw",
            new WithdrawApplicationRequest("Attacker withdrawal attempt"));
        Assert.Equal(HttpStatusCode.Forbidden, idorWithdrawResponse.StatusCode);

        // 5. Loan Officer CAN view Customer A's application
        var (officerClient, _, _) = await LoginUserAsync("officer@wmportal.com", "BankDemo123!Secure#");
        var officerViewResponse = await officerClient.GetAsync($"/api/v1/applications/{appDto.Id}");
        Assert.Equal(HttpStatusCode.OK, officerViewResponse.StatusCode);
    }
}
