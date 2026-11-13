using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusERP.Application.Features.Customers.DTOs;
using NexusERP.DataAccess.Persistence;
using NexusERP.Domain.Entities;

namespace NexusERP.Tests.Integration;

public class CustomerApiDeleteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerApiDeleteTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteCustomer_ShouldSoftDeleteAndPersistAuditFields()
    {
        using var client = _factory.CreateClient();

        var createRequest = new CustomerCreateRequest
        {
            Code = "INT-DELETE-001",
            Name = "Delete Integration Customer",
            Email = "delete.integration@example.com",
            Phone = "03121234567",
            TaxNumber = "1234567890",
            Address = "Ankara"
        };

        var createResponse = await client.PostAsJsonAsync("/api/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdCustomer);

        var createdId = createdCustomer!.Id;
        var createdAtUtc = createdCustomer.CreatedAtUtc;

        var deleteResponse = await client.DeleteAsync($"/api/customers/{createdId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDeleteResponse = await client.GetAsync($"/api/customers/{createdId}");

        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NexusERPDbContext>();

        Assert.Equal(
            "Microsoft.EntityFrameworkCore.Sqlite",
            dbContext.Database.ProviderName,
            ignoreCase: true);

        var databaseCustomer = await dbContext.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == createdId);

        Assert.NotNull(databaseCustomer);

        var persistedCustomer = databaseCustomer!;

        Assert.Equal(createdId, persistedCustomer.Id);
        Assert.Equal(createRequest.Code, persistedCustomer.Code);
        Assert.Equal(createRequest.Name, persistedCustomer.Name);
        Assert.False(persistedCustomer.IsActive);
        Assert.NotNull(persistedCustomer.DeactivatedAtUtc);
        Assert.NotNull(persistedCustomer.UpdatedAtUtc);
        Assert.Equal(createdAtUtc, persistedCustomer.CreatedAtUtc);
        Assert.True(persistedCustomer.DeactivatedAtUtc >= persistedCustomer.CreatedAtUtc);
    }
}
