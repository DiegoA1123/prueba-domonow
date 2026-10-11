using DomoNow.Parking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DomoNow.Parking.Infrastructure.Persistence.Configurations;

internal sealed class OperatorConfiguration : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> builder)
    {
        builder.ToTable("operators");
        builder.HasKey(op => op.Id);
        builder.Property(op => op.Username).HasMaxLength(50).IsRequired();
        builder.HasIndex(op => op.Username).IsUnique();
        builder.Property(op => op.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(op => op.Role).HasMaxLength(20).IsRequired();
    }
}
