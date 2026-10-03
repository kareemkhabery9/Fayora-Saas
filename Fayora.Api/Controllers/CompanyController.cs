using Fayora.Application.Features.CompanyModule;
using Fayora.Application.Features.CompanyModule.Commands.UpdateMyCompanyProfile;
using Fayora.Application.Features.CompanyModule.Queries.GetMyCompanyProfile;
using Fayora.Application.Features.CompanyModule.Queries.CheckCompanySlugAvailability;
using Fayora.Application.Features.CompanyModule.Commands.ConfigureCompanyOnboarding;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Contracts.CompanyModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fayora.Api.Controllers;

[Authorize(Roles = "Company")]
[Route("api/company")]
public sealed class CompanyController(ISender sender) : ApiController
{
    [HttpGet("slug/availability")]
    public async Task<IActionResult> CheckSlugAvailability([FromQuery] string slug, CancellationToken ct)
    {
        var result = await sender.Send(new CheckCompanySlugAvailabilityQuery(slug), ct);
        return result.Match(value => Ok(new { available = value.Available, slug = value.Slug }), Problem);
    }

    [HttpPut("slug")]
    public async Task<IActionResult> UpdateSlug([FromBody] UpdateCompanySlugRequest request, CancellationToken ct)
    { var result = await sender.Send(new UpdateCompanySlugCommand(request.Slug), ct); return result.Match(value => Ok(ToResponse(value)), Problem); }

    [HttpPut("theme")]
    public async Task<IActionResult> UpdateTheme([FromBody] UpdateCompanyThemeRequest request, CancellationToken ct)
    { if (!Enum.TryParse<CompanyTheme>(request.Theme, true, out var theme)) return BadRequest("Invalid theme."); var result = await sender.Send(new UpdateCompanyThemeCommand(theme), ct); return result.Match(value => Ok(ToResponse(value)), Problem); }

    [HttpPut("payment-methods")]
    public async Task<IActionResult> UpdatePaymentMethods([FromBody] UpdateCompanyPaymentMethodsRequest request, CancellationToken ct)
    {
        var methods = new List<PaymentMethodInput>(); foreach (var item in request.PaymentMethods ?? []) { if (!Enum.TryParse<ManualPaymentMethodType>(item.Type, true, out var type)) return BadRequest("Invalid payment method type."); methods.Add(new PaymentMethodInput(type, item.AccountIdentifier, item.InstaPayQrCodeUrl, item.IsEnabled)); }
        var result = await sender.Send(new UpdateCompanyPaymentMethodsCommand(methods), ct); return result.Match(value => Ok(ToResponse(value)), Problem);
    }

    [HttpPost("onboarding/complete")]
    public async Task<IActionResult> CompleteOnboarding(CancellationToken ct)
    { var result = await sender.Send(new CompleteCompanyOnboardingCommand(), ct); return result.Match(value => Ok(ToResponse(value)), Problem); }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyCompanyProfileQuery(), cancellationToken);
        return result.Match(value => Ok(ToResponse(value)), Problem);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateCompanyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateMyCompanyProfileCommand(
            request.Name, request.Description, request.FacebookUrl, request.InstagramUrl,
            request.LogoUrl, request.CoverImageUrl, request.WhatsAppNumbers), cancellationToken);

        return result.Match(value => Ok(ToResponse(value)), Problem);
    }

    private static CompanyProfileResponse ToResponse(CompanyProfileResult value) => new(
        value.Id,
        value.Name,
        value.Email,
        value.PhoneNumber,
        value.Slug,
        value.LogoUrl,
        value.CoverImageUrl,
        value.Description,
        value.FacebookUrl,
        value.InstagramUrl,
        value.WhatsAppNumbers,
        value.Theme.ToString(),
        value.Status.ToString(),
        value.IsOnboardingCompleted);
}
