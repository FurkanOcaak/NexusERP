using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusERP.Application.Features.Customers.Abstractions;
using NexusERP.Application.Features.Customers.DTOs;
using NexusERP.Application.Features.Customers.Exceptions;
using NexusERP.Application.Features.Customers.Services;
using NexusERP.Application.Features.Customers.Validators;
using NexusERP.DataAccess.Interceptors;
using NexusERP.DataAccess.Persistence;
using NexusERP.DataAccess.Repositories;
using NexusERP.Domain.Entities;
using Xunit;

namespace NexusERP.Tests.Services;

public class CustomerServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public CustomerServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private static NexusERPDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<NexusERPDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new AuditSaveChangesInterceptor())
            .Options;

        var context = new NexusERPDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static CustomerService CreateService(NexusERPDbContext context)
    {
        var repository = new CustomerRepository(context);
        var createValidator = new CustomerCreateRequestValidator();
        var updateValidator = new CustomerUpdateRequestValidator();

        return new CustomerService(repository, createValidator, updateValidator);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateCustomer()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var request = new CustomerCreateRequest
        {
            Code = "C-101",
            Name = "Test Customer",
            Email = "test@example.com",
            Phone = "1234567890",
            TaxNumber = "TAX-001",
            Address = "Istanbul"
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("C-101", result.Code);
        Assert.Equal("Test Customer", result.Name);
        Assert.True(result.IsActive);
        Assert.NotEqual(default, result.CreatedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCodeOrName_ShouldThrowValidationException()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var request = new CustomerCreateRequest
        {
            Code = "",
            Name = "     "
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenCodeAlreadyExists_ShouldThrowDuplicateCustomerCodeException()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var first = new CustomerCreateRequest { Code = "DUP-001", Name = "First Customer" };
        await service.CreateAsync(first, CancellationToken.None);

        var duplicate = new CustomerCreateRequest { Code = "DUP-001", Name = "Second Customer" };

        var exception = await Assert.ThrowsAsync<DuplicateCustomerCodeException>(() => service.CreateAsync(duplicate, CancellationToken.None));

        Assert.Equal("DUP-001", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenSoftDeletedCodeExists_ShouldThrowDuplicateCustomerCodeException()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var request = new CustomerCreateRequest { Code = "DUP-002", Name = "Old Customer" };
        var created = await service.CreateAsync(request, CancellationToken.None);

        await service.DeactivateAsync(created.Id, CancellationToken.None);

        var duplicate = new CustomerCreateRequest { Code = "DUP-002", Name = "New Customer" };

        var exception = await Assert.ThrowsAsync<DuplicateCustomerCodeException>(() => service.CreateAsync(duplicate, CancellationToken.None));

        Assert.Equal("DUP-002", exception.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerExists_ShouldReturnMappedDto()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var createRequest = new CustomerCreateRequest
        {
            Code = "C-102",
            Name = "Active Customer",
            Email = "active@example.com",
            Phone = "5551112233",
            TaxNumber = "1234567890",
            Address = "Ankara"
        };

        var created = await service.CreateAsync(createRequest, CancellationToken.None);
        var result = await service.GetByIdAsync(created.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result!.Id);
        Assert.Equal("C-102", result.Code);
        Assert.Equal("Active Customer", result.Name);
        Assert.Equal("active@example.com", result.Email);
        Assert.Equal("Ankara", result.Address);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerDoesNotExist_ShouldReturnNull()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var result = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActiveCustomers()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        await service.CreateAsync(new CustomerCreateRequest { Code = "C-103", Name = "Active 1" }, CancellationToken.None);
        var second = await service.CreateAsync(new CustomerCreateRequest { Code = "C-104", Name = "Active 2" }, CancellationToken.None);
        await service.DeactivateAsync(second.Id, CancellationToken.None);

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Contains(result, x => x.Code == "C-103");
        Assert.DoesNotContain(result, x => x.Code == "C-104");
        Assert.All(result, x => Assert.True(x.IsActive));
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ShouldUpdateCustomer()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var created = await service.CreateAsync(new CustomerCreateRequest { Code = "C-105", Name = "Before Update", Email = "before@example.com" }, CancellationToken.None);

        var updateRequest = new CustomerUpdateRequest
        {
            Code = "C-105",
            Name = "After Update",
            Email = "after@example.com",
            Phone = "5550001111",
            TaxNumber = "1231231231",
            Address = "Izmir"
        };

        var result = await service.UpdateAsync(created.Id, updateRequest, CancellationToken.None);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("After Update", result.Name);
        Assert.Equal("after@example.com", result.Email);
        Assert.Equal("Izmir", result.Address);
        Assert.NotNull(result.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_WhenCodeIsUnchanged_ShouldStillSucceed()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var created = await service.CreateAsync(new CustomerCreateRequest { Code = "C-106", Name = "Same Code" }, CancellationToken.None);

        var updateRequest = new CustomerUpdateRequest
        {
            Code = "C-106",
            Name = "Updated Name"
        };

        var result = await service.UpdateAsync(created.Id, updateRequest, CancellationToken.None);

        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("C-106", result.Code);
    }

    [Fact]
    public async Task UpdateAsync_WhenCodeBelongsToAnotherCustomer_ShouldThrowDuplicateCustomerCodeException()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        await service.CreateAsync(new CustomerCreateRequest { Code = "C-107", Name = "Customer One" }, CancellationToken.None);
        var second = await service.CreateAsync(new CustomerCreateRequest { Code = "C-108", Name = "Customer Two" }, CancellationToken.None);

        var updateRequest = new CustomerUpdateRequest { Code = "C-107", Name = "Changed Name" };

        var exception = await Assert.ThrowsAsync<DuplicateCustomerCodeException>(() => service.UpdateAsync(second.Id, updateRequest, CancellationToken.None));

        Assert.Equal("C-107", exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidRequest_ShouldThrowValidationException()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var created = await service.CreateAsync(new CustomerCreateRequest { Code = "C-109", Name = "Customer" }, CancellationToken.None);

        var invalidRequest = new CustomerUpdateRequest { Code = "", Name = "   " };

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(created.Id, invalidRequest, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_WhenCustomerDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var request = new CustomerUpdateRequest { Code = "C-110", Name = "Missing Customer" };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None));
    }

    [Fact]
    public async Task DeactivateAsync_ShouldSoftDeleteCustomerWithoutPhysicalDelete()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var created = await service.CreateAsync(new CustomerCreateRequest { Code = "C-111", Name = "Delete Me" }, CancellationToken.None);

        await service.DeactivateAsync(created.Id, CancellationToken.None);

        var repository = new CustomerRepository(context);
        var visibleCustomer = await repository.GetByIdAsync(created.Id, CancellationToken.None);

        var databaseCustomer = await context.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(x => x.Id == created.Id);

        Assert.Null(visibleCustomer);
        Assert.NotNull(databaseCustomer);
        Assert.False(databaseCustomer.IsActive);
        Assert.NotNull(databaseCustomer.DeactivatedAtUtc);
        Assert.NotNull(databaseCustomer.UpdatedAtUtc);
    }

    [Fact]
    public async Task DeactivateAsync_WhenCustomerDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeactivateAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAndUpdate_ShouldMapDTOFieldsCorrectly()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var created = await service.CreateAsync(
            new CustomerCreateRequest
            {
                Code = "C-112",
                Name = "Map Check",
                Email = "map@example.com",
                Phone = "5557778888",
                TaxNumber = "9876543210",
                Address = "Bursa"
            },
            CancellationToken.None);

        var updated = await service.UpdateAsync(
            created.Id,
            new CustomerUpdateRequest
            {
                Code = "C-112",
                Name = "Map Check Updated",
                Email = "updated@example.com",
                Phone = "1112223333",
                TaxNumber = "1112223334",
                Address = "Adana"
            },
            CancellationToken.None);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("C-112", updated.Code);
        Assert.Equal("Map Check Updated", updated.Name);
        Assert.Equal("updated@example.com", updated.Email);
        Assert.Equal("1112223333", updated.Phone);
        Assert.Equal("1112223334", updated.TaxNumber);
        Assert.Equal("Adana", updated.Address);
        Assert.Equal(created.CreatedAtUtc, updated.CreatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreserveIdAndCreatedAtUtc()
    {
        await using var context = CreateContext(_connection);
        var service = CreateService(context);

        var created = await service.CreateAsync(new CustomerCreateRequest { Code = "C-113", Name = "Keep Audit" }, CancellationToken.None);

        var updated = await service.UpdateAsync(
            created.Id,
            new CustomerUpdateRequest { Code = "C-113", Name = "Updated Audit" },
            CancellationToken.None);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(created.CreatedAtUtc, updated.CreatedAtUtc);
    }
}
