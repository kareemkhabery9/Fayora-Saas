using Fayora.Application.Features.CompanyModule.CompanyAccommodationBookings;
using Fayora.Contracts.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fayora.Api.Controllers;

[Route("api/public/companies/{slug}/accommodations/{accommodationId:guid}/bookings")]
[AllowAnonymous]
[EnableRateLimiting("public-write")]
public sealed class PublicAccommodationBookingsController(ISender sender) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Create(string slug, Guid accommodationId, [FromBody] CreatePublicAccommodationBookingRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<ManualPaymentMethodType>(request.PaymentMethodType, true, out var method)) return BadRequest("Invalid payment method type.");
        var input = new CreatePublicAccommodationBookingInput(request.CustomerName, request.CustomerPhoneNumber, request.CustomerEmail,
            request.CheckInDate, request.CheckOutDate, request.AdultsCount, request.ChildrenCount, method, request.SenderPaymentIdentifier,
            request.PaymentReceiptUrl);
        var result = await sender.Send(new CreatePublicAccommodationBookingCommand(slug, accommodationId, input), ct);
        return result.Match(value => Created(string.Empty, value), Problem);
    }
}
