using Fayora.Domain.Common.Entity;
using Fayora.Domain.Common.Interfaces;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.SharedModule;
using Fayora.Domain.Enums.PackageModule;
using Fayora.Domain.Errors;
using Fayora.Domain.ValueObjects;

namespace Fayora.Domain.Entities.PackageModule;

public class TravelPackage : AuditableEntity<Guid>, ICompanyOwned
{
    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public TourType TourTypes { get; private set; }
    public int DurationHours { get; private set; }
    public int NumOfDays { get; private set; }
    public int NumOfNights => NumOfDays > 1 ? NumOfDays - 1 : 0;

    public int MaxCapacity { get; private set; }
    public decimal AdultPrice { get; private set; }
    public decimal ChildPrice { get; private set; }
    public decimal DepositPercentage { get; private set; }
    public bool HasGroupDiscount { get; private set; }
    public int? GroupDiscountMinPeople { get; private set; }
    public decimal? GroupDiscountPercent { get; private set; }
    public bool IsActive { get; private set; }
    public int Views { get; private set; }
    public FileUrl MainImageUrl { get; private set; } = default!;
    public FileUrl? MainVideoUrl { get; private set; }
    public string? GuestRequirements { get; private set; }
    public CancellationPolicy CancellationPolicy { get; private set; }
    public string? RefundPolicyDetails { get; private set; }
    public CatalogItemStatus PackageStatus { get; private set; }

    private readonly List<int> _includedItemIds = [];
    public IReadOnlyCollection<int> IncludedItemIds => _includedItemIds.AsReadOnly();

    private readonly List<int>? _excludedItemIds = [];
    public IReadOnlyCollection<int> ExcludedItemIds => _excludedItemIds!.AsReadOnly();
    public string? ArrivalNote { get; private set; }
    public TransportType TransportType { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }

    private readonly List<int> _locationIds = [];
    public IReadOnlyCollection<int> LocationIds => _locationIds.AsReadOnly();

    public string? AdminNotes { get; private set; }

    private readonly List<Guid> _imageIds = [];
    public IReadOnlyCollection<Guid> ImageIds => _imageIds.AsReadOnly();
    public IReadOnlyCollection<Guid> ImageURLs => _imageIds.ToList().AsReadOnly();

    private readonly List<PackageOccurrence> _occurrences = [];
    public IReadOnlyCollection<PackageOccurrence> Occurrences => _occurrences.AsReadOnly();

    private TravelPackage() { }

    private TravelPackage(
        Guid ownerUserId,
        string title,
        string description,
        TourType tourTypes,
        int durationHours,
        int numOfDays,
        TransportType transportType,
        int maxCapacity,
        decimal adultPrice,
        decimal childPrice,
        string? arrivalNote,
        FileUrl mainImageUrl,
        FileUrl? mainVideoUrl,
        string? guestRequirements,
        CancellationPolicy cancellationPolicy,
        decimal depositPercentage,
        string? refundPolicyDetails)
    {
        Id = Guid.NewGuid();
        UserId = ownerUserId;
        Title = title;
        Description = description;
        TourTypes = tourTypes;
        DurationHours = durationHours;
        NumOfDays = numOfDays;
        TransportType = transportType;
        MaxCapacity = maxCapacity;
        AdultPrice = adultPrice;
        ChildPrice = childPrice;
        DepositPercentage = depositPercentage;
        ArrivalNote = arrivalNote;
        MainImageUrl = mainImageUrl;
        MainVideoUrl = mainVideoUrl;
        GuestRequirements = guestRequirements;

        // FayoraTravel companies control visibility directly from their dashboard.
        // A newly created package is visible by default and can be hidden at any time.
        IsActive = true;
        Views = 0;
        AverageRating = 0m;
        ReviewCount = 0;

        CancellationPolicy = cancellationPolicy;
        RefundPolicyDetails = NormalizeRefundPolicyDetails(refundPolicyDetails);
        PackageStatus = CatalogItemStatus.Active;
    }

