using Fayora.Application.Features.PublicStorefront;
using Fayora.Contracts.CompanyModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fayora.Api.Controllers;

[Route("api/public/bookings")]
[AllowAnonymous]
[EnableRateLimiting("public-write")]
public sealed class PublicBookingTrackingController(ISender sender) : ApiController
{
    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] PublicBookingTrackingRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new TrackPublicBookingQuery(request.BookingReference), ct);
        return result.Match(Ok, Problem);
    }
}
