using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.PackageModule;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.Enums.PackageModule;
using Fayora.Domain.ValueObjects;

namespace Fayora.Application.Features.CompanyModule.CompanyPackages;

public sealed record CreateCompanyPackageCommand(CompanyPackageInput Input) : ICommand<Result<CompanyPackageResult>>;
public sealed record UpdateCompanyPackageCommand(Guid PackageId, CompanyPackageInput Input) : ICommand<Result<CompanyPackageResult>>;
public sealed record PublishCompanyPackageCommand(Guid PackageId) : ICommand<Result<CompanyPackageResult>>;
public sealed record UnpublishCompanyPackageCommand(Guid PackageId) : ICommand<Result<CompanyPackageResult>>;
public sealed record SetCompanyPackageVisibilityCommand(Guid PackageId, bool IsVisible) : ICommand<Result<CompanyPackageResult>>;
public sealed record UpdateCompanyPackageAccommodationsCommand(Guid PackageId, IReadOnlyList<Guid> AccommodationIds) : ICommand<Result<CompanyPackageResult>>;
public sealed record DeleteCompanyPackageCommand(Guid PackageId) : ICommand<Result<Success>>;

public abstract class CompanyPackageHandlerBase(
    ICompanyRepository companies,
    IClientContextProvider clientContextProvider)
{
    protected async Task<Result<(Guid CompanyId, Guid UserId)>> GetCompanyContext(CancellationToken cancellationToken)
    {
        var context = clientContextProvider.GetContext();
        if (!context.Roles.Contains(Role.Company.ToString()))
            return Error.Forbidden("Company role is required.");

        var company = await companies.GetByOwnerIdAsync(context.UserId, cancellationToken);
        return company is null
            ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.")
            : (company.Id, context.UserId);
    }

    internal static async Task<Result<CompanyPackageResult>> ToResult(
        TravelPackage package, ICompanyPackageRepository packages, CancellationToken cancellationToken)
    {
        var images = await packages.GetImagesAsync(package.Id, cancellationToken);
        var accommodationIds = await packages.GetAccommodationIdsAsync(package.Id, cancellationToken);
        return new CompanyPackageResult(package.Id, package.Title, package.Description, package.TourTypes,
            package.DurationHours, package.NumOfDays, package.NumOfNights, package.MaxCapacity,
            package.AdultPrice, package.ChildPrice, package.MainImageUrl.Value, package.MainVideoUrl?.Value,
            images.Select(x => x.ImageUrl.Value).ToList(), package.IncludedItemIds.ToList(),
            package.ExcludedItemIds.ToList(), package.GuestRequirements, package.CancellationPolicy,
            package.TransportType, package.ArrivalNote, package.DepositPercentage, package.RefundPolicyDetails, package.IsActive,
            package.PackageStatus, package.CreatedAt, accommodationIds);
    }

    protected static Result<(FileUrl MainImage, FileUrl? Video, List<PackageImage> Gallery)> BuildMedia(
        Guid packageId, CompanyPackageInput input)
    {
        var main = FileUrl.Create(input.MainImageUrl);
        if (main.IsError) return main.Errors;
        FileUrl? video = null;
        if (!string.IsNullOrWhiteSpace(input.VideoUrl))
        {
            var videoResult = FileUrl.Create(input.VideoUrl);
            if (videoResult.IsError) return videoResult.Errors;
            video = videoResult.Value;
        }
        var gallery = new List<PackageImage>();
        foreach (var value in input.GalleryImageUrls ?? [])
        {
            var image = FileUrl.Create(value);
            if (image.IsError) return image.Errors;
            gallery.Add(new PackageImage(packageId, image.Value));
        }
        return (main.Value, video, gallery);
    }
}

