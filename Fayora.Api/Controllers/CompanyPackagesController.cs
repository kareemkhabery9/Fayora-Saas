using Fayora.Application.Features.CompanyModule.CompanyPackages;
using Fayora.Contracts.CompanyModule;
using Fayora.Domain.Enums.SharedModule;
using Fayora.Domain.Enums.PackageModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fayora.Api.Controllers;

[Authorize(Roles = "Company")]
[Route("api/company/packages")]
public sealed class CompanyPackagesController(ISender sender) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompanyPackageRequest request, CancellationToken ct)
    {
        var input = Parse(request, out var error);
        if (input is null) return BadRequest(error);
        var result = await sender.Send(new CreateCompanyPackageCommand(input), ct);
        return result.Match(value => CreatedAtAction(nameof(GetById), new { packageId = value.Id }, value), Problem);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        CatalogItemStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<CatalogItemStatus>(status, true, out var statusValue)) return BadRequest("Invalid status.");
            parsedStatus = statusValue;
        }
        var result = await sender.Send(new GetMyCompanyPackagesQuery(parsedStatus, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{packageId:guid}")]
    public async Task<IActionResult> GetById(Guid packageId, CancellationToken ct)
    {
        var result = await sender.Send(new GetMyCompanyPackageQuery(packageId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("{packageId:guid}")]
    public async Task<IActionResult> Update(Guid packageId, [FromBody] CompanyPackageRequest request, CancellationToken ct)
    {
        var input = Parse(request, out var error);
        if (input is null) return BadRequest(error);
        var result = await sender.Send(new UpdateCompanyPackageCommand(packageId, input), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{packageId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid packageId, CancellationToken ct)
    {
        var result = await sender.Send(new PublishCompanyPackageCommand(packageId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{packageId:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid packageId, CancellationToken ct)
    {
        var result = await sender.Send(new UnpublishCompanyPackageCommand(packageId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{packageId:guid}/visibility")]
    public async Task<IActionResult> SetVisibility(
        Guid packageId, [FromBody] SetCompanyPackageVisibilityRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SetCompanyPackageVisibilityCommand(packageId, request.IsVisible), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("{packageId:guid}/accommodations")]
    public async Task<IActionResult> UpdateAccommodations(
        Guid packageId, [FromBody] UpdateCompanyPackageAccommodationsRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCompanyPackageAccommodationsCommand(
            packageId, request.AccommodationIds ?? []), ct);
        return result.Match(Ok, Problem);
    }

    [HttpDelete("{packageId:guid}")]
    public async Task<IActionResult> Delete(Guid packageId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCompanyPackageCommand(packageId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("{packageId:guid}/occurrences")]
    public async Task<IActionResult> GetOccurrences(Guid packageId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCompanyPackageOccurrencesQuery(packageId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("{packageId:guid}/occurrences")]
    public async Task<IActionResult> CreateOccurrences(
        Guid packageId, [FromBody] CreateCompanyPackageOccurrencesRequest request, CancellationToken ct)
    {
        var occurrences = (request.Occurrences ?? [])
            .Select(x => new OccurrenceInput(x.Date, x.AvailableSeats)).ToList();
        var result = await sender.Send(new CreateCompanyPackageOccurrencesCommand(packageId, occurrences), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("{packageId:guid}/occurrences/{occurrenceId:guid}")]
    public async Task<IActionResult> UpdateOccurrence(
        Guid packageId, Guid occurrenceId, [FromBody] CompanyPackageOccurrenceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCompanyPackageOccurrenceCommand(
            packageId, occurrenceId, new OccurrenceInput(request.Date, request.AvailableSeats)), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{packageId:guid}/occurrences/{occurrenceId:guid}/cancel")]
    public async Task<IActionResult> CancelOccurrence(Guid packageId, Guid occurrenceId, CancellationToken ct)
    {
        var result = await sender.Send(new CancelCompanyPackageOccurrenceCommand(packageId, occurrenceId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpDelete("{packageId:guid}/occurrences/{occurrenceId:guid}")]
    public async Task<IActionResult> DeleteOccurrence(Guid packageId, Guid occurrenceId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCompanyPackageOccurrenceCommand(packageId, occurrenceId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static CompanyPackageInput? Parse(CompanyPackageRequest request, out string error)
    {
        error = string.Empty;
        if (!Enum.TryParse<TourType>(request.TourTypes, true, out var tourTypes) || tourTypes == TourType.None)
        { error = "Invalid tourTypes."; return null; }
        if (!Enum.TryParse<TransportType>(request.TransportType, true, out var transportType))
        { error = "Invalid transportType."; return null; }
        if (!Enum.TryParse<CancellationPolicy>(request.CancellationPolicy, true, out var cancellationPolicy))
        { error = "Invalid cancellationPolicy."; return null; }
        return new CompanyPackageInput(request.Title, request.Description, tourTypes, request.DurationHours,
            request.NumOfDays, transportType, request.ArrivalNote, request.AdultPrice, request.ChildPrice,
            request.MaxCapacity, request.IncludedItemIds, request.ExcludedItemIds, request.MainImageUrl,
            request.VideoUrl, request.GalleryImageUrls, request.GuestRequirements, cancellationPolicy, request.DepositPercentage,
            request.RefundPolicyDetails);
    }
}
