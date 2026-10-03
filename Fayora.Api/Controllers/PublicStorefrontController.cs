using Fayora.Application.Features.PublicStorefront;
using MediatR;
using Fayora.Domain.Enums.CompanyModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fayora.Api.Controllers;

[Route("api/public/companies/{slug}")]
[AllowAnonymous]
public sealed class PublicStorefrontController(ISender sender) : ApiController
{
    [HttpGet("storefront")]
    public async Task<IActionResult> GetStorefront(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicStorefrontOverviewQuery(slug), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet]
    public async Task<IActionResult> GetCompany(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicCompanyQuery(slug), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("packages")]
    public async Task<IActionResult> GetPackages(string slug, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPublicPackagesQuery(slug, search, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("packages/{packageId:guid}")]
    public async Task<IActionResult> GetPackage(string slug, Guid packageId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicPackageDetailsQuery(slug, packageId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("packages/{packageId:guid}/checkout-quote")]
    public async Task<IActionResult> GetCheckoutQuote(string slug, Guid packageId, [FromQuery] int adultsCount,
        [FromQuery] int childrenCount, [FromQuery] string paymentMethodType, [FromQuery] bool payDepositOnly, CancellationToken ct)
    {
        if (!Enum.TryParse<ManualPaymentMethodType>(paymentMethodType, true, out var method)) return BadRequest("Invalid payment method type.");
        var result = await sender.Send(new GetPublicPackageCheckoutQuoteQuery(slug, packageId, adultsCount, childrenCount, method, payDepositOnly), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("accommodations")]
    public async Task<IActionResult> GetAccommodations(string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPublicAccommodationsQuery(slug, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("accommodations/{accommodationId:guid}")]
    public async Task<IActionResult> GetAccommodation(string slug, Guid accommodationId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicAccommodationQuery(slug, accommodationId), ct);
        return result.Match(Ok, Problem);
    }
}
