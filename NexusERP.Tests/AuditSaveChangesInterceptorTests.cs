
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NexusERP.DataAccess.Interceptors;
using NexusERP.DataAccess.Persistence;
using NexusERP.Domain.Entities;
using Xunit;

namespace NexusERP.Tests;

public class AuditSaveChangesInterceptorTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public AuditSaveChangesInterceptorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public void SaveChanges_WhenAddingCustomer_GeneratesIdAndCreatedAtUtc()
    {
        using var context = CreateContext();

        var customer = new Customer
        {
            Code = "CUST-001",
            Name = "Test Customer",
            Email = "test@example.com"
        };

        context.Customers.Add(customer);
        context.SaveChanges();

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.NotEqual(default, customer.CreatedAtUtc);
        Assert.True(customer.IsActive);
        Assert.Null(customer.UpdatedAtUtc);
        Assert.Null(customer.DeactivatedAtUtc);
    }

    [Fact]
    public void SaveChanges_WhenUpdatingCustomer_UpdatesUpdatedAtUtcAndPreservesAuditFields()
    {
        using var context = CreateContext();

        var customer = new Customer
        {
            Code = "CUST-002",
            Name = "Original Name",
            Email = "original@example.com"
        };

        context.Customers.Add(customer);
        context.SaveChanges();

        var originalId = customer.Id;
        var originalCreatedAtUtc = customer.CreatedAtUtc;

        customer.Name = "Updated Name";
        context.SaveChanges();

        Assert.Equal(originalId, customer.Id);
        Assert.Equal(originalCreatedAtUtc, customer.CreatedAtUtc);
        Assert.NotNull(customer.UpdatedAtUtc);
        Assert.True(customer.UpdatedAtUtc > originalCreatedAtUtc);
    }

    [Fact]
    public void SaveChanges_WhenCustomerModified_PreservesAuditFieldsInDatabase()
    {
        var createdByUserId = Guid.NewGuid();
        DateTime persistedCreatedAtUtc;

        using (var context = CreateContext())
        {
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Code = "CUST-003",
                Name = "Initial Name",
                CreatedByUserId = createdByUserId
            };

            context.Customers.Add(customer);
            context.SaveChanges();

            persistedCreatedAtUtc = customer.CreatedAtUtc;

            var tracked = context.Customers
                .Single(x => x.Id == customer.Id);

            tracked.Name = "Changed Name";
            tracked.CreatedAtUtc = DateTime.UtcNow.AddYears(-10);
            tracked.CreatedByUserId = Guid.NewGuid();

            context.SaveChanges();
        }

        using var verificationContext = CreateContext();

        var savedCustomer = verificationContext.Customers
            .AsNoTracking()
            .Single(x => x.Code == "CUST-003");

        Assert.Equal(
            createdByUserId,
            savedCustomer.CreatedByUserId);

        Assert.Equal(
            persistedCreatedAtUtc,
            savedCustomer.CreatedAtUtc);

        Assert.Equal("Changed Name", savedCustomer.Name);
        Assert.NotNull(savedCustomer.UpdatedAtUtc);
        Assert.True(savedCustomer.UpdatedAtUtc > persistedCreatedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAddingCustomer_GeneratesAuditValues()
    {
        await using var context = CreateContext();

        var customer = new Customer
        {
            Code = "CUST-004",
            Name = "Async Customer"
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.NotEqual(default, customer.CreatedAtUtc);
        Assert.True(customer.IsActive);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenUpdatingCustomer_RefreshesUpdatedAtUtc()
    {
        await using var context = CreateContext();

        var customer = new Customer
        {
            Code = "CUST-005",
            Name = "Async Customer 2"
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var originalUpdatedAtUtc = customer.UpdatedAtUtc;

        customer.Name = "Updated Async Name";
        await context.SaveChangesAsync();

        Assert.NotEqual(originalUpdatedAtUtc, customer.UpdatedAtUtc);
        Assert.NotNull(customer.UpdatedAtUtc);
    }

    [Fact]
    public void SaveChanges_WhenRemovingCustomer_PerformsSoftDelete()
    {
        Guid customerId;
        DateTime originalCreatedAtUtc;
        var createdByUserId = Guid.NewGuid();

        using (var context = CreateContext())
        {
            var customer = new Customer
            {
                Code = "CUST-SOFT-001",
                Name = "Soft Delete Test Customer",
                CreatedByUserId = createdByUserId
            };

            context.Customers.Add(customer);
            context.SaveChanges();

            customerId = customer.Id;
            originalCreatedAtUtc = customer.CreatedAtUtc;

            context.Customers.Remove(customer);
            context.SaveChanges();
        }

        using var verificationContext = CreateContext();

        // Pasif kaydı doğrulamak için global filtreyi atlıyoruz.
        var savedCustomer = verificationContext.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefault(x => x.Id == customerId);

        // Kayıt fiziksel olarak silinmemiş olmalı.
        Assert.NotNull(savedCustomer);

        // Kayıt pasifleştirilmiş olmalı.
        Assert.False(savedCustomer.IsActive);

        // Audit tarihleri doldurulmuş olmalı.
        Assert.NotNull(savedCustomer.DeactivatedAtUtc);
        Assert.NotNull(savedCustomer.UpdatedAtUtc);

        // Oluşturulma bilgileri korunmalı.
        Assert.Equal(
            originalCreatedAtUtc,
            savedCustomer.CreatedAtUtc);

        Assert.Equal(
            createdByUserId,
            savedCustomer.CreatedByUserId);

        // İş verileri korunmalı.
        Assert.Equal(
            "Soft Delete Test Customer",
            savedCustomer.Name);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenRemovingCustomer_PerformsSoftDelete()
    {
        Guid customerId;
        DateTime originalCreatedAtUtc;

        await using (var context = CreateContext())
        {
            var customer = new Customer
            {
                Code = "CUST-SOFT-ASYNC-001",
                Name = "Async Soft Delete Customer"
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            customerId = customer.Id;
            originalCreatedAtUtc = customer.CreatedAtUtc;

            context.Customers.Remove(customer);
            await context.SaveChangesAsync();
        }

        await using var verificationContext = CreateContext();

        // Asenkron sorguda da global filtreyi atlıyoruz.
        var savedCustomer = await verificationContext.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == customerId);

        // Kayıt fiziksel olarak silinmemiş olmalı.
        Assert.NotNull(savedCustomer);

        // Kayıt pasifleştirilmiş olmalı.
        Assert.False(savedCustomer.IsActive);

        // Audit tarihleri doldurulmuş olmalı.
        Assert.NotNull(savedCustomer.DeactivatedAtUtc);
        Assert.NotNull(savedCustomer.UpdatedAtUtc);

        // Oluşturulma tarihi korunmalı.
        Assert.Equal(
            originalCreatedAtUtc,
            savedCustomer.CreatedAtUtc);

        // İş verileri korunmalı.
        Assert.Equal(
            "Async Soft Delete Customer",
            savedCustomer.Name);
    }


    [Fact]
    public void QueryFilter_HidesInactiveCustomers()
    {
        Guid activeCustomerId;
        Guid inactiveCustomerId;

        using (var context = CreateContext())
        {
            var activeCustomer = new Customer
            {
                Code = "CUST-FILTER-001",
                Name = "Active Customer"
            };

            var inactiveCustomer = new Customer
            {
                Code = "CUST-FILTER-002",
                Name = "Inactive Customer"
            };

            context.Customers.AddRange(
                activeCustomer,
                inactiveCustomer);

            context.SaveChanges();

            activeCustomerId = activeCustomer.Id;
            inactiveCustomerId = inactiveCustomer.Id;

            // İkinci müşteriyi Soft Delete ile pasifleştir.
            context.Customers.Remove(inactiveCustomer);
            context.SaveChanges();
        }

        using var verificationContext = CreateContext();

        // Normal sorguda yalnızca aktif müşteriler görünmeli.
        var visibleCustomers = verificationContext.Customers
            .AsNoTracking()
            .ToList();

        Assert.Single(visibleCustomers);

        Assert.Contains(
            visibleCustomers,
            x => x.Id == activeCustomerId);

        Assert.DoesNotContain(
            visibleCustomers,
            x => x.Id == inactiveCustomerId);

        // Global filtre kaldırılınca iki kayıt da görünmeli.
        var allCustomers = verificationContext.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToList();

        Assert.Equal(2, allCustomers.Count);

        var savedInactiveCustomer = allCustomers.Single(
    x => x.Id == inactiveCustomerId);

        Assert.False(savedInactiveCustomer.IsActive);
        Assert.NotNull(savedInactiveCustomer.DeactivatedAtUtc);
    }


    private NexusERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NexusERPDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditSaveChangesInterceptor())
            .Options;

        var context = new NexusERPDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
