using Fayora.Domain.Entities.PackageModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

public class TravelPackageConfiguration : IEntityTypeConfiguration<TravelPackage>
{
    public void Configure(EntityTypeBuilder<TravelPackage> builder)
    {
        builder.ToTable("TravelPackages");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CompanyId, x.PackageStatus });
        builder.HasOne<Fayora.Domain.Entities.CompanyModule.Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Title).IsRequired()
            .HasColumnType("nvarchar(255)");
        builder.Property(x => x.Description)
            .HasColumnType("nvarchar(2000)");
        builder.Property(x => x.AdultPrice).IsRequired().HasPrecision(18, 2);
        builder.Property(x => x.ChildPrice).HasPrecision(18, 2);
        builder.Property(x => x.DepositPercentage).HasPrecision(5, 2);
        builder.Property(x => x.AverageRating).HasPrecision(3, 2);
        builder.Property(x => x.CancellationPolicy).HasConversion<int>();
        builder.Property(x => x.TransportType).HasConversion<int>();
        builder.Property(x => x.PackageStatus).HasConversion<int>();
        builder.Property(x => x.GuestRequirements)
            .HasColumnType("nvarchar(1000)");
        builder.Property(x => x.RefundPolicyDetails)
            .HasColumnType("nvarchar(2000)");
        builder.Property(x => x.ArrivalNote)
            .HasColumnType("nvarchar(1000)");

        builder.Property(x => x.HasGroupDiscount).HasDefaultValue(false);
        builder.Property(x => x.GroupDiscountMinPeople).IsRequired(false);
        builder.Property(x => x.GroupDiscountPercent).HasPrecision(18, 2).IsRequired(false);

        builder.OwnsOne(x => x.MainImageUrl, nav =>
        {
            nav.Property(f => f.Value).HasColumnName("MainImageUrl").HasMaxLength(2048);
        });

        builder.OwnsOne(x => x.MainVideoUrl, nav =>
        {
            nav.Property(f => f.Value).HasColumnName("MainVideoUrl").HasMaxLength(2048);
        });



        builder.Property<List<int>>("_includedItemIds")
            .HasColumnName("IncludedItemIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions?)null) ?? new List<int>())
            .Metadata.SetValueComparer(CreateIntListComparer());

        builder.Property<List<int>>("_excludedItemIds")
            .HasColumnName("ExcludedItemIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions?)null) ?? new List<int>())
            .Metadata.SetValueComparer(CreateIntListComparer());

        builder.Property<List<Guid>>("_imageIds")
            .HasColumnName("ImageIds")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(CreateGuidListComparer());

        builder.Property(x => x.TourTypes).HasConversion<int>();

        builder.Property(x => x.TourTypes).HasConversion<int>();

        builder.HasMany(p => p.Occurrences)
               .WithOne()
               .HasForeignKey(o => o.PackageId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(TravelPackage.Occurrences))
       ?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property<List<int>>("_locationIds")
              .HasColumnName("LocationIds")
              .HasConversion(
              v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
              v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions?)null) ?? new List<int>())
             .Metadata.SetValueComparer(CreateIntListComparer());

    }

    private ValueComparer<List<int>> CreateIntListComparer()
    {
        ValueComparer<List<int>> valueComparer = new((c1, c2) => c1!.SequenceEqual(c2!), c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())), c => c.ToList());
        return valueComparer;
    }

    private ValueComparer<List<Guid>> CreateGuidListComparer()
    {
        ValueComparer<List<Guid>> valueComparer = new((c1, c2) => c1!.SequenceEqual(c2!), c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())), c => c.ToList());
        return valueComparer;
    }

}
