namespace Fayora.Contracts.CompanyModule;

public sealed record CreatePublicCompanyReviewRequest(string CustomerName, decimal Rating, string? Comment);
