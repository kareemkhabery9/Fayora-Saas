namespace Fayora.Contracts.CompanyModule;

public sealed record CompanyPackageOccurrenceRequest(DateOnly Date, int AvailableSeats);
public sealed record CreateCompanyPackageOccurrencesRequest(List<CompanyPackageOccurrenceRequest> Occurrences);
