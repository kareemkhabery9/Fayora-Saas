namespace Fayora.Contracts.CompanyModule;

public record UpdateCompanyProfileRequest(
    string Name, string Description, string? FacebookUrl, string? InstagramUrl,
    string? LogoUrl, string? CoverImageUrl, List<string> WhatsAppNumbers);