    public static Result<TravelPackage> Create(
        Guid ownerUserId, string title, string description,
        TourType tourTypes, int durationHours, int numOfDays,
        TransportType transportType,
        int maxCapacity, decimal adultPrice, decimal childPrice,
        string? arrivalNote, FileUrl mainImageUrl,
        FileUrl? mainVideoUrl = null, string? guestRequirements = null, CancellationPolicy cancellationPolicy = CancellationPolicy.NonRefundable,
        bool hasGroupDiscount = false, int? groupDiscountMinPeople = null, decimal? groupDiscountPercent = null,
        decimal depositPercentage = 0, string? refundPolicyDetails = null)
    {
        if (adultPrice <= 0)
            return Error.Validation("Package.InvalidPrice", "Adult price must be positive.");

        if (durationHours <= 0)
            return Error.Validation("Package.InvalidDuration", "Duration must be greater than zero.");

        if (numOfDays <= 0)
            return Error.Validation("Package.InvalidNumOfDays", "Number of days must be greater than zero.");

        if (maxCapacity <= 0)
            return Error.Validation("Package.InvalidCapacity", "Max capacity must be greater than zero.");

        if (depositPercentage is < 0 or > 100)
            return Error.Validation("Package.InvalidDepositPercentage", "Deposit percentage must be between 0 and 100.");

        var package = new TravelPackage(ownerUserId, title, description, tourTypes,
            durationHours, numOfDays, transportType, maxCapacity,
            adultPrice, childPrice, arrivalNote, mainImageUrl,
            mainVideoUrl, guestRequirements, cancellationPolicy, depositPercentage, refundPolicyDetails);

        var discountResult = package.SetGroupDiscount(hasGroupDiscount, groupDiscountMinPeople, groupDiscountPercent);
        if (discountResult.IsError) return discountResult.Errors;

        return package;
    }

    public Result<Success> AssignToCompany(Guid companyId)
    {
        if (companyId == Guid.Empty)
            return Error.Validation("Package.CompanyRequired", "A company is required.");

        if (CompanyId != Guid.Empty && CompanyId != companyId)
            return Error.Conflict("Package.CompanyImmutable", "A package cannot be moved to another company.");

        CompanyId = companyId;
        return Result.Success;
    }

    public void AddIncludedItem(int id) => _includedItemIds.Add(id);
    public void AddExcludedItem(int id) => _excludedItemIds!.Add(id);
    public void AddImage(Guid imageId)
    {
        _imageIds.Add(imageId);
    }

    public void AddImages(IEnumerable<Guid> imageIds)
    {
        foreach (var id in imageIds) AddImage(id);
    }

    public Result<Success> Activate()
    {
        if (IsActive)
            return Error.Conflict("Package.AlreadyActive", "Package is already active.");
        IsActive = true;
        Updated();
        return Result.Success;
    }

    public Result<Success> Deactivate()
    {
        if (!IsActive)
            return Error.Conflict("Package.AlreadyInactive", "Package is already inactive.");

        IsActive = false;
        Updated();
        return Result.Success;
    }

    public Result<Success> Suspend()
    {
        var deactivateResult = Deactivate();
        if (deactivateResult.IsError) return deactivateResult;

        PackageStatus = CatalogItemStatus.Suspended;
        Updated();
        return Result.Success;
    }

    public void SetPublicVisibility(bool isVisible)
    {
        IsActive = isVisible;
        PackageStatus = isVisible ? CatalogItemStatus.Active : CatalogItemStatus.Suspended;
        Updated();
    }

    public void IncrementViews() => Views++;

    public Result<Success> UpdateDetails(
        string title,
        string description,
        int durationHours,
        int numOfDays,
        decimal adultPrice,
        decimal childPrice,
        TourType tourTypes,
        int maxCapacity,
        string? arrivalNote,
        TransportType transportType,
        string? guestRequirements,
        CancellationPolicy cancellationPolicy,
        FileUrl mainImageUrl,
        FileUrl? mainVideoUrl,
        bool hasGroupDiscount = false,
        int? groupDiscountMinPeople = null,
        decimal? groupDiscountPercent = null)
    {
        if (adultPrice <= 0)
            return Error.Validation("Package.InvalidPrice", "Adult price must be positive.");

        if (durationHours <= 0)
            return Error.Validation("Package.InvalidDuration", "Duration must be greater than zero.");

        if (numOfDays <= 0)
            return Error.Validation("Package.InvalidNumOfDays", "Number of days must be greater than zero.");

        if (maxCapacity <= 0)
            return Error.Validation("Package.InvalidCapacity", "Max capacity must be greater than zero.");

        var discountResult = SetGroupDiscount(hasGroupDiscount, groupDiscountMinPeople, groupDiscountPercent);
        if (discountResult.IsError) return discountResult;

        Title = title;
        Description = description;
        DurationHours = durationHours;
        NumOfDays = numOfDays;
        AdultPrice = adultPrice;
        ChildPrice = childPrice;
        TourTypes = tourTypes;
        MaxCapacity = maxCapacity;
        ArrivalNote = arrivalNote;
        TransportType = transportType;
        GuestRequirements = guestRequirements;
        CancellationPolicy = cancellationPolicy;
        MainImageUrl = mainImageUrl;
        MainVideoUrl = mainVideoUrl;
        Updated();
        return Result.Success;
    }

