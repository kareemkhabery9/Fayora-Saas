using Fayora.Application.Features.CompanyModule.CompanyAccommodationBookings;
using Fayora.Contracts.CompanyModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fayora.Api.Controllers;

[Authorize(Roles = "Company")]
[Route("api/company/accommodation-bookings")]
public sealed class CompanyAccommodationBookingsController(ISender sender) : ApiController
{
    [HttpPatch("{bookingId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid bookingId, [FromBody] ReviewCompanyPackageBookingRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ConfirmCompanyAccommodationBookingCommand(bookingId, request.Note), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{bookingId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid bookingId, [FromBody] ReviewCompanyPackageBookingRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RejectCompanyAccommodationBookingCommand(bookingId, request.Note), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("scan-qr")]
    public async Task<IActionResult> ScanQr([FromBody] ScanCompanyPackageBookingQrRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ScanCompanyAccommodationBookingQrCommand(request.Token), ct);
        return result.Match(Ok, Problem);
    }
}
