using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.AccommodationModule;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Persistences.SharedModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.AccommodationModule;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.Enums.PackageModule;
using Fayora.Domain.ValueObjects;

namespace Fayora.Application.Features.CompanyModule.CompanyAccommodations;

public sealed record CreateCompanyAccommodationCommand(CompanyAccommodationInput Input) : ICommand<Result<CompanyAccommodationResult>>;
public sealed record UpdateCompanyAccommodationCommand(Guid AccommodationId, CompanyAccommodationInput Input) : ICommand<Result<CompanyAccommodationResult>>;
public sealed record SetCompanyAccommodationVisibilityCommand(Guid AccommodationId, bool IsVisible) : ICommand<Result<CompanyAccommodationResult>>;
public sealed record DeleteCompanyAccommodationCommand(Guid AccommodationId) : ICommand<Result<Success>>;

public abstract class CompanyAccommodationHandlerBase(ICompanyRepository companies, IClientContextProvider context)
{
    protected async Task<Result<(Guid CompanyId, Guid UserId)>> GetCompanyContext(CancellationToken cancellationToken)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        return company is null ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.") : (company.Id, current.UserId);
    }

    internal static async Task<Result<CompanyAccommodationResult>> ToResult(HousingUnit unit, ICompanyAccommodationRepository units, CancellationToken cancellationToken)
    {
        var images = await units.GetImagesAsync(unit.Id, cancellationToken);
        return new CompanyAccommodationResult(unit.Id, unit.Title, unit.Description, unit.Type, unit.LocationId,
            unit.AddressDetails, unit.Coordinates.Latitude, unit.Coordinates.Longitude, unit.PricePerNight,
            unit.NumberOfRooms, unit.BedRooms, unit.BathRooms, unit.NumberOfBeds, unit.MaxGuests,
            unit.CheckInTime, unit.CheckOutTime, unit.MainImageUrl.Value, images.Select(x => x.ImageUrl.Value).ToList(),
            unit.Amenities.Select(x => x.Id).ToList(), unit.Status == CatalogItemStatus.Active, unit.Status, unit.CreatedAt);
    }

    protected static Result<(GeoPoint Coordinates, FileUrl MainImage, List<HousingUnitImage> Images)> BuildMedia(Guid unitId, CompanyAccommodationInput input)
    {
        var coordinates = GeoPoint.Create(input.Latitude, input.Longitude); if (coordinates.IsError) return coordinates.Errors;
        var mainImage = FileUrl.Create(input.MainImageUrl); if (mainImage.IsError) return mainImage.Errors;
        var images = new List<HousingUnitImage>();
        foreach (var url in input.GalleryImageUrls ?? [])
        {
            var image = FileUrl.Create(url); if (image.IsError) return image.Errors;
            images.Add(new HousingUnitImage(unitId, image.Value));
        }
        return (coordinates.Value, mainImage.Value, images);
    }

    protected static Result<Success> ValidateInput(CompanyAccommodationInput input)
    {
        if (input.LocationId < 1) return Error.Validation("Accommodation.LocationRequired", "A location is required.");
        return Result.Success;
    }
}

public sealed class CreateCompanyAccommodationCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyAccommodationRepository units,
    IMasterAmenityRepository amenities, ILocationRepository locations, IUnitOfWork unitOfWork)
    : CompanyAccommodationHandlerBase(companies, context), ICommandHandler<CreateCompanyAccommodationCommand, Result<CompanyAccommodationResult>>
{
    public async Task<Result<CompanyAccommodationResult>> Handle(CreateCompanyAccommodationCommand request, CancellationToken cancellationToken)
    {
        var company = await GetCompanyContext(cancellationToken); if (company.IsError) return company.Errors;
        var validation = ValidateInput(request.Input); if (validation.IsError) return validation.Errors;
        if (!await locations.LocationExistsAsync(request.Input.LocationId, cancellationToken)) return Error.Validation("Accommodation.Location", "Selected location does not exist.");
        var amenityIds = request.Input.AmenityIds?.Distinct().ToList() ?? [];
        var selectedAmenities = await amenities.GetByIdsAsync(amenityIds, cancellationToken);
        if (selectedAmenities.Count != amenityIds.Count) return Error.Validation("Accommodation.Amenities", "One or more amenities are invalid.");
        var preMedia = BuildMedia(Guid.Empty, request.Input); if (preMedia.IsError) return preMedia.Errors;
        var creation = HousingUnit.Create(company.Value.UserId, request.Input.Title, request.Input.Description,
            request.Input.LocationId, request.Input.AddressDetails, preMedia.Value.Coordinates, request.Input.Type,
            request.Input.PricePerNight, request.Input.NumberOfRooms, request.Input.BedRooms, request.Input.BathRooms,
            request.Input.NumberOfBeds, request.Input.MaxGuests, request.Input.CheckInTime, request.Input.CheckOutTime,
            preMedia.Value.MainImage, null, selectedAmenities);
        if (creation.IsError) return creation.Errors;
        var unit = creation.Value;
        var ownership = unit.AssignToCompany(company.Value.CompanyId); if (ownership.IsError) return ownership.Errors;
        var media = BuildMedia(unit.Id, request.Input); if (media.IsError) return media.Errors;
        unit.AddImages(media.Value.Images.Select(x => x.Id));
        units.Add(unit); units.ReplaceImages(unit.Id, media.Value.Images);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return await ToResult(unit, units, cancellationToken);
    }
}

public sealed class UpdateCompanyAccommodationCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyAccommodationRepository units,
    IMasterAmenityRepository amenities, ILocationRepository locations, IUnitOfWork unitOfWork)
    : CompanyAccommodationHandlerBase(companies, context), ICommandHandler<UpdateCompanyAccommodationCommand, Result<CompanyAccommodationResult>>
{
    public async Task<Result<CompanyAccommodationResult>> Handle(UpdateCompanyAccommodationCommand request, CancellationToken cancellationToken)
    {
        var company = await GetCompanyContext(cancellationToken); if (company.IsError) return company.Errors;
        var unit = await units.GetByIdAsync(company.Value.CompanyId, request.AccommodationId, true, cancellationToken);
        if (unit is null) return Error.NotFound("Accommodation.NotFound", "Accommodation was not found.");
        var validation = ValidateInput(request.Input); if (validation.IsError) return validation.Errors;
        if (!await locations.LocationExistsAsync(request.Input.LocationId, cancellationToken)) return Error.Validation("Accommodation.Location", "Selected location does not exist.");
        var amenityIds = request.Input.AmenityIds?.Distinct().ToList() ?? [];
        var selectedAmenities = await amenities.GetByIdsAsync(amenityIds, cancellationToken);
        if (selectedAmenities.Count != amenityIds.Count) return Error.Validation("Accommodation.Amenities", "One or more amenities are invalid.");
        var media = BuildMedia(unit.Id, request.Input); if (media.IsError) return media.Errors;
        unit.AdminUpdate(request.Input.Title, request.Input.Description, request.Input.Type, request.Input.LocationId,
            request.Input.AddressDetails, media.Value.Coordinates, request.Input.NumberOfRooms, request.Input.BedRooms,
            request.Input.BathRooms, request.Input.NumberOfBeds, request.Input.MaxGuests, request.Input.CheckInTime,
            request.Input.CheckOutTime, request.Input.PricePerNight, media.Value.MainImage, unit.Status);
        foreach (var amenity in unit.Amenities.ToList()) unit.RemoveAmenity(amenity.Id);
        unit.AddAmenities(selectedAmenities); unit.ClearImages(); unit.AddImages(media.Value.Images.Select(x => x.Id));
        units.ReplaceImages(unit.Id, media.Value.Images);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return await ToResult(unit, units, cancellationToken);
    }
}

public sealed class SetCompanyAccommodationVisibilityCommandHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyAccommodationRepository units, IUnitOfWork unitOfWork)
    : CompanyAccommodationHandlerBase(companies, context), ICommandHandler<SetCompanyAccommodationVisibilityCommand, Result<CompanyAccommodationResult>>
{
    public async Task<Result<CompanyAccommodationResult>> Handle(SetCompanyAccommodationVisibilityCommand request, CancellationToken cancellationToken)
    {
        var company = await GetCompanyContext(cancellationToken); if (company.IsError) return company.Errors;
        var unit = await units.GetByIdAsync(company.Value.CompanyId, request.AccommodationId, true, cancellationToken);
        if (unit is null) return Error.NotFound("Accommodation.NotFound", "Accommodation was not found.");
        unit.SetPublicVisibility(request.IsVisible); await unitOfWork.CommitChangesAsync(cancellationToken);
        return await ToResult(unit, units, cancellationToken);
    }
}

public sealed class DeleteCompanyAccommodationCommandHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyAccommodationRepository units, IUnitOfWork unitOfWork)
    : CompanyAccommodationHandlerBase(companies, context), ICommandHandler<DeleteCompanyAccommodationCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(DeleteCompanyAccommodationCommand request, CancellationToken cancellationToken)
    {
        var company = await GetCompanyContext(cancellationToken); if (company.IsError) return company.Errors;
        var unit = await units.GetByIdAsync(company.Value.CompanyId, request.AccommodationId, true, cancellationToken);
        if (unit is null) return Error.NotFound("Accommodation.NotFound", "Accommodation was not found.");
        units.Remove(unit); await unitOfWork.CommitChangesAsync(cancellationToken); return Result.Success;
    }
}
