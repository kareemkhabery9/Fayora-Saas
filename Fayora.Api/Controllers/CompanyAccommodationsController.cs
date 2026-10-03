using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Features.CompanyModule.CompanyAccommodations;
using Fayora.Contracts.CompanyModule;
using Fayora.Domain.Enums.AccommodationModule;
using Fayora.Domain.Enums.PackageModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fayora.Api.Controllers;

[Authorize(Roles = "Company")]
[Route("api/company/accommodations")]
public sealed class CompanyAccommodationsController(ISender sender) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompanyAccommodationRequest request, CancellationToken ct)
    {
        var input = Parse(request, out var error); if (input is null) return BadRequest(error);
        var result = await sender.Send(new CreateCompanyAccommodationCommand(input), ct);
        return result.Match(value => CreatedAtAction(nameof(GetById), new { accommodationId = value.Id }, value), Problem);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        CatalogItemStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<CatalogItemStatus>(status, true, out var value)) return BadRequest("Invalid status.");
            parsedStatus = value;
        }
        var result = await sender.Send(new GetMyCompanyAccommodationsQuery(parsedStatus, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{accommodationId:guid}")]
    public async Task<IActionResult> GetById(Guid accommodationId, CancellationToken ct)
    {
        var result = await sender.Send(new GetMyCompanyAccommodationQuery(accommodationId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("{accommodationId:guid}")]
    public async Task<IActionResult> Update(Guid accommodationId, [FromBody] CompanyAccommodationRequest request, CancellationToken ct)
    {
        var input = Parse(request, out var error); if (input is null) return BadRequest(error);
        var result = await sender.Send(new UpdateCompanyAccommodationCommand(accommodationId, input), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{accommodationId:guid}/visibility")]
    public async Task<IActionResult> SetVisibility(Guid accommodationId, [FromBody] SetCompanyAccommodationVisibilityRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SetCompanyAccommodationVisibilityCommand(accommodationId, request.IsVisible), ct);
        return result.Match(Ok, Problem);
    }

    [HttpDelete("{accommodationId:guid}")]
    public async Task<IActionResult> Delete(Guid accommodationId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCompanyAccommodationCommand(accommodationId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static CompanyAccommodationInput? Parse(CompanyAccommodationRequest request, out string error)
    {
        error = string.Empty;
        if (!Enum.TryParse<HousingType>(request.Type, true, out var type)) { error = "Invalid accommodation type."; return null; }
        return new CompanyAccommodationInput(request.Title, request.Description, type, request.LocationId,
            request.AddressDetails, request.Latitude, request.Longitude, request.PricePerNight, request.NumberOfRooms,
            request.BedRooms, request.BathRooms, request.NumberOfBeds, request.MaxGuests, request.CheckInTime,
            request.CheckOutTime, request.MainImageUrl, request.GalleryImageUrls, request.AmenityIds);
    }
}
