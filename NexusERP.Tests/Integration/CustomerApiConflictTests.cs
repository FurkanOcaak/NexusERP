using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NexusERP.Application.Features.Customers.DTOs;

namespace NexusERP.Tests.Integration;

public class CustomerApiConflictTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerApiConflictTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCustomer_WithDuplicateCode_ShouldReturnConflictAndKeepOnlyOriginalRecord()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var firstRequest = new CustomerCreateRequest
        {
            Code = "INT-CONFLICT-001",
            Name = "First Customer",
            Email = "first@example.com",
            Phone = "03121234567",
            TaxNumber = "1234567890",
            Address = "Ankara"
        };

        var firstResponse = await client.PostAsJsonAsync("/api/customers", firstRequest);
        var firstCreatedCustomer = await firstResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.NotNull(firstCreatedCustomer);

        var firstCustomerId = firstCreatedCustomer!.Id;
        var firstCustomerCode = firstCreatedCustomer.Code;

        var duplicateRequest = new CustomerCreateRequest
        {
            Code = "INT-CONFLICT-001",
            Name = "Second Customer",
            Email = "second@example.com",
            Phone = "03129876543",
            TaxNumber = "9876543210",
            Address = "İstanbul"
        };

        var duplicateResponse = await client.PostAsJsonAsync("/api/customers", duplicateRequest);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal("application/problem+json", duplicateResponse.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await duplicateResponse.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("status", out var statusProperty));
        Assert.Equal(409, statusProperty.GetInt32());

        Assert.True(root.TryGetProperty("title", out var titleProperty));
        Assert.False(string.IsNullOrWhiteSpace(titleProperty.GetString()));

        Assert.True(root.TryGetProperty("code", out var codeProperty));
        Assert.Equal("INT-CONFLICT-001", codeProperty.GetString());

        var getAllResponse = await client.GetAsync("/api/customers");
        var customers = await getAllResponse.Content.ReadFromJsonAsync<List<CustomerResponse>>();

        Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);
        Assert.NotNull(customers);
        Assert.Single(customers!);

        var onlyCustomer = customers![0];
        Assert.Equal(firstCustomerId, onlyCustomer.Id);
        Assert.Equal(firstCustomerCode, onlyCustomer.Code);
        Assert.Equal("First Customer", onlyCustomer.Name);
    }
}
