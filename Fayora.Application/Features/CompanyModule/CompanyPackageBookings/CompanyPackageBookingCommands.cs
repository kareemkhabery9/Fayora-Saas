using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Common.Interfaces.Services.CompanyModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.ValueObjects;

namespace Fayora.Application.Features.CompanyModule.CompanyPackageBookings;

public sealed record CreatePublicPackageBookingCommand(string Slug, Guid PackageId, Guid OccurrenceId, CreatePublicPackageBookingInput Input)
    : ICommand<Result<CompanyPackageBookingResult>>;
public sealed record ConfirmCompanyPackageBookingCommand(Guid BookingId, string? Note) : ICommand<Result<CompanyPackageBookingResult>>;
public sealed record RejectCompanyPackageBookingCommand(Guid BookingId, string? Note) : ICommand<Result<CompanyPackageBookingResult>>;

public sealed class CreatePublicPackageBookingCommandHandler(
    IPublicStorefrontRepository storefront, ICompanyPackageBookingRepository bookings, IEmailService emailService, IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePublicPackageBookingCommand, Result<CompanyPackageBookingResult>>
{
    public async Task<Result<CompanyPackageBookingResult>> Handle(CreatePublicPackageBookingCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug)) return Error.Validation("Booking.InvalidSlug", "Company slug is required.");
        var company = await storefront.GetCompanyBySlugAsync(request.Slug.Trim().ToLowerInvariant(), cancellationToken);
        if (company is null) return Error.NotFound("Booking.CompanyNotFound", "Company storefront was not found.");
        var package = await storefront.GetPackageAsync(company.Id, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Booking.PackageNotFound", "Package was not found or is not available.");
        var occurrence = await bookings.GetAvailableOccurrenceAsync(company.Id, package.Id, request.OccurrenceId, cancellationToken);
        if (occurrence is null) return Error.NotFound("Booking.TripDateNotFound", "Trip date was not found or is no longer available.");
        var paymentMethod = company.PaymentMethods.FirstOrDefault(x => x.Type == request.Input.PaymentMethodType && x.IsEnabled);
        if (paymentMethod is null) return Error.Validation("Booking.PaymentMethod", "Selected payment method is not available for this company.");
        var receipt = FileUrl.Create(request.Input.PaymentReceiptUrl); if (receipt.IsError) return receipt.Errors;
        var total = package.CalculateGuestTotal(request.Input.AdultsCount, request.Input.ChildrenCount);
        if (total.IsError) return total.Errors;
        var paidAmount = request.Input.PayDepositOnly
            ? decimal.Round(total.Value * package.DepositPercentage / 100m, 2, MidpointRounding.AwayFromZero)
            : total.Value;
        if (request.Input.PayDepositOnly && package.DepositPercentage <= 0)
            return Error.Validation("Booking.DepositUnavailable", "This package does not offer deposit payment.");
        var booking = CompanyPackageBooking.Create(company.Id, package.Id, occurrence.Id, request.Input.CustomerName,
            request.Input.CustomerPhoneNumber, request.Input.CustomerEmail, request.Input.AdultsCount,
            request.Input.ChildrenCount, total.Value, paidAmount, request.Input.PayDepositOnly, paymentMethod.Type,
            paymentMethod.AccountIdentifier, request.Input.SenderPaymentIdentifier, receipt.Value);
        if (booking.IsError) return booking.Errors;
        var seatReservation = occurrence.ReserveSeats(booking.Value.GuestsCount);
        if (seatReservation.IsError) return seatReservation.Errors;
        bookings.Add(booking.Value);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        try
        {
            await emailService.SendEmailAsync(company.ContactEmail.Value,
                $"حجز جديد يحتاج مراجعة - {booking.Value.BookingReference}",
                BuildCompanyNewBookingEmail(company.Name, package.Title, occurrence.Date, booking.Value));
        }
        catch
        {
            // A temporary email issue must not lose a stored booking or its seat reservation.
        }
        return ToResult(booking.Value);
    }

    internal static CompanyPackageBookingResult ToResult(CompanyPackageBooking booking) => new(booking.Id, booking.BookingReference,
        booking.PackageId, booking.OccurrenceId, booking.CustomerName, booking.CustomerPhoneNumber, booking.CustomerEmail,
        booking.AdultsCount, booking.ChildrenCount, booking.TotalAmount, booking.PaidAmount, booking.RemainingAmount,
        booking.IsDepositPayment, booking.PaymentMethodType, booking.PaymentAccountIdentifier, booking.PaymentReceiptUrl.Value,
        booking.SenderPaymentIdentifier, booking.Status, booking.CompanyReviewNote, booking.CreatedAt, booking.ReviewedAt, booking.CheckedInAt);

    private static string BuildCompanyNewBookingEmail(string companyName, string packageTitle, DateOnly tripDate, CompanyPackageBooking booking)
    {
        Func<string, string> encode = System.Net.WebUtility.HtmlEncode;
        return $"<div dir=\"rtl\"><h2>طلب حجز جديد يحتاج مراجعة</h2><p>شركة: {encode(companyName)}</p>" +
               $"<p>رقم الحجز: <strong>{encode(booking.BookingReference)}</strong></p>" +
               $"<p>الرحلة: {encode(packageTitle)} — {tripDate:yyyy-MM-dd}</p>" +
               $"<p>العميل: {encode(booking.CustomerName)} — {encode(booking.CustomerPhoneNumber)} — {encode(booking.CustomerEmail)}</p>" +
               $"<p>المقاعد: {booking.GuestsCount} (بالغون: {booking.AdultsCount}، أطفال: {booking.ChildrenCount})</p>" +
               $"<p>المطلوب: {booking.TotalAmount:N2} ج.م<br/>المرفوع كدفع: {booking.PaidAmount:N2} ج.م<br/>المتبقي: {booking.RemainingAmount:N2} ج.م</p>" +
               $"<p>وسيلة الدفع: {booking.PaymentMethodType} — المرسل: {encode(booking.SenderPaymentIdentifier)}</p>" +
               "<p>راجع الإيصال واتخذ القرار من لوحة التحكم.</p></div>";
    }
}

