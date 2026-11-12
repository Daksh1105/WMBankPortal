using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WMBankPortal.Application.Features.Auth.DTOs;
using Xunit;

namespace WMBankPortal.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
            builder.UseSetting("RateLimiting:GlobalPermitLimit", "1000");
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true // Automatically tracks HttpOnly cookies
        });
        _client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccessWithJwtToken()
    {
        // Arrange
        var uniqueEmail = $"test_{Guid.NewGuid():N}@wmportal.com";
        var request = new RegisterRequest(uniqueEmail, "Password123!Secure", "Password123!Secure");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));
        Assert.Equal(uniqueEmail, authResponse.User.Email);
        Assert.Equal("Customer", authResponse.User.Role);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndSetsHttpOnlyCookie()
    {
        // Arrange: Register a fresh user
        var uniqueEmail = $"login_{Guid.NewGuid():N}@wmportal.com";
        var password = "Password123!Secure";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(uniqueEmail, password, password));

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(uniqueEmail, password));

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));

        // Verify Set-Cookie header contains refreshToken and is HttpOnly + SameSite=Strict
        Assert.True(loginResponse.Headers.Contains("Set-Cookie"));
        var setCookieHeader = loginResponse.Headers.GetValues("Set-Cookie").First();
        Assert.Contains("refreshToken=", setCookieHeader);
        Assert.Contains("httponly", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithFiveConsecutiveFailedAttempts_LocksOutAccount()
    {
        // Arrange: Register user
        var uniqueEmail = $"lockout_{Guid.NewGuid():N}@wmportal.com";
        var correctPassword = "Password123!Secure";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(uniqueEmail, correctPassword, correctPassword));

        // Act: Fail login 5 times
        HttpResponseMessage lastResponse = null!;
        for (int i = 0; i < 5; i++)
        {
            lastResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(uniqueEmail, "WrongPassword123!"));
        }

        // 6th attempt with CORRECT password must still be rejected due to lockout
        var attemptAfterLockout = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(uniqueEmail, correctPassword));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, attemptAfterLockout.StatusCode);
        var responseContent = await attemptAfterLockout.Content.ReadFromJsonAsync<object>();
        Assert.NotNull(responseContent);
        var rawContent = await attemptAfterLockout.Content.ReadAsStringAsync();
        Assert.Contains("Account is locked", rawContent);
    }

    [Fact]
    public async Task RefreshToken_WithValidCookie_ReturnsNewAccessTokenAndRotatesCookie()
    {
        // Arrange: Register and login to establish session cookie
        var uniqueEmail = $"refresh_{Guid.NewGuid():N}@wmportal.com";
        var password = "Password123!Secure";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(uniqueEmail, password, password));
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(uniqueEmail, password));
        var initialCookie = loginResponse.Headers.GetValues("Set-Cookie").First();

        // Act: Call refresh endpoint
        var refreshResponse = await _client.PostAsync("/api/v1/auth/refresh", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var authResponse = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));

        // Verify that a new Set-Cookie was issued (rotation)
        Assert.True(refreshResponse.Headers.Contains("Set-Cookie"));
        var newCookie = refreshResponse.Headers.GetValues("Set-Cookie").First();
        Assert.NotEqual(initialCookie, newCookie);
    }
}
