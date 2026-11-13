using Microsoft.EntityFrameworkCore;
using NexusERP.Application.Features.Customers.Abstractions;
using NexusERP.DataAccess.Persistence;
using NexusERP.Domain.Entities;

namespace NexusERP.DataAccess.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly NexusERPDbContext _context;

    public CustomerRepository(NexusERPDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Customers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _context.Customers
            .IgnoreQueryFilters()
            .AnyAsync(customer => customer.Code == code, cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }

    public void Remove(Customer customer)
    {
        _context.Customers.Remove(customer);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
