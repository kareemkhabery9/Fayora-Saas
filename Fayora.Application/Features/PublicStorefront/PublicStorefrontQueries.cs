using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.AccommodationModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Entities.PackageModule;
using Fayora.Domain.Enums.CompanyModule;

namespace Fayora.Application.Features.PublicStorefront;

public sealed record GetPublicCompanyQuery(string Slug) : IQuery<Result<PublicCompanyResult>>;
public sealed record GetPublicStorefrontOverviewQuery(string Slug) : IQuery<Result<PublicStorefrontOverviewResult>>;
public sealed record GetPublicPackagesQuery(string Slug, string? Search, int Page, int PageSize) : IQuery<Result<PublicPackagePageResult>>;
public sealed record GetPublicPackageDetailsQuery(string Slug, Guid PackageId) : IQuery<Result<PublicPackageDetailsResult>>;
public sealed record GetPublicAccommodationsQuery(string Slug, int Page, int PageSize) : IQuery<Result<PublicAccommodationPageResult>>;
public sealed record GetPublicAccommodationQuery(string Slug, Guid AccommodationId) : IQuery<Result<PublicAccommodationResult>>;
public sealed record GetPublicPackageCheckoutQuoteQuery(string Slug, Guid PackageId, int AdultsCount, int ChildrenCount,
    ManualPaymentMethodType PaymentMethodType, bool PayDepositOnly) : IQuery<Result<PublicCheckoutQuoteResult>>;

