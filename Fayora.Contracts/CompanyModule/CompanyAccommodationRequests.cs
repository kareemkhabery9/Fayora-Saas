namespace Fayora.Contracts.CompanyModule;

public sealed record CompanyAccommodationRequest(
    string Title, string? Description, string Type, int LocationId, string AddressDetails,
    decimal Latitude, decimal Longitude, decimal PricePerNight, int NumberOfRooms, int BedRooms,
    int BathRooms, int NumberOfBeds, int MaxGuests, TimeSpan CheckInTime, TimeSpan CheckOutTime,
    string MainImageUrl, List<string>? GalleryImageUrls, List<int>? AmenityIds);

public sealed record SetCompanyAccommodationVisibilityRequest(bool IsVisible);
