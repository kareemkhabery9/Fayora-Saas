using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.PackageModule;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.Enums.PackageModule;

namespace Fayora.Application.Features.CompanyModule.CompanyPackages;

public sealed record OccurrenceInput(DateOnly Date, int AvailableSeats);
public sealed record CompanyPackageOccurrenceResult(Guid Id, DateOnly Date, int AvailableSeats, OccurrenceStatus Status);
public sealed record CreateCompanyPackageOccurrencesCommand(Guid PackageId, IReadOnlyList<OccurrenceInput> Occurrences)
    : ICommand<Result<IReadOnlyList<CompanyPackageOccurrenceResult>>>;
public sealed record UpdateCompanyPackageOccurrenceCommand(Guid PackageId, Guid OccurrenceId, OccurrenceInput Input)
    : ICommand<Result<CompanyPackageOccurrenceResult>>;
public sealed record CancelCompanyPackageOccurrenceCommand(Guid PackageId, Guid OccurrenceId)
    : ICommand<Result<CompanyPackageOccurrenceResult>>;
public sealed record DeleteCompanyPackageOccurrenceCommand(Guid PackageId, Guid OccurrenceId) : ICommand<Result<Success>>;

public abstract class CompanyPackageOccurrenceHandlerBase(
    ICompanyRepository companies, IClientContextProvider context)
{
    protected async Task<Result<Guid>> GetCompanyId(CancellationToken cancellationToken)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        return company is null ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.") : company.Id;
    }

    protected static CompanyPackageOccurrenceResult ToResult(PackageOccurrence occurrence) =>
        new(occurrence.Id, occurrence.Date, occurrence.AvailableSeats, occurrence.Status);

    protected static Result<Success> ValidateInput(OccurrenceInput input, int maxCapacity)
    {
        if (input.Date < DateOnly.FromDateTime(DateTime.UtcNow))
            return Error.Validation("Occurrence.DatePast", "The trip date cannot be in the past.");
        if (input.AvailableSeats is < 1 or > 10000)
            return Error.Validation("Occurrence.InvalidSeats", "Available seats must be between 1 and 10000.");
        if (input.AvailableSeats > maxCapacity)
            return Error.Validation("Occurrence.CapacityExceeded", "Available seats cannot exceed the package capacity.");
        return Result.Success;
    }

    protected static bool HasDurationConflict(IEnumerable<PackageOccurrence> occurrences, Guid? ignoredId,
        DateOnly proposedDate, int packageDays) =>
        occurrences.Any(x => x.Id != ignoredId && x.Status != OccurrenceStatus.Cancelled &&
            proposedDate < x.Date.AddDays(packageDays) && x.Date < proposedDate.AddDays(packageDays));
}

public sealed class CreateCompanyPackageOccurrencesCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages, IUnitOfWork unitOfWork)
    : CompanyPackageOccurrenceHandlerBase(companies, context), ICommandHandler<CreateCompanyPackageOccurrencesCommand, Result<IReadOnlyList<CompanyPackageOccurrenceResult>>>
{
    public async Task<Result<IReadOnlyList<CompanyPackageOccurrenceResult>>> Handle(CreateCompanyPackageOccurrencesCommand request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var package = await packages.GetByIdAsync(companyId.Value, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Package.NotFound", "Package was not found.");
        if (request.Occurrences is null || request.Occurrences.Count == 0)
            return Error.Validation("Occurrence.Required", "At least one trip date is required.");
        if (request.Occurrences.GroupBy(x => x.Date).Any(x => x.Count() > 1))
            return Error.Validation("Occurrence.DuplicateDate", "The request contains duplicate dates.");
        foreach (var item in request.Occurrences)
        {
            var validation = ValidateInput(item, package.MaxCapacity);
            if (validation.IsError) return validation.Errors;
        }
        var existing = await packages.GetOccurrencesAsync(companyId.Value, package.Id, cancellationToken);
        var newOccurrences = new List<PackageOccurrence>();
        foreach (var item in request.Occurrences.OrderBy(x => x.Date))
        {
            if (HasDurationConflict(existing.Concat(newOccurrences), null, item.Date, package.NumOfDays))
                return Error.Conflict("Occurrence.DateConflict", "This trip overlaps an existing trip date.");
            newOccurrences.Add(new PackageOccurrence(package.Id, item.Date, item.AvailableSeats));
        }
        packages.AddOccurrences(newOccurrences);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return newOccurrences.Select(ToResult).ToList();
    }
}

public sealed class UpdateCompanyPackageOccurrenceCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages,
    ICompanyPackageBookingRepository manualBookings, IUnitOfWork unitOfWork)
    : CompanyPackageOccurrenceHandlerBase(companies, context), ICommandHandler<UpdateCompanyPackageOccurrenceCommand, Result<CompanyPackageOccurrenceResult>>
{
    public async Task<Result<CompanyPackageOccurrenceResult>> Handle(UpdateCompanyPackageOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var package = await packages.GetByIdAsync(companyId.Value, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Package.NotFound", "Package was not found.");
        var occurrence = await packages.GetOccurrenceAsync(companyId.Value, package.Id, request.OccurrenceId, cancellationToken);
        if (occurrence is null) return Error.NotFound("Occurrence.NotFound", "Trip date was not found.");
        var validation = ValidateInput(request.Input, package.MaxCapacity); if (validation.IsError) return validation.Errors;
        if (await manualBookings.HasNonRejectedBookingsForOccurrenceAsync(package.Id, occurrence.Date, cancellationToken))
            return Error.Conflict("Occurrence.HasBookings", "A trip with bookings cannot be changed.");
        var existing = await packages.GetOccurrencesAsync(companyId.Value, package.Id, cancellationToken);
        if (HasDurationConflict(existing, occurrence.Id, request.Input.Date, package.NumOfDays))
            return Error.Conflict("Occurrence.DateConflict", "This trip overlaps an existing trip date.");
        occurrence.Update(request.Input.Date, request.Input.AvailableSeats);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return ToResult(occurrence);
    }
}

public sealed class CancelCompanyPackageOccurrenceCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages, IUnitOfWork unitOfWork)
    : CompanyPackageOccurrenceHandlerBase(companies, context), ICommandHandler<CancelCompanyPackageOccurrenceCommand, Result<CompanyPackageOccurrenceResult>>
{
    public async Task<Result<CompanyPackageOccurrenceResult>> Handle(CancelCompanyPackageOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var occurrence = await packages.GetOccurrenceAsync(companyId.Value, request.PackageId, request.OccurrenceId, cancellationToken);
        if (occurrence is null) return Error.NotFound("Occurrence.NotFound", "Trip date was not found.");
        occurrence.Cancel(); await unitOfWork.CommitChangesAsync(cancellationToken); return ToResult(occurrence);
    }
}

public sealed class DeleteCompanyPackageOccurrenceCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages,
    ICompanyPackageBookingRepository manualBookings, IUnitOfWork unitOfWork)
    : CompanyPackageOccurrenceHandlerBase(companies, context), ICommandHandler<DeleteCompanyPackageOccurrenceCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(DeleteCompanyPackageOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var occurrence = await packages.GetOccurrenceAsync(companyId.Value, request.PackageId, request.OccurrenceId, cancellationToken);
        if (occurrence is null) return Error.NotFound("Occurrence.NotFound", "Trip date was not found.");
        if (await manualBookings.HasNonRejectedBookingsForOccurrenceAsync(request.PackageId, occurrence.Date, cancellationToken))
            return Error.Conflict("Occurrence.HasBookings", "A trip with bookings cannot be deleted.");
        packages.RemoveOccurrence(occurrence); await unitOfWork.CommitChangesAsync(cancellationToken); return Result.Success;
    }
}