public sealed class PublicStorefrontQueryHandler(IPublicStorefrontRepository storefront)
    : IQueryHandler<GetPublicCompanyQuery, Result<PublicCompanyResult>>,
      IQueryHandler<GetPublicStorefrontOverviewQuery, Result<PublicStorefrontOverviewResult>>,
      IQueryHandler<GetPublicPackagesQuery, Result<PublicPackagePageResult>>,
      IQueryHandler<GetPublicPackageDetailsQuery, Result<PublicPackageDetailsResult>>,
      IQueryHandler<GetPublicAccommodationsQuery, Result<PublicAccommodationPageResult>>,
      IQueryHandler<GetPublicAccommodationQuery, Result<PublicAccommodationResult>>,
      IQueryHandler<GetPublicPackageCheckoutQuoteQuery, Result<PublicCheckoutQuoteResult>>
{
    public async Task<Result<PublicCompanyResult>> Handle(GetPublicCompanyQuery request, CancellationToken cancellationToken)
    {
        var company = await FindCompany(request.Slug, cancellationToken); if (company.IsError) return company.Errors;
        return ToCompanyResult(company.Value);
    }

    public async Task<Result<PublicStorefrontOverviewResult>> Handle(GetPublicStorefrontOverviewQuery request, CancellationToken cancellationToken)
    {
        var company = await FindCompany(request.Slug, cancellationToken); if (company.IsError) return company.Errors;
        var packages = await storefront.GetPackagesAsync(company.Value.Id, null, 1, 6, cancellationToken);
        var accommodations = await storefront.GetAccommodationsAsync(company.Value.Id, 1, 6, cancellationToken);
        var accommodationResults = new List<PublicAccommodationResult>();
        foreach (var accommodation in accommodations.Items)
            accommodationResults.Add(await ToAccommodationResult(accommodation, cancellationToken));
        return new PublicStorefrontOverviewResult(ToCompanyResult(company.Value), packages.Items.Select(ToPackageSummary).ToList(), accommodationResults);
    }

    public async Task<Result<PublicPackagePageResult>> Handle(GetPublicPackagesQuery request, CancellationToken cancellationToken)
    {
        if (!IsValidPage(request.Page, request.PageSize, out var error)) return error;
        var company = await FindCompany(request.Slug, cancellationToken); if (company.IsError) return company.Errors;
        var page = await storefront.GetPackagesAsync(company.Value.Id, request.Search, request.Page, request.PageSize, cancellationToken);
        return new PublicPackagePageResult(page.Items.Select(ToPackageSummary).ToList(), request.Page, request.PageSize, page.TotalCount);
    }

    public async Task<Result<PublicPackageDetailsResult>> Handle(GetPublicPackageDetailsQuery request, CancellationToken cancellationToken)
    {
        var company = await FindCompany(request.Slug, cancellationToken); if (company.IsError) return company.Errors;
        var package = await storefront.GetPackageAsync(company.Value.Id, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Public.PackageNotFound", "Package was not found.");
        var images = await storefront.GetPackageImagesAsync(package.Id, cancellationToken);
        var occurrences = await storefront.GetAvailableOccurrencesAsync(package.Id, cancellationToken);
        var accommodationIds = await storefront.GetAccommodationIdsForPackageAsync(package.Id, cancellationToken);
        var accommodations = await storefront.GetAccommodationsByIdsAsync(company.Value.Id, accommodationIds, cancellationToken);
        var accommodationResults = new List<PublicAccommodationResult>();
        foreach (var accommodation in accommodations)
            accommodationResults.Add(await ToAccommodationResult(accommodation, cancellationToken));
        return new PublicPackageDetailsResult(ToPackageSummary(package), package.MainVideoUrl?.Value,
            images.Select(x => x.ImageUrl.Value).ToList(), package.IncludedItemIds.ToList(), package.ExcludedItemIds.ToList(),
            package.GuestRequirements, package.CancellationPolicy, package.TransportType, package.ArrivalNote, package.RefundPolicyDetails,
            occurrences.Select(x => new PublicPackageOccurrenceResult(x.Id, x.Date, x.AvailableSeats)).ToList(), accommodationResults);
    }

    public async Task<Result<PublicAccommodationPageResult>> Handle(GetPublicAccommodationsQuery request, CancellationToken cancellationToken)
    {
        if (!IsValidPage(request.Page, request.PageSize, out var error)) return error;
        var company = await FindCompany(request.Slug, cancellationToken); if (company.IsError) return company.Errors;
        var page = await storefront.GetAccommodationsAsync(company.Value.Id, request.Page, request.PageSize, cancellationToken);
        var items = new List<PublicAccommodationResult>();
        foreach (var unit in page.Items) items.Add(await ToAccommodationResult(unit, cancellationToken));
        return new PublicAccommodationPageResult(items, request.Page, request.PageSize, page.TotalCount);
    }

    public async Task<Result<PublicAccommodationResult>> Handle(GetPublicAccommodationQuery request, CancellationToken cancellationToken)
    {
        var company = await FindCompany(request.Slug, cancellationToken); if (company.IsError) return company.Errors;
        var unit = await storefront.GetAccommodationAsync(company.Value.Id, request.AccommodationId, cancellationToken);
        return unit is null ? Error.NotFound("Public.AccommodationNotFound", "Accommodation was not found.")
            : await ToAccommodationResult(unit, cancellationToken);
    }

    public async Task<Result<PublicCheckoutQuoteResult>> Handle(GetPublicPackageCheckoutQuoteQuery request, CancellationToken cancellationToken)
    {
        var company = await FindCompany(request.Slug, cancellationToken); if (company.IsError) return company.Errors;
        var package = await storefront.GetPackageAsync(company.Value.Id, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Public.PackageNotFound", "Package was not found.");
        var total = package.CalculateGuestTotal(request.AdultsCount, request.ChildrenCount);
        if (total.IsError) return total.Errors;
        var method = company.Value.PaymentMethods.FirstOrDefault(x => x.Type == request.PaymentMethodType && x.IsEnabled);
        if (method is null) return Error.Validation("Booking.PaymentMethod", "Selected payment method is not available.");
        if (request.PayDepositOnly && package.DepositPercentage <= 0)
            return Error.Validation("Booking.DepositUnavailable", "This package does not offer deposit payment.");
        var minimumDeposit = decimal.Round(total.Value * package.DepositPercentage / 100m, 2, MidpointRounding.AwayFromZero);
        var dueNow = request.PayDepositOnly ? minimumDeposit : total.Value;
        var remaining = total.Value - dueNow;
        var instruction = request.PaymentMethodType == ManualPaymentMethodType.MobileWallet
            ? "حوّل المبلغ المطلوب إلى رقم المحفظة التالي، ثم اكتب رقم المحفظة التي حوّلت منها وارفع الإيصال."
            : "حوّل المبلغ المطلوب إلى حساب InstaPay التالي أو امسح QR الخاص به، ثم اكتب اسم حساب InstaPay الذي حوّلت منه وارفع الإيصال.";
        return new PublicCheckoutQuoteResult(total.Value, package.DepositPercentage, minimumDeposit, request.PayDepositOnly,
            dueNow, remaining, instruction, method.AccountIdentifier, method.InstaPayQrCodeUrl?.Value);
    }

    private async Task<Result<Company>> FindCompany(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug)) return Error.Validation("Public.InvalidSlug", "Company slug is required.");
        var company = await storefront.GetCompanyBySlugAsync(slug.Trim().ToLowerInvariant(), cancellationToken);
        return company is null ? Error.NotFound("Public.CompanyNotFound", "Company storefront was not found.") : company;
    }

    private async Task<PublicAccommodationResult> ToAccommodationResult(HousingUnit unit, CancellationToken cancellationToken)
    {
        var images = await storefront.GetAccommodationImagesAsync(unit.Id, cancellationToken);
        return new PublicAccommodationResult(unit.Id, unit.Title, unit.Description, unit.Type, unit.LocationId,
            unit.AddressDetails, unit.Coordinates.Latitude, unit.Coordinates.Longitude, unit.PricePerNight,
            unit.NumberOfRooms, unit.BedRooms, unit.BathRooms, unit.NumberOfBeds, unit.MaxGuests,
            unit.CheckInTime, unit.CheckOutTime, unit.MainImageUrl.Value, images.Select(x => x.ImageUrl.Value).ToList(),
            unit.Amenities.Select(x => x.Id).ToList());
    }

    private static PublicCompanyResult ToCompanyResult(Company company) => new(company.Name, company.Slug!, company.LogoUrl?.Value,
        company.CoverImageUrl?.Value, company.Description, company.FacebookUrl, company.InstagramUrl,
        company.WhatsAppNumbers.Select(x => x.PhoneNumber.Value).ToList(), company.Theme,
        company.PaymentMethods.Where(x => x.IsEnabled)
            .Select(x => new PublicPaymentMethodResult(x.Type, x.AccountIdentifier, x.InstaPayQrCodeUrl?.Value)).ToList());

    private static PublicPackageSummaryResult ToPackageSummary(TravelPackage package) => new(package.Id, package.Title,
        package.Description, package.TourTypes, package.DurationHours, package.NumOfDays, package.MaxCapacity,
        package.AdultPrice, package.ChildPrice, package.DepositPercentage, package.MainImageUrl.Value, package.MainVideoUrl is not null, package.CreatedAt);

    private static bool IsValidPage(int page, int pageSize, out Error error)
    {
        if (page >= 1 && pageSize is >= 1 and <= 100) { error = null!; return true; }
        error = Error.Validation("Pagination.Invalid", "Page must be positive and pageSize must be between 1 and 100."); return false;
    }
}