public abstract class CompanyPackageBookingReviewHandlerBase(ICompanyRepository companies, IClientContextProvider context)
{
    protected ICompanyRepository Companies { get; } = companies;
    protected IClientContextProvider Context { get; } = context;

    protected async Task<Result<Guid>> GetCompanyId(CancellationToken cancellationToken)
    {
        var current = Context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await Companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        return company is null ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.") : company.Id;
    }
}

public sealed class ConfirmCompanyPackageBookingCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageBookingRepository bookings,
    ICompanyPackageRepository packages, ICompanyBookingQrService qrService, IEmailService emailService,
    IUnitOfWork unitOfWork)
    : CompanyPackageBookingReviewHandlerBase(companies, context), ICommandHandler<ConfirmCompanyPackageBookingCommand, Result<CompanyPackageBookingResult>>
{
    public async Task<Result<CompanyPackageBookingResult>> Handle(ConfirmCompanyPackageBookingCommand request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var booking = await bookings.GetByIdAsync(companyId.Value, request.BookingId, true, cancellationToken);
        if (booking is null) return Error.NotFound("Booking.NotFound", "Booking was not found.");
        var confirmed = booking.Confirm(request.Note); if (confirmed.IsError) return confirmed.Errors;
        await unitOfWork.CommitChangesAsync(cancellationToken);
        try
        {
            var company = await Companies.GetByOwnerIdAsync(Context.GetContext().UserId, cancellationToken);
            var package = await packages.GetByIdAsync(companyId.Value, booking.PackageId, cancellationToken);
            var occurrence = await bookings.GetOccurrenceAsync(companyId.Value, booking.PackageId, booking.OccurrenceId, cancellationToken);
            if (company is not null && package is not null && occurrence is not null)
            {
                var expiresAt = occurrence.Date.AddDays(package.NumOfDays).ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                var qr = qrService.Generate(booking.Id, companyId.Value, expiresAt);
                var body = BuildConfirmationEmail(company.Name, package.Title, occurrence.Date, booking, qr.Token);
                await emailService.SendEmailWithInlineImageAsync(booking.CustomerEmail,
                    $"تأكيد حجزك {booking.BookingReference} - {company.Name}", body, qr.PngBytes, "booking-qr", "booking-qr.png");
            }
        }
        catch
        {
            // Email delivery must not roll back a payment receipt already approved by the company.
        }
        return CreatePublicPackageBookingCommandHandler.ToResult(booking);
    }

    private static string BuildConfirmationEmail(string companyName, string packageTitle, DateOnly tripDate,
        CompanyPackageBooking booking, string token)
    {
        Func<string, string> encode = System.Net.WebUtility.HtmlEncode;
        return $"<div dir=\"rtl\"><h2>تم تأكيد حجزك</h2><p>شركة: {encode(companyName)}</p>" +
               $"<p>رقم الحجز: <strong>{encode(booking.BookingReference)}</strong></p>" +
               $"<p>الرحلة: {encode(packageTitle)} — تاريخ الرحلة: {tripDate:yyyy-MM-dd}</p>" +
               $"<p>الاسم: {encode(booking.CustomerName)} — المقاعد: {booking.GuestsCount} " +
               $"(بالغون: {booking.AdultsCount}، أطفال: {booking.ChildrenCount})</p>" +
               $"<p>إجمالي الحجز: {booking.TotalAmount:N2} ج.م<br/>المدفوع: {booking.PaidAmount:N2} ج.م<br/>المتبقي: {booking.RemainingAmount:N2} ج.م</p>" +
               $"<p>اعرض QR التالي عند الوصول. لا تشاركه؛ يُستخدم مرة واحدة فقط.</p>" +
               "<img src=\"cid:booking-qr\" alt=\"Booking QR\" width=\"260\" height=\"260\" />" +
               $"<p style=\"display:none\">{encode(token)}</p></div>";
    }
}

public sealed class RejectCompanyPackageBookingCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageBookingRepository bookings, IUnitOfWork unitOfWork)
    : CompanyPackageBookingReviewHandlerBase(companies, context), ICommandHandler<RejectCompanyPackageBookingCommand, Result<CompanyPackageBookingResult>>
{
    public async Task<Result<CompanyPackageBookingResult>> Handle(RejectCompanyPackageBookingCommand request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var booking = await bookings.GetByIdAsync(companyId.Value, request.BookingId, true, cancellationToken);
        if (booking is null) return Error.NotFound("Booking.NotFound", "Booking was not found.");
        var occurrence = await bookings.GetOccurrenceAsync(companyId.Value, booking.PackageId, booking.OccurrenceId, cancellationToken);
        if (occurrence is null) return Error.NotFound("Booking.TripDateNotFound", "Trip date was not found.");
        var rejected = booking.Reject(request.Note); if (rejected.IsError) return rejected.Errors;
        occurrence.ReleaseSeats(booking.GuestsCount);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return CreatePublicPackageBookingCommandHandler.ToResult(booking);
    }
}
