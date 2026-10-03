using Fayora.Domain.Entities.CompanyModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fayora.Infrastructure.Persistence.Configurations.CompanyModule;

public sealed class CompanyWhatsAppNumberConfiguration : IEntityTypeConfiguration<CompanyWhatsAppNumber>
{
    public void Configure(EntityTypeBuilder<CompanyWhatsAppNumber> builder)
    {
        builder.ToTable("CompanyWhatsAppNumbers");
        builder.HasKey(x => x.Id);
        builder.OwnsOne(x => x.PhoneNumber, phone =>
        {
            phone.Property(x => x.Value).HasColumnName("PhoneNumber").HasMaxLength(20).IsRequired();
        });
    }
}
