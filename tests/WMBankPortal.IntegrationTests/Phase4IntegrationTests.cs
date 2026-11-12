using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WMBankPortal.Application.Features.Applications.DTOs;
using WMBankPortal.Application.Features.Auth.DTOs;
using WMBankPortal.Application.Features.Notifications.DTOs;
using WMBankPortal.Application.Features.Products.DTOs;
using WMBankPortal.Domain.Common;
using Xunit;

namespace WMBankPortal.IntegrationTests;

public class Phase4IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private const string ValidSavingsFormJson =
        "{\"initialDeposit\": 25000, \"sourceOfFunds\": \"Salary\", \"debitCardTier\": \"Platinum Visa\", \"nomineeName\": \"Pooja Gupta\", \"nomineeRelationship\": \"Spouse\"}";

    public Phase4IntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task SubmitApplication_GeneratesNotification_And_CanBeReadViaApi()
    {
        var (client, _, user) = await LoginUserAsync("customer@wmportal.com", "BankDemo123!Secure#");

        // 1. Get products and submit application
        var productsRes = await client.GetAsync("/api/v1/products");
        var products = await productsRes.Content.ReadFromJsonAsync<List<BankingProductDto>>();
        Assert.NotNull(products);
        var product = products.First(p => p.Code == "SAV-01");

        var appRes = await client.PostAsJsonAsync("/api/v1/applications", new SubmitApplicationRequest(
            product.Id,
            25000m,
            ValidSavingsFormJson));
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);
        var app = await appRes.Content.ReadFromJsonAsync<BankingApplicationDto>();
        Assert.NotNull(app);

        // 2. Verify unread notification count is > 0
        var unreadRes = await client.GetAsync("/api/v1/notifications/unread-count");
        Assert.Equal(HttpStatusCode.OK, unreadRes.StatusCode);
        var unreadCount = await unreadRes.Content.ReadFromJsonAsync<UnreadNotificationCountDto>();
        Assert.NotNull(unreadCount);
        Assert.True(unreadCount.Count > 0);

        // 3. Fetch notifications list
        var notifListRes = await client.GetAsync("/api/v1/notifications?pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, notifListRes.StatusCode);
        var pagedNotifs = await notifListRes.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        Assert.NotNull(pagedNotifs);
        Assert.NotEmpty(pagedNotifs.Items);

        var latestNotif = pagedNotifs.Items.First(n => n.ApplicationId == app.Id);
        Assert.Equal("Application Submitted", latestNotif.Title);
        Assert.Equal("Success", latestNotif.Type);
        Assert.False(latestNotif.IsRead);

        // 4. Mark notification as read
        var markReadRes = await client.PutAsync($"/api/v1/notifications/{latestNotif.Id}/read", null);
        Assert.Equal(HttpStatusCode.OK, markReadRes.StatusCode);

        // 5. Verify notification now shows IsRead = true
        var verifyListRes = await client.GetAsync("/api/v1/notifications?pageNumber=1&pageSize=10");
        var updatedPaged = await verifyListRes.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        Assert.NotNull(updatedPaged);
        var updatedNotif = updatedPaged.Items.First(n => n.Id == latestNotif.Id);
        Assert.True(updatedNotif.IsRead);
        Assert.NotNull(updatedNotif.ReadAt);
    }

    [Fact]
    public async Task MarkAllAsRead_MarksAllUnreadNotificationsAsRead()
    {
        var (client, _, _) = await LoginUserAsync("customer@wmportal.com", "BankDemo123!Secure#");

        // Call read-all endpoint
        var readAllRes = await client.PutAsync("/api/v1/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.OK, readAllRes.StatusCode);

        // Verify unread count is now 0
        var countRes = await client.GetAsync("/api/v1/notifications/unread-count");
        var countDto = await countRes.Content.ReadFromJsonAsync<UnreadNotificationCountDto>();
        Assert.NotNull(countDto);
        Assert.Equal(0, countDto.Count);
    }

    [Fact]
    public async Task IDOR_Protection_CustomerBCannotMarkCustomerANotificationAsRead()
    {
        // 1. Customer A submits an application (triggering a notification for Customer A)
        var (clientA, _, _) = await LoginUserAsync("customer@wmportal.com", "BankDemo123!Secure#");

        var productsRes = await clientA.GetAsync("/api/v1/products");
        var products = await productsRes.Content.ReadFromJsonAsync<List<BankingProductDto>>();
        Assert.NotNull(products);
        var product = products.First(p => p.Code == "SAV-01");

        var appRes = await clientA.PostAsJsonAsync("/api/v1/applications", new SubmitApplicationRequest(
            product.Id,
            25000m,
            ValidSavingsFormJson));
        Assert.Equal(HttpStatusCode.OK, appRes.StatusCode);
        var app = await appRes.Content.ReadFromJsonAsync<BankingApplicationDto>();
        Assert.NotNull(app);

        var notifListRes = await clientA.GetAsync("/api/v1/notifications");
        var pagedNotifs = await notifListRes.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        Assert.NotNull(pagedNotifs);
        var notifA = pagedNotifs.Items.First(n => n.ApplicationId == app.Id);

        // 2. Register / Login Customer B
        var randomSuffix = Guid.NewGuid().ToString("N")[..8];
        var clientB = CreateClient();
        var emailB = $"customer_b_{randomSuffix}@wmportal.com";

        await clientB.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(emailB, "SecurePass123!#", "SecurePass123!#"));
        var loginBRes = await clientB.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(emailB, "SecurePass123!#"));
        var authB = await loginBRes.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authB);
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authB.AccessToken);

        // 3. Customer B attempts to mark Customer A's notification as read
        var idorRes = await clientB.PutAsync($"/api/v1/notifications/{notifA.Id}/read", null);

        // Must be rejected with HTTP 403 Forbidden!
        Assert.Equal(HttpStatusCode.Forbidden, idorRes.StatusCode);
    }
}