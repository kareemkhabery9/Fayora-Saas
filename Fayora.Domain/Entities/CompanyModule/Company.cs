using Fayora.Domain.Common.Entity;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace Fayora.Domain.Entities.CompanyModule;

public class Company : AuditableEntity<Guid>
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.Ordinal)
    {
        "admin", "api", "dashboard", "www", "super-admin"
    };

    private readonly List<CompanyWhatsAppNumber> _whatsAppNumbers = [];
    private readonly List<CompanyPaymentMethod> _paymentMethods = [];

    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = null!;
    public Email ContactEmail { get; private set; } = null!;
    public PhoneNumber ContactPhoneNumber { get; private set; } = null!;
    public string? Slug { get; private set; }
    public FileUrl? LogoUrl { get; private set; }
    public FileUrl? CoverImageUrl { get; private set; }
    public string? Description { get; private set; }
    public string? FacebookUrl { get; private set; }
    public string? InstagramUrl { get; private set; }
    public CompanyTheme Theme { get; private set; } = CompanyTheme.ClassicNavy;
    public CompanyStatus Status { get; private set; } = CompanyStatus.Trial;
    public DateTimeOffset TrialStartedAt { get; private set; }
    public bool IsOnboardingCompleted { get; private set; }

    public IReadOnlyCollection<CompanyWhatsAppNumber> WhatsAppNumbers => _whatsAppNumbers.AsReadOnly();
    public IReadOnlyCollection<CompanyPaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    private Company() { }

    private Company(Guid ownerUserId, string name, Email contactEmail, PhoneNumber contactPhoneNumber)
    {
        Id = Guid.CreateVersion7();
        OwnerUserId = ownerUserId;
        Name = name;
        ContactEmail = contactEmail;
        ContactPhoneNumber = contactPhoneNumber;
        TrialStartedAt = DateTimeOffset.UtcNow;
    }

    public static Result<Company> Create(
        Guid ownerUserId,
        string name,
        string email,
        string phoneNumber)
    {
        if (ownerUserId == Guid.Empty)
            return Error.Validation("Company.OwnerRequired", "A company owner is required.");

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            return Error.Validation("Company.NameInvalid", "Company name is required and cannot exceed 200 characters.");

        var emailResult = Email.Create(email);
        if (emailResult.IsError)
            return emailResult.Errors;

        var phoneResult = PhoneNumber.Create(phoneNumber);
        if (phoneResult.IsError)
            return phoneResult.Errors;

        return new Company(ownerUserId, name.Trim(), emailResult.Value, phoneResult.Value);
    }

    public Result<Success> UpdateProfile(
        string name,
        string description,
        string? facebookUrl,
        string? instagramUrl,
        FileUrl? logoUrl,
        FileUrl? coverImageUrl)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            return Error.Validation("Company.NameInvalid", "Company name is required and cannot exceed 200 characters.");

        if (description?.Length > 2_000)
            return Error.Validation("Company.DescriptionTooLong", "Company description cannot exceed 2000 characters.");

        Name = name.Trim();
        Description = description?.Trim();
        FacebookUrl = NormalizeOptionalUrl(facebookUrl);
        InstagramUrl = NormalizeOptionalUrl(instagramUrl);
        LogoUrl = logoUrl;
        CoverImageUrl = coverImageUrl;
        Updated();
        return Result.Success;
    }

    public Result<Success> SetSlug(string slug)
    {
        var normalizedSlug = slug?.Trim().ToLowerInvariant() ?? string.Empty;

        if (normalizedSlug.Length is < 3 or > 30 || !SlugPattern.IsMatch(normalizedSlug) || ReservedSlugs.Contains(normalizedSlug))
            return Error.Validation("Company.InvalidSlug", "Slug must contain 3 to 30 lowercase letters, numbers, or hyphens.");

        Slug = normalizedSlug;
        Updated();
        return Result.Success;
    }

    public void SetTheme(CompanyTheme theme)
    {
        Theme = theme;
        Updated();
    }

    public Result<Success> ReplaceWhatsAppNumbers(IEnumerable<CompanyWhatsAppNumber> numbers)
    {
        var replacement = numbers?.ToList() ?? [];
        if (replacement.Count == 0)
            return Error.Validation("Company.WhatsAppRequired", "At least one WhatsApp number is required.");

        if (replacement.Select(x => x.PhoneNumber.Value).Distinct(StringComparer.Ordinal).Count() != replacement.Count)
            return Error.Validation("Company.DuplicateWhatsApp", "WhatsApp numbers must be unique.");

        if (replacement.Any(x => x.CompanyId != Id))
            return Error.Validation("Company.WhatsAppOwnershipInvalid", "All WhatsApp numbers must belong to this company.");

        _whatsAppNumbers.Clear();
        _whatsAppNumbers.AddRange(replacement);
        Updated();
        return Result.Success;
    }

    public Result<Success> ReplacePaymentMethods(IEnumerable<CompanyPaymentMethod> methods)
    {
        var replacement = methods?.ToList() ?? [];
        if (!replacement.Any(x => x.IsEnabled))
            return Error.Validation("Company.PaymentMethodRequired", "At least one payment method must be enabled.");

        if (replacement.Select(x => x.Type).Distinct().Count() != replacement.Count)
            return Error.Validation("Company.DuplicatePaymentMethod", "Each payment method can be configured only once.");

        if (replacement.Any(x => x.CompanyId != Id))
            return Error.Validation("Company.PaymentMethodOwnershipInvalid", "All payment methods must belong to this company.");

        _paymentMethods.Clear();
        _paymentMethods.AddRange(replacement);
        Updated();
        return Result.Success;
    }

    public Result<Success> CompleteOnboarding()
    {
        if (string.IsNullOrWhiteSpace(Slug))
            return Error.Validation("Company.SlugRequired", "A company slug is required before completing onboarding.");

        if (_whatsAppNumbers.Count == 0)
            return Error.Validation("Company.WhatsAppRequired", "At least one WhatsApp number is required before completing onboarding.");

        if (!_paymentMethods.Any(x => x.IsEnabled))
            return Error.Validation("Company.PaymentMethodRequired", "At least one payment method must be enabled before completing onboarding.");

        IsOnboardingCompleted = true;
        Updated();
        return Result.Success;
    }

    public void Activate()
    {
        Status = CompanyStatus.Active;
        Updated();
    }

    public void Suspend()
    {
        Status = CompanyStatus.Suspended;
        Updated();
    }

    private static string? NormalizeOptionalUrl(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
