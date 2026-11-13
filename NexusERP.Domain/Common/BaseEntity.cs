namespace NexusERP.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public Guid? DeactivatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? DeactivatedAtUtc { get; set; }
}
