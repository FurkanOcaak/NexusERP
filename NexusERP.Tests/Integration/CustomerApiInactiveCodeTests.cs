using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusERP.Application.Features.Customers.DTOs;
using NexusERP.DataAccess.Persistence;
using NexusERP.Domain.Entities;

namespace NexusERP.Tests.Integration;

public class CustomerApiInactiveCodeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerApiInactiveCodeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCustomer_WithInactiveCode_ShouldReturnConflictAndKeepOnlyInactiveRecord()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var firstRequest = new CustomerCreateRequest
        {
            Code = "INT-INACTIVE-001",
            Name = "Original Inactive Customer",
            Email = "inactive.original@example.com",
            Phone = "03121234567",
            TaxNumber = "1234567890",
            Address = "Ankara"
        };

        var createResponse = await client.PostAsJsonAsync("/api/customers", firstRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdCustomer);

        var customerId = createdCustomer!.Id;
        var createdAtUtc = createdCustomer.CreatedAtUtc;

        var deleteResponse = await client.DeleteAsync($"/api/customers/{customerId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDeleteResponse = await client.GetAsync($"/api/customers/{customerId}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);

        var duplicateRequest = new CustomerCreateRequest
        {
            Code = "INT-INACTIVE-001",
            Name = "Second Customer",
            Email = "inactive.second@example.com",
            Phone = "03129876543",
            TaxNumber = "9876543210",
            Address = "İstanbul"
        };

        var duplicateResponse = await client.PostAsJsonAsync("/api/customers", duplicateRequest);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal("application/problem+json", duplicateResponse.Content.Headers.ContentType?.MediaType);

        using var duplicateJson = JsonDocument.Parse(await duplicateResponse.Content.ReadAsStringAsync());
        var duplicateRoot = duplicateJson.RootElement;

        Assert.True(duplicateRoot.TryGetProperty("status", out var duplicateStatus));
        Assert.Equal(409, duplicateStatus.GetInt32());

        Assert.True(duplicateRoot.TryGetProperty("code", out var duplicateCodeProperty));
        Assert.Equal("INT-INACTIVE-001", duplicateCodeProperty.GetString());

        var getAllResponse = await client.GetAsync("/api/customers");
        var customers = await getAllResponse.Content.ReadFromJsonAsync<List<CustomerResponse>>();

        Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);
        Assert.NotNull(customers);
        Assert.Empty(customers!);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NexusERPDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", dbContext.Database.ProviderName, ignoreCase: true);

        var persistedCustomers = await dbContext.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Code == "INT-INACTIVE-001")
            .ToListAsync();

        Assert.Single(persistedCustomers);

        var persistedCustomer = persistedCustomers[0];
        Assert.Equal(customerId, persistedCustomer.Id);
        Assert.Equal(createdAtUtc, persistedCustomer.CreatedAtUtc);
        Assert.False(persistedCustomer.IsActive);
        Assert.NotNull(persistedCustomer.DeactivatedAtUtc);
        Assert.NotNull(persistedCustomer.UpdatedAtUtc);
        Assert.Equal("Original Inactive Customer", persistedCustomer.Name);
        Assert.Equal(firstRequest.Code, persistedCustomer.Code);
    }
}
