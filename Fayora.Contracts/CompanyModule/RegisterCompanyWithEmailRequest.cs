namespace Fayora.Contracts.CompanyModule;

public record RegisterCompanyWithEmailRequest(
    string CompanyName,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    string PhoneNumber,
    string Password);
