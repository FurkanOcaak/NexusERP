using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusERP.Domain.Common;

namespace NexusERP.DataAccess.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                {
                    entry.Entity.Id = Guid.NewGuid();
                }

                entry.Entity.CreatedAtUtc = utcNow;
                entry.Entity.UpdatedAtUtc = null;
                entry.Entity.DeactivatedAtUtc = null;
            }
            else if (entry.State == EntityState.Deleted)
            {
                // Cancel DELETE. Start with no columns marked for update.
                entry.State = EntityState.Unchanged;

                // Explicitly update only the three soft-delete fields.
                entry.Property(x => x.IsActive).CurrentValue = false;
                entry.Property(x => x.IsActive).IsModified = true;

                entry.Property(x => x.DeactivatedAtUtc).CurrentValue = utcNow;
                entry.Property(x => x.DeactivatedAtUtc).IsModified = true;

                entry.Property(x => x.UpdatedAtUtc).CurrentValue = utcNow;
                entry.Property(x => x.UpdatedAtUtc).IsModified = true;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Do not overwrite identity or creation audit fields.
                entry.Property(x => x.Id).IsModified = false;
                entry.Property(x => x.CreatedAtUtc).IsModified = false;
                entry.Property(x => x.CreatedByUserId).IsModified = false;

                entry.Entity.UpdatedAtUtc = utcNow;
            }
        }
    }
}
