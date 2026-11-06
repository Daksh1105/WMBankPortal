using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace WMBankPortal.IntegrationTests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task GetHealth_ReturnsOkStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_ShouldIncludeSecurityHeadersAndCorrelationId()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert Security Headers
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());

        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.GetValues("X-Correlation-ID").First()));
    }
}
