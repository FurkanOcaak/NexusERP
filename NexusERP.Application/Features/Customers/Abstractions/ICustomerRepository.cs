using NexusERP.Domain.Entities;

namespace NexusERP.Application.Features.Customers.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(Customer customer, CancellationToken cancellationToken);

    void Remove(Customer customer);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
