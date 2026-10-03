namespace Fayora.Domain.Entities.CompanyModule;

/// <summary>
/// Optional accommodation listings included or recommended by a travel package.
/// Both sides are scoped to the same company by the application layer.
/// </summary>
public sealed class PackageAccommodationLink
{
    public Guid PackageId { get; private set; }
    public Guid AccommodationId { get; private set; }

    private PackageAccommodationLink() { }

    public PackageAccommodationLink(Guid packageId, Guid accommodationId)
    {
        PackageId = packageId;
        AccommodationId = accommodationId;
    }
}
