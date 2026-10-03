using Fayora.Domain.Entities.PackageModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fayora.Infrastructure.Persistence.Configurations.PackageModule;

public class PackageOccurrenceConfiguration : IEntityTypeConfiguration<PackageOccurrence>
{
    public void Configure(EntityTypeBuilder<PackageOccurrence> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AvailableSeats)
            .IsRequired()
            .IsConcurrencyToken(); // if multiple users try to reserve seats at the same time, this will help prevent overbooking

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.HasIndex(x => new { x.PackageId, x.Date })
            .IsUnique();

        builder.HasOne<TravelPackage>()
            .WithMany()
            .HasForeignKey(x => x.PackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
