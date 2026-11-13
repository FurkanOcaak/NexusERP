using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NexusERP.Application.Features.Customers.DTOs;

namespace NexusERP.Tests.Integration;

public class CustomerApiValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerApiValidationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCustomer_WithInvalidRequest_ShouldReturnBadRequestAndNoPersistence()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var invalidRequest = new
        {
            code = "",
            name = "",
            email = "gecersiz-email",
            phone = "03121234567",
            taxNumber = "1234567890",
            address = "Ankara"
        };

        var response = await client.PostAsJsonAsync("/api/customers", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("status", out var statusProperty));
        Assert.Equal(400, statusProperty.GetInt32());

        Assert.True(root.TryGetProperty("errors", out var errorsProperty));
        Assert.Equal(JsonValueKind.Object, errorsProperty.ValueKind);

        var errorEntries = errorsProperty.EnumerateObject().ToList();
        Assert.NotEmpty(errorEntries);

        var hasCodeError = false;
        var hasNameError = false;
        var hasEmailError = false;

        foreach (var errorEntry in errorEntries)
        {
            var key = errorEntry.Name;
            Assert.Equal(JsonValueKind.Array, errorEntry.Value.ValueKind);

            var messages = errorEntry.Value.EnumerateArray().Select(x => x.GetString()).ToList();
            Assert.NotEmpty(messages);
            Assert.All(messages, message => Assert.False(string.IsNullOrWhiteSpace(message)));

            if (string.Equals(key, "Code", StringComparison.OrdinalIgnoreCase))
            {
                hasCodeError = true;
            }
            else if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase))
            {
                hasNameError = true;
            }
            else if (string.Equals(key, "Email", StringComparison.OrdinalIgnoreCase))
            {
                hasEmailError = true;
            }
        }

        Assert.True(hasCodeError, "Code validation errors were expected.");
        Assert.True(hasNameError, "Name validation errors were expected.");
        Assert.True(hasEmailError, "Email validation errors were expected.");

        var getResponse = await client.GetAsync("/api/customers");
        var customers = await getResponse.Content.ReadFromJsonAsync<List<CustomerResponse>>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(customers);
        Assert.Empty(customers!);
    }
}
