using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NexusERP.Tests.Integration;

public class CustomerApiNotFoundTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerApiNotFoundTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCustomerById_WithUnknownId_ShouldReturnNotFoundProblemDetails()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var missingCustomerId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/customers/{missingCustomerId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("status", out var statusProperty));
        Assert.Equal(404, statusProperty.GetInt32());

        Assert.True(root.TryGetProperty("title", out var titleProperty));
        Assert.False(string.IsNullOrWhiteSpace(titleProperty.GetString()));

        Assert.True(root.TryGetProperty("type", out var typeProperty));
        Assert.False(string.IsNullOrWhiteSpace(typeProperty.GetString()));

        var getAllResponse = await client.GetAsync("/api/customers");
        var content = await getAllResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);
        Assert.Equal("[]", content.Trim());
    }
}
