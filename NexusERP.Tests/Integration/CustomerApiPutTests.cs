using System.Net;
using System.Net.Http.Json;
using NexusERP.Application.Features.Customers.DTOs;

namespace NexusERP.Tests.Integration;

public class CustomerApiPutTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerApiPutTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PutCustomer_ShouldUpdateCustomerAndPersistAuditFields()
    {
        using var client = _factory.CreateClient();

        var createRequest = new CustomerCreateRequest
        {
            Code = "INT-PUT-001",
            Name = "Original Customer",
            Email = "original@example.com",
            Phone = "03121234567",
            TaxNumber = "1234567890",
            Address = "Ankara"
        };

        var createResponse = await client.PostAsJsonAsync("/api/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdCustomer);

        var createdId = createdCustomer!.Id;
        var originalCreatedAtUtc = createdCustomer.CreatedAtUtc;

        var updateRequest = new CustomerUpdateRequest
        {
            Code = "INT-PUT-001",
            Name = "Updated Customer",
            Email = "updated@example.com",
            Phone = "03129876543",
            TaxNumber = "1234567890",
            Address = "Ankara, Çankaya"
        };

        var putResponse = await client.PutAsJsonAsync($"/api/customers/{createdId}", updateRequest);
        var updatedCustomer = await putResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        Assert.NotNull(updatedCustomer);

        Assert.Equal(createdId, updatedCustomer!.Id);
        Assert.Equal(updateRequest.Code, updatedCustomer.Code);
        Assert.Equal(updateRequest.Name, updatedCustomer.Name);
        Assert.Equal(updateRequest.Email, updatedCustomer.Email);
        Assert.Equal(updateRequest.Phone, updatedCustomer.Phone);
        Assert.Equal(updateRequest.Address, updatedCustomer.Address);
        Assert.True(updatedCustomer.IsActive);
        Assert.Equal(originalCreatedAtUtc, updatedCustomer.CreatedAtUtc);
        Assert.NotNull(updatedCustomer.UpdatedAtUtc);
        Assert.True(updatedCustomer.UpdatedAtUtc > originalCreatedAtUtc);

        var getResponse = await client.GetAsync($"/api/customers/{createdId}");
        var fetchedCustomer = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(fetchedCustomer);

        Assert.Equal(updatedCustomer.Id, fetchedCustomer!.Id);
        Assert.Equal(updatedCustomer.Code, fetchedCustomer.Code);
        Assert.Equal(updatedCustomer.Name, fetchedCustomer.Name);
        Assert.Equal(updatedCustomer.Email, fetchedCustomer.Email);
        Assert.Equal(updatedCustomer.Phone, fetchedCustomer.Phone);
        Assert.Equal(updatedCustomer.Address, fetchedCustomer.Address);
        Assert.Equal(updatedCustomer.IsActive, fetchedCustomer.IsActive);
        Assert.Equal(updatedCustomer.CreatedAtUtc, fetchedCustomer.CreatedAtUtc);
        Assert.NotNull(fetchedCustomer.UpdatedAtUtc);
        Assert.Equal(updatedCustomer.UpdatedAtUtc, fetchedCustomer.UpdatedAtUtc);
    }
}
