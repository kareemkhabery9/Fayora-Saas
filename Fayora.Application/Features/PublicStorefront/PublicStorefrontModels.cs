using Fayora.Domain.Enums.AccommodationModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.SharedModule;
using Fayora.Domain.Enums.PackageModule;

namespace Fayora.Application.Features.PublicStorefront;

public sealed record PublicCompanyResult(string Name, string Slug, string? LogoUrl, string? CoverImageUrl,
    string? Description, string? FacebookUrl, string? InstagramUrl, IReadOnlyList<string> WhatsAppNumbers, CompanyTheme Theme,
    IReadOnlyList<PublicPaymentMethodResult> PaymentMethods);

public sealed record PublicPaymentMethodResult(ManualPaymentMethodType Type, string AccountIdentifier, string? InstaPayQrCodeUrl);

public sealed record PublicPackageSummaryResult(Guid Id, string Title, string Description, TourType TourTypes,
    int DurationHours, int NumOfDays, int MaxCapacity, decimal AdultPrice, decimal ChildPrice, decimal DepositPercentage,
    string MainImageUrl, bool HasVideo, DateTimeOffset CreatedAt);

public sealed record PublicPackageOccurrenceResult(Guid Id, DateOnly Date, int AvailableSeats);

public sealed record PublicAccommodationResult(Guid Id, string Title, string? Description, HousingType Type,
    int LocationId, string AddressDetails, decimal Latitude, decimal Longitude, decimal PricePerNight,
    int NumberOfRooms, int BedRooms, int BathRooms, int NumberOfBeds, int MaxGuests, TimeSpan CheckInTime,
    TimeSpan CheckOutTime, string MainImageUrl, IReadOnlyList<string> GalleryImageUrls, IReadOnlyList<int> AmenityIds);

public sealed record PublicPackageDetailsResult(PublicPackageSummaryResult Package, string? VideoUrl,
    IReadOnlyList<string> GalleryImageUrls, IReadOnlyList<int> IncludedItemIds, IReadOnlyList<int> ExcludedItemIds,
    string? GuestRequirements, CancellationPolicy CancellationPolicy, TransportType TransportType,
    string? ArrivalNote, string? RefundPolicyDetails, IReadOnlyList<PublicPackageOccurrenceResult> AvailableOccurrences,
    IReadOnlyList<PublicAccommodationResult> Accommodations);

public sealed record PublicPackagePageResult(IReadOnlyList<PublicPackageSummaryResult> Items, int Page, int PageSize, int TotalCount);
public sealed record PublicAccommodationPageResult(IReadOnlyList<PublicAccommodationResult> Items, int Page, int PageSize, int TotalCount);

public sealed record PublicStorefrontOverviewResult(PublicCompanyResult Company,
    IReadOnlyList<PublicPackageSummaryResult> FeaturedPackages,
    IReadOnlyList<PublicAccommodationResult> FeaturedAccommodations);

public sealed record PublicCheckoutQuoteResult(decimal TotalAmount, decimal DepositPercentage, decimal MinimumDepositAmount,
    bool IsDepositPayment, decimal AmountDueNow, decimal RemainingAmount, string PaymentInstruction,
    string PaymentAccountIdentifier, string? InstaPayQrCodeUrl);
