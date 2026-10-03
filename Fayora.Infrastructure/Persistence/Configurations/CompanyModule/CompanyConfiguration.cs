using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Entities.IdentityModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fayora.Infrastructure.Persistence.Configurations.CompanyModule;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OwnerUserId).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");

        // V1 gives each company one dashboard owner. A membership model can extend this later
        // without weakening the tenant boundary established by CompanyId.
        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<Company>(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(30);
        builder.Property(x => x.Description).HasMaxLength(2_000);
        builder.Property(x => x.FacebookUrl).HasMaxLength(2_048);
        builder.Property(x => x.InstagramUrl).HasMaxLength(2_048);
        builder.Property(x => x.Theme).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.OwnsOne(x => x.ContactEmail, email =>
        {
            email.Property(x => x.Value).HasColumnName("ContactEmail").HasMaxLength(320).IsRequired();
        });

        builder.OwnsOne(x => x.ContactPhoneNumber, phone =>
        {
            phone.Property(x => x.Value).HasColumnName("ContactPhoneNumber").HasMaxLength(20).IsRequired();
        });

        builder.OwnsOne(x => x.LogoUrl, url =>
        {
            url.Property(x => x.Value).HasColumnName("LogoUrl").HasMaxLength(2_048);
        });

        builder.OwnsOne(x => x.CoverImageUrl, url =>
        {
            url.Property(x => x.Value).HasColumnName("CoverImageUrl").HasMaxLength(2_048);
        });

        builder.HasMany(x => x.WhatsAppNumbers)
            .WithOne()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.WhatsAppNumbers)
            .HasField("_whatsAppNumbers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.PaymentMethods)
            .WithOne()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.PaymentMethods)
            .HasField("_paymentMethods")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