    public Result<Success> SetRefundPolicyDetails(string? refundPolicyDetails)
    {
        if (refundPolicyDetails?.Trim().Length > 2_000)
            return Error.Validation("Package.RefundPolicyTooLong", "Refund policy details cannot exceed 2000 characters.");
        RefundPolicyDetails = NormalizeRefundPolicyDetails(refundPolicyDetails);
        Updated();
        return Result.Success;
    }

    public Result<Success> SetDepositPercentage(decimal depositPercentage)
    {
        if (depositPercentage is < 0 or > 100)
            return Error.Validation("Package.InvalidDepositPercentage", "Deposit percentage must be between 0 and 100.");
        DepositPercentage = depositPercentage;
        Updated();
        return Result.Success;
    }

    public void UpdateIncludedItems(IEnumerable<int> ids)
    {
        _includedItemIds.Clear();
        _includedItemIds.AddRange(ids);
        Updated();
    }

    public void UpdateExcludedItems(IEnumerable<int> ids)
    {
        _excludedItemIds!.Clear();
        _excludedItemIds.AddRange(ids);
        Updated();
    }

    public void UpdateLocations(IEnumerable<int> locationIds)
    {
        _locationIds.Clear();
        _locationIds.AddRange(locationIds);
        Updated();
    }

    public void UpdateImages(IEnumerable<Guid> imageIds)
    {
        _imageIds.Clear();
        _imageIds.AddRange(imageIds);
        Updated();
    }

    public void AddLocation(int locationId) => _locationIds.Add(locationId);
    public void AddLocations(IEnumerable<int> locationIds) => _locationIds.AddRange(locationIds);

    public void AddIncludedItems(IEnumerable<int> ids)
    {
        foreach (var id in ids) _includedItemIds.Add(id);
    }

    public void AddExcludedItems(IEnumerable<int> ids)
    {
        foreach (var id in ids) _excludedItemIds!.Add(id);
    }

    public void Delete() => DeletedAt = DateTimeOffset.UtcNow;

    public Result<Success> Approve()
    {
        if (PackageStatus is CatalogItemStatus.Active)
            return Error.Validation("Package.AlreadyActive", "Package is already active.");

        PackageStatus = CatalogItemStatus.Active;
        return Result.Success;
    }

    public Result<Success> Reject(string adminNotes)
    {
        if (PackageStatus is CatalogItemStatus.Rejected)
            return Error.Validation("Package.AlreadyRejected", "Package is already rejected.");
        PackageStatus = CatalogItemStatus.Rejected;
        AdminNotes = adminNotes;
        return Result.Success;
    }

    public Result<decimal> CalculateGuestTotal(int numAdults, int numChildren)
    {
        if (numAdults < 1 || numChildren < 0)
            return Error.Validation("Package.InvalidBooking", "At least one adult is required and children cannot be negative.");
        var guests = numAdults + numChildren;
        if (guests > MaxCapacity)
            return Error.Validation("Package.OverCapacity", "Total number of guests exceeds package capacity.");
        var total = (AdultPrice * numAdults) + (ChildPrice * numChildren);
        if (HasGroupDiscount && GroupDiscountMinPeople.HasValue && GroupDiscountPercent.HasValue && guests >= GroupDiscountMinPeople.Value)
            total -= total * (GroupDiscountPercent.Value / 100m);
        return total;
    }

    private Result<Success> SetGroupDiscount(bool hasGroupDiscount, int? groupDiscountMinPeople, decimal? groupDiscountPercent)
    {
        if (hasGroupDiscount)
        {
            if (!groupDiscountMinPeople.HasValue || groupDiscountMinPeople.Value <= 0)
                return Error.Validation("Package.InvalidGroupDiscountMinPeople", "Group discount minimum people must be greater than zero.");

            if (!groupDiscountPercent.HasValue || groupDiscountPercent.Value <= 0 || groupDiscountPercent.Value > 100)
                return Error.Validation("Package.InvalidGroupDiscountPercent", "Group discount percent must be between 0 and 100.");
        }

        HasGroupDiscount = hasGroupDiscount;
        GroupDiscountMinPeople = hasGroupDiscount ? groupDiscountMinPeople : null;
        GroupDiscountPercent = hasGroupDiscount ? groupDiscountPercent : null;
        return Result.Success;
    }

