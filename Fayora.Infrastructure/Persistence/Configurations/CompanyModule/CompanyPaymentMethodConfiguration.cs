using Fayora.Domain.Entities.CompanyModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fayora.Infrastructure.Persistence.Configurations.CompanyModule;

public sealed class CompanyPaymentMethodConfiguration : IEntityTypeConfiguration<CompanyPaymentMethod>
{
    public void Configure(EntityTypeBuilder<CompanyPaymentMethod> builder)
    {
        builder.ToTable("CompanyPaymentMethods");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CompanyId, x.Type }).IsUnique();
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.AccountIdentifier).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.OwnsOne(x => x.InstaPayQrCodeUrl, url =>
        {
            url.Property(x => x.Value).HasColumnName("InstaPayQrCodeUrl").HasMaxLength(2_048);
        });
    }
}
