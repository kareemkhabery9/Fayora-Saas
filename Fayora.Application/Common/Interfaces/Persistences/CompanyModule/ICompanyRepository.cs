using Fayora.Domain.Entities.CompanyModule;

namespace Fayora.Application.Common.Interfaces.Persistences.CompanyModule;

public interface ICompanyRepository
{
    void Add(Company company);
    Task<bool> ExistsForOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<Company?> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<CompanyWhatsAppNumber?> GetWhatsAppNumberAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<int> GetWhatsAppCountAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> IsSlugInUseAsync(string slug, Guid exceptCompanyId, CancellationToken cancellationToken = default);
    Task<(List<Company> Items, int TotalCount)> GetAdminPageAsync(Fayora.Domain.Enums.CompanyModule.CompanyStatus? status, string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<Company?> GetByIdForAdminAsync(Guid companyId, CancellationToken cancellationToken);
    void AddWhatsAppNumber(CompanyWhatsAppNumber number);
    void RemoveWhatsAppNumber(CompanyWhatsAppNumber number);
    void AddPaymentMethod(CompanyPaymentMethod method);
    void RemovePaymentMethod(CompanyPaymentMethod method);
}
