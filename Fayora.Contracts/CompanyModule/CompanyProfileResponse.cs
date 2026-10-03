namespace Fayora.Contracts.CompanyModule;

public record CompanyProfileResponse(
    Guid Id, string Name, string Email, string PhoneNumber, string? Slug, string? LogoUrl,
    string? CoverImageUrl, string? Description, string? FacebookUrl, string? InstagramUrl,
    List<string> WhatsAppNumbers,
    string Theme, string Status, bool IsOnboardingCompleted);