public sealed class CreateCompanyPackageCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages, IUnitOfWork unitOfWork)
    : CompanyPackageHandlerBase(companies, context), ICommandHandler<CreateCompanyPackageCommand, Result<CompanyPackageResult>>
{
    public async Task<Result<CompanyPackageResult>> Handle(CreateCompanyPackageCommand request, CancellationToken cancellationToken)
    {
        var companyContext = await GetCompanyContext(cancellationToken);
        if (companyContext.IsError) return companyContext.Errors;
        var media = BuildMedia(Guid.Empty, request.Input);
        if (media.IsError) return media.Errors;
        var input = request.Input;
        var creation = TravelPackage.Create(companyContext.Value.UserId, input.Title, input.Description,
            input.TourTypes, input.DurationHours, input.NumOfDays,
            input.TransportType, input.MaxCapacity, input.AdultPrice, input.ChildPrice, input.ArrivalNote,
            media.Value.MainImage, media.Value.Video, input.GuestRequirements, input.CancellationPolicy,
            depositPercentage: input.DepositPercentage, refundPolicyDetails: input.RefundPolicyDetails);
        if (creation.IsError) return creation.Errors;
        var package = creation.Value;
        var ownership = package.AssignToCompany(companyContext.Value.CompanyId);
        if (ownership.IsError) return ownership.Errors;
        package.AddIncludedItems(input.IncludedItemIds ?? []);
        package.AddExcludedItems(input.ExcludedItemIds ?? []);
        var resolvedMedia = BuildMedia(package.Id, input);
        if (resolvedMedia.IsError) return resolvedMedia.Errors;
        package.AddImages(resolvedMedia.Value.Gallery.Select(x => x.Id));
        packages.Add(package);
        packages.ReplaceImages(package.Id, resolvedMedia.Value.Gallery);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return await ToResult(package, packages, cancellationToken);
    }
}

