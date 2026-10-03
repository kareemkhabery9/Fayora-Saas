using Fayora.Domain.Common.Entity;
using Fayora.Domain.Common.Results;
using Fayora.Domain.ValueObjects;

namespace Fayora.Domain.Entities.CompanyModule;

public class CompanyWhatsAppNumber : BaseEntity<Guid>
{
    public Guid CompanyId { get; private set; }
    public PhoneNumber PhoneNumber { get; private set; } = null!;

    private CompanyWhatsAppNumber() { }

    private CompanyWhatsAppNumber(Guid companyId, PhoneNumber phoneNumber)
    {
        Id = Guid.CreateVersion7();
        CompanyId = companyId;
        PhoneNumber = phoneNumber;
    }

    public static Result<CompanyWhatsAppNumber> Create(Guid companyId, string phoneNumber)
    {
        if (companyId == Guid.Empty)
            return Error.Validation("CompanyWhatsApp.CompanyRequired", "A company is required.");

        var phoneResult = PhoneNumber.Create(phoneNumber);
        if (phoneResult.IsError)
            return phoneResult.Errors;

        return new CompanyWhatsAppNumber(companyId, phoneResult.Value);
    }

    public Result<Success> Update(string phoneNumber)
    {
        var phoneResult = PhoneNumber.Create(phoneNumber);
        if (phoneResult.IsError) return phoneResult.Errors;

        PhoneNumber = phoneResult.Value;
        return Result.Success;
    }
}
