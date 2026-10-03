using Fayora.Domain.Enums.SharedModule;
using Fayora.Domain.Enums.PackageModule;

namespace Fayora.Application.Features.CompanyModule.CompanyPackages;

public sealed record CompanyPackageResult(
    Guid Id, string Title, string Description, TourType TourTypes,
    int DurationHours, int NumOfDays, int NumOfNights, int MaxCapacity,
    decimal AdultPrice, decimal ChildPrice, string MainImageUrl, string? VideoUrl,
    IReadOnlyList<string> GalleryImageUrls, IReadOnlyList<int> IncludedItemIds,
    IReadOnlyList<int> ExcludedItemIds, string? GuestRequirements,
    CancellationPolicy CancellationPolicy, TransportType TransportType,
    string? ArrivalNote, decimal DepositPercentage, string? RefundPolicyDetails, bool IsPublished, CatalogItemStatus Status, DateTimeOffset CreatedAt,
    IReadOnlyList<Guid> AccommodationIds);

public sealed record CompanyPackagePageResult(
    IReadOnlyList<CompanyPackageResult> Items, int Page, int PageSize, int TotalCount);

public sealed record CompanyPackageInput(
    string Title, string Description, TourType TourTypes, int DurationHours, int NumOfDays,
    TransportType TransportType, string? ArrivalNote, decimal AdultPrice, decimal ChildPrice,
    int MaxCapacity, IReadOnlyList<int>? IncludedItemIds, IReadOnlyList<int>? ExcludedItemIds,
    string MainImageUrl, string? VideoUrl, IReadOnlyList<string>? GalleryImageUrls,
    string? GuestRequirements, CancellationPolicy CancellationPolicy, decimal DepositPercentage, string? RefundPolicyDetails);
