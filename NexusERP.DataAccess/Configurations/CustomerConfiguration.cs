using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusERP.Domain.Entities;

namespace NexusERP.DataAccess.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasQueryFilter(customer => customer.IsActive);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(32)
            .HasColumnType("varchar(32)");
        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("nvarchar(200)");

        builder.Property(x => x.Email)
            .HasMaxLength(254)
            .HasColumnType("varchar(254)")
            .IsRequired(false);

        builder.Property(x => x.Phone)
            .HasMaxLength(32)
            .HasColumnType("varchar(32)")
            .IsRequired(false);

        builder.Property(x => x.TaxNumber)
            .HasMaxLength(32)
            .HasColumnType("varchar(32)")
            .IsRequired(false);

        builder.Property(x => x.Address)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)")
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(x => x.DeactivatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(x => x.CreatedByUserId)
            .HasColumnType("uniqueidentifier")
            .IsRequired(false);

        builder.Property(x => x.UpdatedByUserId)
            .HasColumnType("uniqueidentifier")
            .IsRequired(false);

        builder.Property(x => x.DeactivatedByUserId)
            .HasColumnType("uniqueidentifier")
            .IsRequired(false);
    }
}