    private static string? NormalizeRefundPolicyDetails(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();


    public Result<List<PackageOccurrence>> AddOccurrences(IEnumerable<(DateOnly Date, int AvailableSeats)> newOccurrences)
    {
        var requestedDates = newOccurrences.Select(x => x.Date).ToList();

        var hasDuplicatesInRequest = requestedDates.GroupBy(x => x).Any(g => g.Count() > 1);
        if (hasDuplicatesInRequest)
            return Error.Validation("Package.DuplicateOccurrenceDates", "Occurrence dates must be unique.");

        var hasOverlapWithExisting = _occurrences.Any(existing => requestedDates.Contains(existing.Date));
        if (hasOverlapWithExisting)
            return Error.Conflict("Package.OccurrenceOverlap", "One or more occurrence dates already exist.");

        var addedOccurrences = new List<PackageOccurrence>();
        foreach (var item in newOccurrences)
        {
            var occurrence = new PackageOccurrence(this.Id, item.Date, item.AvailableSeats);
            _occurrences.Add(occurrence);
            addedOccurrences.Add(occurrence);
        }

        return addedOccurrences;
    }

    public Result<Success> RemoveOccurrence(Guid occurrenceId)
    {
        var occurrence = _occurrences.FirstOrDefault(o => o.Id == occurrenceId);

        if (occurrence is null)
            return Error.NotFound("Package.OccurrenceNotFound", "The requested occurrence was not found.");


        _occurrences.Remove(occurrence);
        return Result.Success;
    }

    public Result<Success> SyncOccurrences(IEnumerable<(Guid? Id, DateOnly Date, int AvailableSeats)> requestedOccurrences)
    {
        var requestedList = requestedOccurrences.ToList();

        if (requestedList.GroupBy(x => x.Date).Any(g => g.Count() > 1))
            return Error.Validation("Package.DuplicateOccurrenceDates", "Occurrence dates must be unique.");

        var incomingIds = requestedList
            .Select(x => x.Id)
            .OfType<Guid>()
            .ToList();

        var occurrencesToRemove = _occurrences.Where(o => !incomingIds.Contains(o.Id)).ToList();
        foreach (var toRemove in occurrencesToRemove)
        {
            _occurrences.Remove(toRemove);
        }

        foreach (var request in requestedList)
        {
            if (request.Id is Guid occurrenceId)
            {
                var existingOccurrence = _occurrences.FirstOrDefault(o => o.Id == occurrenceId);
                if (existingOccurrence != null)
                {
                    if (existingOccurrence.Date != request.Date && _occurrences.Any(o => o.Id != existingOccurrence.Id && o.Date == request.Date))
                        return Error.Conflict("Package.OccurrenceOverlap", "One or more occurrence dates already exist.");

                    existingOccurrence.Update(request.Date, request.AvailableSeats);
                }
            }
            else
            {
                if (_occurrences.Any(o => o.Date == request.Date))
                    return Error.Conflict("Package.OccurrenceOverlap", "One or more occurrence dates already exist.");

                _occurrences.Add(new PackageOccurrence(this.Id, request.Date, request.AvailableSeats));
            }
        }

        return Result.Success;
    }

    public void AdminUpdate(
        string title,
        string description,
        int durationHours,
        int maxCapacity,
        decimal adultPrice,
        decimal childPrice,
        TourType tourTypes,
        CatalogItemStatus status)
    {
        Title = title;
        Description = description;
        DurationHours = durationHours;
        MaxCapacity = maxCapacity;
        AdultPrice = adultPrice;
        ChildPrice = childPrice;
        TourTypes = tourTypes;
        PackageStatus = status;
        Updated();
    }

    public void AddReview(decimal newRating)
    {
        AverageRating = ((AverageRating * ReviewCount) + newRating) / (ReviewCount + 1);
        ReviewCount++;
    }

    public void UpdateReview(decimal oldRating, decimal newRating)
    {
        if (ReviewCount > 0)
        {
            AverageRating = ((AverageRating * ReviewCount) - oldRating + newRating) / ReviewCount;
        }
    }

    public void DeleteReview(decimal rating)
    {
        if (ReviewCount > 1)
        {
            AverageRating = ((AverageRating * ReviewCount) - rating) / (ReviewCount - 1);
            ReviewCount--;
        }
        else
        {
            AverageRating = 0;
            ReviewCount = 0;
        }
    }
}
