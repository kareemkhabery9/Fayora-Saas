using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Domain.Common.Results;

namespace Fayora.Application.Features.CompanyModule.Commands.UpdateMyCompanyProfile;

public record UpdateMyCompanyProfileCommand(
    string Name, string Description, string? FacebookUrl, string? InstagramUrl,
    string? LogoUrl, string? CoverImageUrl, List<string> WhatsAppNumbers) : ICommand<Result<CompanyProfileResult>>;
