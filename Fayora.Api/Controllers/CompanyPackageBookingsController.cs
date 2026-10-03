using Fayora.Application.Features.CompanyModule.CompanyPackageBookings;
using Fayora.Contracts.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fayora.Api.Controllers;

[Authorize(Roles = "Company")]
[Route("api/company/package-bookings")]
public sealed class CompanyPackageBookingsController(ISender sender) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] bool? isDepositPayment, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        ManualBookingStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ManualBookingStatus>(status, true, out var statusValue)) return BadRequest("Invalid booking status.");
            parsedStatus = statusValue;
        }
        var result = await sender.Send(new GetCompanyPackageBookingsQuery(parsedStatus, isDepositPayment, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{bookingId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid bookingId, [FromBody] ReviewCompanyPackageBookingRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ConfirmCompanyPackageBookingCommand(bookingId, request.Note), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{bookingId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid bookingId, [FromBody] ReviewCompanyPackageBookingRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RejectCompanyPackageBookingCommand(bookingId, request.Note), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("scan-qr")]
    public async Task<IActionResult> ScanQr([FromBody] ScanCompanyPackageBookingQrRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ScanCompanyPackageBookingQrCommand(request.Token), ct);
        return result.Match(Ok, Problem);
    }
}
