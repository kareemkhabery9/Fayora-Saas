using Fayora.Application.Features.CompanyModule.CompanyCustomerReviews;
using Fayora.Contracts.CompanyModule;
using Fayora.Domain.Enums.ReviewModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fayora.Api.Controllers;

[Route("api/public/companies/{slug}/reviews")]
[AllowAnonymous]
[EnableRateLimiting("public-write")]
public sealed class PublicCompanyReviewsController(ISender sender) : ApiController
{
    [HttpPost("{targetType}/{targetId:guid}")]
    public async Task<IActionResult> Create(string slug, string targetType, Guid targetId, [FromBody] CreatePublicCompanyReviewRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<ReviewTargetType>(targetType, true, out var type) || type is not (ReviewTargetType.TravelPackage or ReviewTargetType.HousingUnit))
            return BadRequest("Target type must be TravelPackage or HousingUnit.");
        var input = new CreatePublicCompanyReviewInput(request.CustomerName, request.Rating, request.Comment);
        var result = await sender.Send(new CreatePublicCompanyReviewCommand(slug, targetId, type, input), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{targetType}/{targetId:guid}")]
    public async Task<IActionResult> GetApproved(string slug, string targetType, Guid targetId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ReviewTargetType>(targetType, true, out var type) || type is not (ReviewTargetType.TravelPackage or ReviewTargetType.HousingUnit))
            return BadRequest("Target type must be TravelPackage or HousingUnit.");
        var result = await sender.Send(new GetPublicApprovedCompanyReviewsQuery(slug, targetId, type, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }
}
