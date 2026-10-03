using Fayora.Domain.Entities.AccommodationModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Entities.PackageModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fayora.Infrastructure.Persistence.Configurations.CompanyModule;

public sealed class PackageAccommodationLinkConfiguration : IEntityTypeConfiguration<PackageAccommodationLink>
{
    public void Configure(EntityTypeBuilder<PackageAccommodationLink> builder)
    {
        builder.ToTable("PackageAccommodationLinks");
        builder.HasKey(x => new { x.PackageId, x.AccommodationId });
        builder.HasOne<TravelPackage>().WithMany().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<HousingUnit>().WithMany().HasForeignKey(x => x.AccommodationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.AccommodationId);
    }
}
