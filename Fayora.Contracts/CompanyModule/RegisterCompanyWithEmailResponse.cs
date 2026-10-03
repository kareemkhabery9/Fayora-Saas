namespace Fayora.Contracts.CompanyModule;

public record RegisterCompanyWithEmailResponse(Guid CompanyId, Guid UserId, string Email);
