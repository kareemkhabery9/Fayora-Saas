namespace Fayora.Application.Features.CompanyModule.Commands.RegisterCompanyWithEmail;

public record RegisterCompanyWithEmailResult(Guid CompanyId, Guid UserId, string Email);
