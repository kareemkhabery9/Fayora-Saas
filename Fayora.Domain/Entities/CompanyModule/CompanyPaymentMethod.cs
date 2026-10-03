using Fayora.Domain.Common.Entity;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.ValueObjects;

namespace Fayora.Domain.Entities.CompanyModule;

public class CompanyPaymentMethod : BaseEntity<Guid>
{
    public Guid CompanyId { get; private set; }
    public ManualPaymentMethodType Type { get; private set; }
    public string AccountIdentifier { get; private set; } = null!;
    public FileUrl? InstaPayQrCodeUrl { get; private set; }
    public bool IsEnabled { get; private set; }

    private CompanyPaymentMethod() { }

    private CompanyPaymentMethod(Guid companyId, ManualPaymentMethodType type, string accountIdentifier, FileUrl? instaPayQrCodeUrl, bool isEnabled)
    {
        Id = Guid.CreateVersion7();
        CompanyId = companyId;
        Type = type;
        AccountIdentifier = accountIdentifier;
        InstaPayQrCodeUrl = instaPayQrCodeUrl;
        IsEnabled = isEnabled;
    }

    public static Result<CompanyPaymentMethod> Create(
        Guid companyId,
        ManualPaymentMethodType type,
        string accountIdentifier,
        string? instaPayQrCodeUrl,
        bool isEnabled)
    {
        if (companyId == Guid.Empty)
            return Error.Validation("CompanyPayment.CompanyRequired", "A company is required.");

        if (!Enum.IsDefined(type))
            return Error.Validation("CompanyPayment.InvalidType", "Payment method type is invalid.");

        if (isEnabled && string.IsNullOrWhiteSpace(accountIdentifier))
            return Error.Validation("CompanyPayment.IdentifierRequired", "An account identifier is required for enabled payment methods.");

        if (accountIdentifier?.Trim().Length > 100)
            return Error.Validation("CompanyPayment.IdentifierTooLong", "Account identifier cannot exceed 100 characters.");

        FileUrl? qrCode = null;
        if (!string.IsNullOrWhiteSpace(instaPayQrCodeUrl))
        {
            var qrCodeResult = FileUrl.Create(instaPayQrCodeUrl);
            if (qrCodeResult.IsError) return qrCodeResult.Errors;
            qrCode = qrCodeResult.Value;
        }
        if (isEnabled && type == ManualPaymentMethodType.InstaPay && qrCode is null)
            return Error.Validation("CompanyPayment.InstaPayQrRequired", "An InstaPay QR code is required for an enabled InstaPay account.");

        return new CompanyPaymentMethod(companyId, type, accountIdentifier?.Trim() ?? string.Empty, qrCode, isEnabled);
    }
}
