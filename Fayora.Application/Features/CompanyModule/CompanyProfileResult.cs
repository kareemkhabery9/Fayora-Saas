using Fayora.Domain.Enums.CompanyModule;

namespace Fayora.Application.Features.CompanyModule;

public record CompanyProfileResult(
    Guid Id,
    string Name,
    string Email,
    string PhoneNumber,
    string? Slug,
    string? LogoUrl,
    string? CoverImageUrl,
    string? Description,
    string? FacebookUrl,
    string? InstagramUrl,
    List<string> WhatsAppNumbers,
    CompanyTheme Theme,
    CompanyStatus Status,
    bool IsOnboardingCompleted);
