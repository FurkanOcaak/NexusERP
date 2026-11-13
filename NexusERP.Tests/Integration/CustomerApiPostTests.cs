using System.Net;
using System.Net.Http.Json;
using NexusERP.Application.Features.Customers.DTOs;

namespace NexusERP.Tests.Integration;

public class CustomerApiPostTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerApiPostTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCustomer_ShouldCreateCustomerAndBeRetrievableById()
    {
        using var client = _factory.CreateClient();

        var request = new CustomerCreateRequest
        {
            Code = "INT-POST-001",
            Name = "Integration Test Customer",
            Email = "integration@example.com",
            Phone = "03121234567",
            TaxNumber = "1234567890",
            Address = "Ankara"
        };

        var postResponse = await client.PostAsJsonAsync("/api/customers", request);
        var createdContent = await postResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        Assert.NotNull(createdContent);
        Assert.NotEqual(Guid.Empty, createdContent!.Id);
        Assert.Equal(request.Code, createdContent.Code);
        Assert.Equal(request.Name, createdContent.Name);
        Assert.True(createdContent.IsActive);
        Assert.NotEqual(default, createdContent.CreatedAtUtc);
        Assert.Null(createdContent.UpdatedAtUtc);

        Assert.True(postResponse.Headers.Location is not null);

        var createdId = createdContent.Id;
        var getResponse = await client.GetAsync(postResponse.Headers.Location!);
        var fetchedCustomer = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(fetchedCustomer);
        Assert.Equal(createdId, fetchedCustomer!.Id);
        Assert.Equal(request.Code, fetchedCustomer.Code);
        Assert.Equal(request.Name, fetchedCustomer.Name);
    }
}
