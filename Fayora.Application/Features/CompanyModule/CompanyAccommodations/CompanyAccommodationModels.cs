using Fayora.Domain.Enums.SharedModule;
using Fayora.Domain.Enums.AccommodationModule;
using Fayora.Domain.Enums.PackageModule;

namespace Fayora.Application.Features.CompanyModule.CompanyAccommodations;

public sealed record CompanyAccommodationInput(
    string Title, string? Description, HousingType Type, int LocationId, string AddressDetails,
    decimal Latitude, decimal Longitude, decimal PricePerNight, int NumberOfRooms, int BedRooms,
    int BathRooms, int NumberOfBeds, int MaxGuests, TimeSpan CheckInTime, TimeSpan CheckOutTime,
    string MainImageUrl, IReadOnlyList<string>? GalleryImageUrls, IReadOnlyList<int>? AmenityIds);

public sealed record CompanyAccommodationResult(
    Guid Id, string Title, string? Description, HousingType Type, int LocationId, string AddressDetails,
    decimal Latitude, decimal Longitude, decimal PricePerNight, int NumberOfRooms, int BedRooms,
    int BathRooms, int NumberOfBeds, int MaxGuests, TimeSpan CheckInTime, TimeSpan CheckOutTime,
    string MainImageUrl, IReadOnlyList<string> GalleryImageUrls, IReadOnlyList<int> AmenityIds,
    bool IsVisible, CatalogItemStatus Status, DateTimeOffset CreatedAt);

public sealed record CompanyAccommodationPageResult(IReadOnlyList<CompanyAccommodationResult> Items, int Page, int PageSize, int TotalCount);
