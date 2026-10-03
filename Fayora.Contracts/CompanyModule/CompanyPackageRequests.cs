namespace Fayora.Contracts.CompanyModule;

public sealed record CompanyPackageRequest(
    string Title,
    string Description,
    string TourTypes,
    int DurationHours,
    int NumOfDays,
    string TransportType,
    string? ArrivalNote,
    decimal AdultPrice,
    decimal ChildPrice,
    int MaxCapacity,
    List<int>? IncludedItemIds,
    List<int>? ExcludedItemIds,
    string MainImageUrl,
    string? VideoUrl,
    List<string>? GalleryImageUrls,
    string? GuestRequirements,
    string CancellationPolicy,
    decimal DepositPercentage = 0,
    string? RefundPolicyDetails = null);

public sealed record SetCompanyPackageVisibilityRequest(bool IsVisible);
