using Microsoft.EntityFrameworkCore;
using NexusERP.Domain.Entities;

namespace NexusERP.DataAccess.Persistence;

public class NexusERPDbContext : DbContext
{
    public NexusERPDbContext(DbContextOptions<NexusERPDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexusERPDbContext).Assembly);
    }
}
