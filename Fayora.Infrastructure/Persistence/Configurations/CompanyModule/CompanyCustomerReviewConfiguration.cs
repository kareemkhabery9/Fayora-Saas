using Fayora.Domain.Entities.CompanyModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fayora.Infrastructure.Persistence.Configurations.CompanyModule;

public sealed class CompanyCustomerReviewConfiguration : IEntityTypeConfiguration<CompanyCustomerReview>
{
    public void Configure(EntityTypeBuilder<CompanyCustomerReview> builder)
    {
        builder.ToTable("CompanyCustomerReviews");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CompanyId, x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.CompanyId, x.TargetType, x.TargetId, x.Status });
        builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Rating).HasPrecision(2, 1);
        builder.Property(x => x.Comment).HasMaxLength(1000);
        builder.Property(x => x.TargetType).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
