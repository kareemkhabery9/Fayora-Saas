using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Infrastructure.Persistence.Repositories.CompanyModule;

public sealed class CompanyRepository(ApplicationDbContext context) : ICompanyRepository
{
    public void Add(Company company) => context.Companies.Add(company);

    public Task<bool> ExistsForOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
        context.Companies.AnyAsync(x => x.OwnerUserId == ownerUserId, cancellationToken);

    public Task<Company?> GetByOwnerIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
        context.Companies.Include(x => x.WhatsAppNumbers).Include(x => x.PaymentMethods)
            .FirstOrDefaultAsync(x => x.OwnerUserId == ownerUserId, cancellationToken);

    public Task<CompanyWhatsAppNumber?> GetWhatsAppNumberAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default) =>
        context.CompanyWhatsAppNumbers.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, cancellationToken);

    public Task<int> GetWhatsAppCountAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        context.CompanyWhatsAppNumbers.CountAsync(x => x.CompanyId == companyId, cancellationToken);

    public Task<bool> IsSlugInUseAsync(string slug, Guid exceptCompanyId, CancellationToken cancellationToken = default) =>
            context.Companies.AnyAsync(x => x.Id != exceptCompanyId && x.Slug == slug, cancellationToken);

    public async Task<(List<Company> Items, int TotalCount)> GetAdminPageAsync(CompanyStatus? status, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Companies.AsNoTracking().Include(x => x.WhatsAppNumbers).Include(x => x.PaymentMethods).AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(x => x.Name.Contains(term) || x.ContactEmail.Value.Contains(term) || (x.Slug != null && x.Slug.Contains(term))); }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Company?> GetByIdForAdminAsync(Guid companyId, CancellationToken cancellationToken) =>
        context.Companies.Include(x => x.WhatsAppNumbers).Include(x => x.PaymentMethods).FirstOrDefaultAsync(x => x.Id == companyId, cancellationToken);

    public void AddWhatsAppNumber(CompanyWhatsAppNumber number) => context.CompanyWhatsAppNumbers.Add(number);
    public void RemoveWhatsAppNumber(CompanyWhatsAppNumber number) => context.CompanyWhatsAppNumbers.Remove(number);
    public void AddPaymentMethod(CompanyPaymentMethod method) => context.CompanyPaymentMethods.Add(method);
    public void RemovePaymentMethod(CompanyPaymentMethod method) => context.CompanyPaymentMethods.Remove(method);
}
