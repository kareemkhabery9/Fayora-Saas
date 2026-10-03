using Fayora.Application.Features.CompanyModule.CompanyCustomerReviews;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.ReviewModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fayora.Api.Controllers;

[Authorize(Roles = "Company")]
[Route("api/company/reviews")]
public sealed class CompanyCustomerReviewsController(ISender sender) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? targetType, [FromQuery] string? targetName,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        CompanyCustomerReviewStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<CompanyCustomerReviewStatus>(status, true, out var value)) return BadRequest("Invalid review status.");
            parsedStatus = value;
        }
        ReviewTargetType? parsedTargetType = null;
        if (!string.IsNullOrWhiteSpace(targetType))
        {
            if (!Enum.TryParse<ReviewTargetType>(targetType, true, out var value) || value is not (ReviewTargetType.TravelPackage or ReviewTargetType.HousingUnit))
                return BadRequest("Target type must be TravelPackage or HousingUnit.");
            parsedTargetType = value;
        }
        var result = await sender.Send(new GetCompanyCustomerReviewsQuery(parsedStatus, parsedTargetType, targetName, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{reviewId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid reviewId, CancellationToken ct)
    {
        var result = await sender.Send(new ApproveCompanyCustomerReviewCommand(reviewId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{reviewId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid reviewId, CancellationToken ct)
    {
        var result = await sender.Send(new RejectCompanyCustomerReviewCommand(reviewId), ct);
        return result.Match(Ok, Problem);
    }
}
