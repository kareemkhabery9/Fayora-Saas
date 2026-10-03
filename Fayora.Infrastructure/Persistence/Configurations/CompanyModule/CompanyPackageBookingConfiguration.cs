using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fayora.Infrastructure.Persistence.Configurations.CompanyModule;

public sealed class CompanyPackageBookingConfiguration : IEntityTypeConfiguration<CompanyPackageBooking>
{
    public void Configure(EntityTypeBuilder<CompanyPackageBooking> builder)
    {
        builder.ToTable("CompanyPackageBookings");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CompanyId, x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.PackageId, x.OccurrenceId });
        builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CustomerPhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CustomerEmail).HasMaxLength(320);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Property(x => x.RemainingAmount).HasPrecision(18, 2);
        builder.Property(x => x.IsDepositPayment).IsRequired();
        builder.Property(x => x.PaymentMethodType).HasConversion<int>();
        builder.Property(x => x.PaymentAccountIdentifier).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SenderPaymentIdentifier).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BookingReference).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.BookingReference).IsUnique();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CompanyReviewNote).HasMaxLength(1000);
        builder.OwnsOne(x => x.PaymentReceiptUrl, url =>
        {
            url.Property(x => x.Value).HasColumnName("PaymentReceiptUrl").HasMaxLength(2048).IsRequired();
        });
        builder.HasOne<Fayora.Domain.Entities.CompanyModule.Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Fayora.Domain.Entities.PackageModule.TravelPackage>().WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Fayora.Domain.Entities.PackageModule.PackageOccurrence>().WithMany().HasForeignKey(x => x.OccurrenceId).OnDelete(DeleteBehavior.Restrict);
    }
}
