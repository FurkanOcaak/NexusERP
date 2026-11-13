using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusERP.Application.Features.Customers.Abstractions;
using NexusERP.DataAccess.Interceptors;
using NexusERP.DataAccess.Persistence;
using NexusERP.DataAccess.Repositories;
using NexusERP.Domain.Entities;
using Xunit;

namespace NexusERP.Tests.Repositories;

public class CustomerRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public CustomerRepositoryTests()
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

        // SQLite test veritabanında gerekli tabloları oluşturur.
        context.Database.EnsureCreated();

        return context;
    }


    [Fact]
    public async Task AddAsync_And_SaveChangesAsync_ShouldPersistCustomer()
    {
        await using var context = CreateContext(_connection);
        var repository = new CustomerRepository(context);

        var customer = new Customer
        {
            Code = "C-001",
            Name = "Alpha Customer",
            Email = "alpha@example.com",
            Phone = "1234567890",
            TaxNumber = "TAX-001",
            Address = "Istanbul"
        };

        await repository.AddAsync(customer, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var persisted = await context.Customers
            .AsNoTracking()
            .SingleAsync(x => x.Id == customer.Id);

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("C-001", persisted.Code);
        Assert.Equal("Alpha Customer", persisted.Name);
        Assert.Equal("alpha@example.com", persisted.Email);
        Assert.True(persisted.IsActive);
        Assert.NotEqual(default, persisted.CreatedAtUtc);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerExists_ShouldReturnActiveCustomer()
    {
        await using var context = CreateContext(_connection);
        var repository = new CustomerRepository(context);

        var customer = new Customer
        {
            Code = "C-002",
            Name = "Active Customer"
        };

        await repository.AddAsync(customer, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var result = await repository.GetByIdAsync(customer.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(customer.Id, result!.Id);
        Assert.Equal("C-002", result.Code);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerDoesNotExist_ShouldReturnNull()
    {
        await using var context = CreateContext(_connection);
        var repository = new CustomerRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActiveCustomers()
    {
        await using var context = CreateContext(_connection);
        var repository = new CustomerRepository(context);

        var activeCustomer = new Customer
        {
            Code = "C-003",
            Name = "Active One"
        };

        var inactiveCustomer = new Customer
        {
            Code = "C-004",
            Name = "Inactive One",
            IsActive = false,
            DeactivatedAtUtc = DateTime.UtcNow
        };

        await repository.AddAsync(activeCustomer, CancellationToken.None);
        await repository.AddAsync(inactiveCustomer, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var result = await repository.GetAllAsync(CancellationToken.None);

        Assert.Contains(result, x => x.Id == activeCustomer.Id);
        Assert.DoesNotContain(result, x => x.Id == inactiveCustomer.Id);
        Assert.All(result, x => Assert.True(x.IsActive));
    }

    [Fact]
    public async Task ExistsByCodeAsync_WhenCustomerIsActive_ShouldReturnTrue()
    {
        await using var context = CreateContext(_connection);
        var repository = new CustomerRepository(context);

        var customer = new Customer
        {
            Code = "C-005",
            Name = "Find Me"
        };

        await repository.AddAsync(customer, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var exists = await repository.ExistsByCodeAsync("C-005", CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByCodeAsync_WhenCustomerIsSoftDeleted_ShouldReturnTrue()
    {
        await using var context = CreateContext(_connection);
        var repository = new CustomerRepository(context);

        var customer = new Customer
        {
            Code = "C-006",
            Name = "Soft Deleted Customer"
        };

        await repository.AddAsync(customer, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        repository.Remove(customer);
        await repository.SaveChangesAsync(CancellationToken.None);

        var exists = await repository.ExistsByCodeAsync("C-006", CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task Remove_And_SaveChangesAsync_ShouldPerformSoftDeleteInsteadOfPhysicalDelete()
    {
        await using var context = CreateContext(_connection);
        var repository = new CustomerRepository(context);

        var customer = new Customer
        {
            Code = "C-007",
            Name = "Delete Me",
            Email = "delete@example.com"
        };

        await repository.AddAsync(customer, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var createdAtUtc = customer.CreatedAtUtc;

        repository.Remove(customer);
        await repository.SaveChangesAsync(CancellationToken.None);

        var visibleRecord = await context.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == customer.Id);

        var deletedRecord = await context.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(x => x.Id == customer.Id);

        Assert.Null(visibleRecord);
        Assert.NotNull(deletedRecord);
        Assert.False(deletedRecord.IsActive);
        Assert.NotNull(deletedRecord.DeactivatedAtUtc);
        Assert.NotNull(deletedRecord.UpdatedAtUtc);
        Assert.Equal(createdAtUtc, deletedRecord.CreatedAtUtc);
        Assert.Equal("Delete Me", deletedRecord.Name);
        Assert.Equal("C-007", deletedRecord.Code);
    }
}