public sealed class UpdateCompanyPackageCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages, IUnitOfWork unitOfWork)
    : CompanyPackageHandlerBase(companies, context), ICommandHandler<UpdateCompanyPackageCommand, Result<CompanyPackageResult>>
{
    public async Task<Result<CompanyPackageResult>> Handle(UpdateCompanyPackageCommand request, CancellationToken cancellationToken)
    {
        var companyContext = await GetCompanyContext(cancellationToken);
        if (companyContext.IsError) return companyContext.Errors;
        var package = await packages.GetByIdAsync(companyContext.Value.CompanyId, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Package.NotFound", "Package was not found.");
        var media = BuildMedia(package.Id, request.Input);
        if (media.IsError) return media.Errors;
        var input = request.Input;
        var update = package.UpdateDetails(input.Title, input.Description, input.DurationHours, input.NumOfDays,
            input.AdultPrice, input.ChildPrice, input.TourTypes, input.MaxCapacity, input.ArrivalNote,
            input.TransportType, input.GuestRequirements, input.CancellationPolicy, media.Value.MainImage,
            media.Value.Video);
        if (update.IsError) return update.Errors;
        var deposit = package.SetDepositPercentage(input.DepositPercentage);
        if (deposit.IsError) return deposit.Errors;
        var refundPolicy = package.SetRefundPolicyDetails(input.RefundPolicyDetails);
        if (refundPolicy.IsError) return refundPolicy.Errors;
        package.UpdateIncludedItems(input.IncludedItemIds ?? []);
        package.UpdateExcludedItems(input.ExcludedItemIds ?? []);
        package.UpdateImages(media.Value.Gallery.Select(x => x.Id));
        packages.ReplaceImages(package.Id, media.Value.Gallery);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return await ToResult(package, packages, cancellationToken);
    }
}

public sealed class PublishCompanyPackageCommandHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages, IUnitOfWork unitOfWork)
    : CompanyPackageHandlerBase(companies, context), ICommandHandler<PublishCompanyPackageCommand, Result<CompanyPackageResult>>
{
    public async Task<Result<CompanyPackageResult>> Handle(PublishCompanyPackageCommand request, CancellationToken cancellationToken)
    {
        var companyContext = await GetCompanyContext(cancellationToken); if (companyContext.IsError) return companyContext.Errors;
        var package = await packages.GetByIdAsync(companyContext.Value.CompanyId, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Package.NotFound", "Package was not found.");
        var active = package.Activate(); if (active.IsError) return active.Errors;
        var approval = package.Approve(); if (approval.IsError) return approval.Errors;
        await unitOfWork.CommitChangesAsync(cancellationToken); return await ToResult(package, packages, cancellationToken);
    }
}

public sealed class UnpublishCompanyPackageCommandHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages, IUnitOfWork unitOfWork)
    : CompanyPackageHandlerBase(companies, context), ICommandHandler<UnpublishCompanyPackageCommand, Result<CompanyPackageResult>>
{
    public async Task<Result<CompanyPackageResult>> Handle(UnpublishCompanyPackageCommand request, CancellationToken cancellationToken)
    {
        var companyContext = await GetCompanyContext(cancellationToken); if (companyContext.IsError) return companyContext.Errors;
        var package = await packages.GetByIdAsync(companyContext.Value.CompanyId, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Package.NotFound", "Package was not found.");
        var suspended = package.Suspend(); if (suspended.IsError) return suspended.Errors;
        await unitOfWork.CommitChangesAsync(cancellationToken); return await ToResult(package, packages, cancellationToken);
    }
}

public sealed class SetCompanyPackageVisibilityCommandHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages, IUnitOfWork unitOfWork)
    : CompanyPackageHandlerBase(companies, context), ICommandHandler<SetCompanyPackageVisibilityCommand, Result<CompanyPackageResult>>
{
    public async Task<Result<CompanyPackageResult>> Handle(SetCompanyPackageVisibilityCommand request, CancellationToken cancellationToken)
    {
        var companyContext = await GetCompanyContext(cancellationToken);
        if (companyContext.IsError) return companyContext.Errors;
        var package = await packages.GetByIdAsync(companyContext.Value.CompanyId, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Package.NotFound", "Package was not found.");

        package.SetPublicVisibility(request.IsVisible);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return await ToResult(package, packages, cancellationToken);
    }
}

public sealed class UpdateCompanyPackageAccommodationsCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages,
    ICompanyAccommodationRepository accommodations, IUnitOfWork unitOfWork)
    : CompanyPackageHandlerBase(companies, context), ICommandHandler<UpdateCompanyPackageAccommodationsCommand, Result<CompanyPackageResult>>
{
    public async Task<Result<CompanyPackageResult>> Handle(UpdateCompanyPackageAccommodationsCommand request, CancellationToken cancellationToken)
    {
        var companyContext = await GetCompanyContext(cancellationToken);
        if (companyContext.IsError) return companyContext.Errors;
        var package = await packages.GetByIdAsync(companyContext.Value.CompanyId, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Package.NotFound", "Package was not found.");

        var requestedAccommodationIds = request.AccommodationIds ?? [];
        var accommodationIds = requestedAccommodationIds.Distinct().ToList();
        if (accommodationIds.Count != requestedAccommodationIds.Count)
            return Error.Validation("Package.Accommodations", "An accommodation can only be added once.");
        if (accommodationIds.Count > 0)
        {
            var ownedCount = await accommodations.CountOwnedAsync(companyContext.Value.CompanyId, accommodationIds, cancellationToken);
            if (ownedCount != accommodationIds.Count)
                return Error.Validation("Package.Accommodations", "Every selected accommodation must belong to your company.");
        }
        packages.ReplaceAccommodationLinks(package.Id, accommodationIds);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return await ToResult(package, packages, cancellationToken);
    }
}

public sealed class DeleteCompanyPackageCommandHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages, IUnitOfWork unitOfWork)
    : CompanyPackageHandlerBase(companies, context), ICommandHandler<DeleteCompanyPackageCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(DeleteCompanyPackageCommand request, CancellationToken cancellationToken)
    {
        var companyContext = await GetCompanyContext(cancellationToken); if (companyContext.IsError) return companyContext.Errors;
        var package = await packages.GetByIdAsync(companyContext.Value.CompanyId, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Package.NotFound", "Package was not found.");
        package.Delete(); await unitOfWork.CommitChangesAsync(cancellationToken); return Result.Success;
    }
}
