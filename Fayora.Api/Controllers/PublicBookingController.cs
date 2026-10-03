using Fayora.Application.Features.CompanyModule.CompanyPackageBookings;
using Fayora.Contracts.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fayora.Api.Controllers;

[Route("api/public/companies/{slug}/packages/{packageId:guid}/occurrences/{occurrenceId:guid}/bookings")]
[AllowAnonymous]
[EnableRateLimiting("public-write")]
public sealed class PublicBookingController(ISender sender) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> Create(string slug, Guid packageId, Guid occurrenceId,
        [FromBody] CreatePublicPackageBookingRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ManualPaymentMethodType>(request.PaymentMethodType, true, out var paymentMethodType))
            return BadRequest("Invalid payment method type.");
        var input = new CreatePublicPackageBookingInput(request.CustomerName, request.CustomerPhoneNumber,
            request.CustomerEmail, request.AdultsCount, request.ChildrenCount, paymentMethodType,
            request.SenderPaymentIdentifier, request.PayDepositOnly, request.PaymentReceiptUrl);
        var result = await sender.Send(new CreatePublicPackageBookingCommand(slug, packageId, occurrenceId, input), ct);
        return result.Match(value => Created(string.Empty, value), Problem);
    }
}
