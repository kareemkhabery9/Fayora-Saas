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

namespace Fayora.Application.Features.CompanyModule.CompanyAccommodationBookings;

public sealed record CreatePublicAccommodationBookingInput(string CustomerName, string CustomerPhoneNumber, string CustomerEmail,
    DateOnly CheckInDate, DateOnly CheckOutDate, int AdultsCount, int ChildrenCount, ManualPaymentMethodType PaymentMethodType,
    string SenderPaymentIdentifier, string PaymentReceiptUrl);
public sealed record CreatePublicAccommodationBookingCommand(string Slug, Guid AccommodationId, CreatePublicAccommodationBookingInput Input)
    : ICommand<Result<CompanyAccommodationBookingResult>>;
public sealed record ConfirmCompanyAccommodationBookingCommand(Guid BookingId, string? Note) : ICommand<Result<CompanyAccommodationBookingResult>>;
public sealed record RejectCompanyAccommodationBookingCommand(Guid BookingId, string? Note) : ICommand<Result<CompanyAccommodationBookingResult>>;
public sealed record ScanCompanyAccommodationBookingQrCommand(string Token) : ICommand<Result<AccommodationBookingQrScanResult>>;

public sealed record CompanyAccommodationBookingResult(Guid Id, string BookingReference, Guid AccommodationId, DateOnly CheckInDate,
    DateOnly CheckOutDate, int NightsCount, string CustomerName, string CustomerPhoneNumber, string CustomerEmail, int AdultsCount,
    int ChildrenCount, int GuestsCount, decimal TotalAmount, decimal PaidAmount, decimal RemainingAmount, ManualPaymentMethodType PaymentMethodType,
    string PaymentAccountIdentifier, string SenderPaymentIdentifier, string PaymentReceiptUrl, ManualBookingStatus Status,
    string? CompanyReviewNote, DateTimeOffset CreatedAt, DateTimeOffset? ReviewedAt, DateTimeOffset? CheckedInAt);
public sealed record AccommodationBookingQrScanResult(string BookingReference, string AccommodationName, DateOnly CheckInDate,
    DateOnly CheckOutDate, string CustomerName, int GuestsCount, decimal TotalAmount, decimal PaidAmount, decimal RemainingAmount,
    DateTimeOffset CheckedInAt);

public sealed class CreatePublicAccommodationBookingCommandHandler(IPublicStorefrontRepository storefront,
    ICompanyAccommodationBookingRepository bookings, IEmailService emailService, IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePublicAccommodationBookingCommand, Result<CompanyAccommodationBookingResult>>
{
    public async Task<Result<CompanyAccommodationBookingResult>> Handle(CreatePublicAccommodationBookingCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug)) return Error.NotFound("AccommodationBooking.CompanyNotFound", "Company storefront was not found.");
        var company = await storefront.GetCompanyBySlugAsync(request.Slug.Trim().ToLowerInvariant(), cancellationToken);
        if (company is null) return Error.NotFound("AccommodationBooking.CompanyNotFound", "Company storefront was not found.");
        var unit = await bookings.GetAvailableAccommodationAsync(company.Id, request.AccommodationId, cancellationToken);
        if (unit is null) return Error.NotFound("AccommodationBooking.NotFound", "Accommodation was not found or is not available.");
        if (request.Input.AdultsCount + request.Input.ChildrenCount > unit.MaxGuests) return Error.Validation("AccommodationBooking.Capacity", "Guest count exceeds accommodation capacity.");
        if (request.Input.CheckOutDate <= request.Input.CheckInDate) return Error.Validation("AccommodationBooking.Dates", "Check-out must be after check-in.");
        if (await bookings.HasOverlapAsync(unit.Id, request.Input.CheckInDate, request.Input.CheckOutDate, cancellationToken))
            return Error.Conflict("AccommodationBooking.Unavailable", "Accommodation is not available for the selected dates.");
        var method = company.PaymentMethods.FirstOrDefault(x => x.Type == request.Input.PaymentMethodType && x.IsEnabled);
        if (method is null) return Error.Validation("AccommodationBooking.PaymentMethod", "Selected payment method is not available for this company.");
        var receipt = FileUrl.Create(request.Input.PaymentReceiptUrl); if (receipt.IsError) return receipt.Errors;
        var total = decimal.Round(unit.PricePerNight * (request.Input.CheckOutDate.DayNumber - request.Input.CheckInDate.DayNumber), 2, MidpointRounding.AwayFromZero);
        var booking = CompanyAccommodationBooking.Create(company.Id, unit.Id, request.Input.CheckInDate, request.Input.CheckOutDate,
            request.Input.AdultsCount, request.Input.ChildrenCount, request.Input.CustomerName, request.Input.CustomerPhoneNumber,
            request.Input.CustomerEmail, total, method.Type, method.AccountIdentifier, request.Input.SenderPaymentIdentifier, receipt.Value);
        if (booking.IsError) return booking.Errors;
        bookings.Add(booking.Value);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        try { await emailService.SendEmailAsync(company.ContactEmail.Value, $"حجز إقامة جديد يحتاج مراجعة - {booking.Value.BookingReference}", $"<div dir=\"rtl\"><h2>طلب حجز إقامة جديد</h2><p>رقم الحجز: <strong>{booking.Value.BookingReference}</strong></p><p>العميل: {System.Net.WebUtility.HtmlEncode(booking.Value.CustomerName)}</p><p>تاريخ الدخول: {booking.Value.CheckInDate:yyyy-MM-dd} — الخروج: {booking.Value.CheckOutDate:yyyy-MM-dd}</p><p>المبلغ: {booking.Value.TotalAmount:N2} ج.م</p></div>"); } catch { }
        return ToResult(booking.Value);
    }

    internal static CompanyAccommodationBookingResult ToResult(CompanyAccommodationBooking b) => new(b.Id, b.BookingReference, b.AccommodationId,
        b.CheckInDate, b.CheckOutDate, b.NightsCount, b.CustomerName, b.CustomerPhoneNumber, b.CustomerEmail, b.AdultsCount,
        b.ChildrenCount, b.GuestsCount, b.TotalAmount, b.PaidAmount, b.RemainingAmount, b.PaymentMethodType, b.PaymentAccountIdentifier,
        b.SenderPaymentIdentifier, b.PaymentReceiptUrl.Value, b.Status, b.CompanyReviewNote, b.CreatedAt, b.ReviewedAt, b.CheckedInAt);
}

public abstract class CompanyAccommodationBookingReviewHandlerBase(ICompanyRepository companies, IClientContextProvider context)
{
    protected ICompanyRepository Companies { get; } = companies;
    protected IClientContextProvider Context { get; } = context;

    protected async Task<Result<Guid>> GetCompanyId(CancellationToken ct)
    {
        var current = Context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await Companies.GetByOwnerIdAsync(current.UserId, ct);
        return company is null ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.") : company.Id;
    }
}

public sealed class ConfirmCompanyAccommodationBookingCommandHandler(ICompanyRepository companies, IClientContextProvider context,
    ICompanyAccommodationBookingRepository bookings, ICompanyAccommodationRepository accommodations, ICompanyBookingQrService qrService,
    IEmailService emailService, IUnitOfWork unitOfWork) : CompanyAccommodationBookingReviewHandlerBase(companies, context),
    ICommandHandler<ConfirmCompanyAccommodationBookingCommand, Result<CompanyAccommodationBookingResult>>
{
    public async Task<Result<CompanyAccommodationBookingResult>> Handle(ConfirmCompanyAccommodationBookingCommand request, CancellationToken ct)
    {
        var companyId = await GetCompanyId(ct); if (companyId.IsError) return companyId.Errors;
        var booking = await bookings.GetByIdAsync(companyId.Value, request.BookingId, true, ct);
        if (booking is null) return Error.NotFound("AccommodationBooking.NotFound", "Booking was not found.");
        var confirmed = booking.Confirm(request.Note); if (confirmed.IsError) return confirmed.Errors;
        await unitOfWork.CommitChangesAsync(ct);
        try
        {
            var company = await Companies.GetByOwnerIdAsync(Context.GetContext().UserId, ct);
            var accommodation = await accommodations.GetByIdAsync(companyId.Value, booking.AccommodationId, false, ct);
            if (company is not null && accommodation is not null)
            {
                var qr = qrService.Generate(booking.Id, companyId.Value, booking.CheckOutDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
                var body = $"<div dir=\"rtl\"><h2>تم تأكيد حجز الإقامة</h2><p>رقم الحجز: <strong>{booking.BookingReference}</strong></p><p>الإقامة: {System.Net.WebUtility.HtmlEncode(accommodation.Title)}</p><p>الدخول: {booking.CheckInDate:yyyy-MM-dd} — الخروج: {booking.CheckOutDate:yyyy-MM-dd}</p><p>إجمالي: {booking.TotalAmount:N2} ج.م — المدفوع: {booking.PaidAmount:N2} ج.م — المتبقي: {booking.RemainingAmount:N2} ج.م</p><p>اعرض QR عند الوصول؛ يُستخدم مرة واحدة فقط.</p><img src=\"cid:booking-qr\" width=\"260\" height=\"260\" /></div>";
                await emailService.SendEmailWithInlineImageAsync(booking.CustomerEmail, $"تأكيد حجزك {booking.BookingReference} - {company.Name}", body, qr.PngBytes, "booking-qr", "booking-qr.png");
            }
        }
        catch { }
        return CreatePublicAccommodationBookingCommandHandler.ToResult(booking);
    }
}

public sealed class RejectCompanyAccommodationBookingCommandHandler(ICompanyRepository companies, IClientContextProvider context,
    ICompanyAccommodationBookingRepository bookings, IUnitOfWork unitOfWork) : CompanyAccommodationBookingReviewHandlerBase(companies, context),
    ICommandHandler<RejectCompanyAccommodationBookingCommand, Result<CompanyAccommodationBookingResult>>
{
    public async Task<Result<CompanyAccommodationBookingResult>> Handle(RejectCompanyAccommodationBookingCommand request, CancellationToken ct)
    {
        var companyId = await GetCompanyId(ct); if (companyId.IsError) return companyId.Errors;
        var booking = await bookings.GetByIdAsync(companyId.Value, request.BookingId, true, ct);
        if (booking is null) return Error.NotFound("AccommodationBooking.NotFound", "Booking was not found.");
        var rejected = booking.Reject(request.Note); if (rejected.IsError) return rejected.Errors;
        await unitOfWork.CommitChangesAsync(ct);
        return CreatePublicAccommodationBookingCommandHandler.ToResult(booking);
    }
}

public sealed class ScanCompanyAccommodationBookingQrCommandHandler(ICompanyRepository companies, IClientContextProvider context,
    ICompanyAccommodationBookingRepository bookings, ICompanyAccommodationRepository accommodations, ICompanyBookingQrService qrService,
    IUnitOfWork unitOfWork) : CompanyAccommodationBookingReviewHandlerBase(companies, context), ICommandHandler<ScanCompanyAccommodationBookingQrCommand, Result<AccommodationBookingQrScanResult>>
{
    public async Task<Result<AccommodationBookingQrScanResult>> Handle(ScanCompanyAccommodationBookingQrCommand request, CancellationToken ct)
    {
        var companyId = await GetCompanyId(ct); if (companyId.IsError) return companyId.Errors;
        var token = qrService.Validate(request.Token); if (token.IsError) return token.Errors;
        if (token.Value.CompanyId != companyId.Value) return Error.Forbidden("This QR code does not belong to your company.");
        var booking = await bookings.GetByIdAsync(companyId.Value, token.Value.BookingId, true, ct);
        if (booking is null) return Error.NotFound("AccommodationBooking.NotFound", "Booking was not found.");
        var accommodation = await accommodations.GetByIdAsync(companyId.Value, booking.AccommodationId, false, ct);
        if (accommodation is null) return Error.NotFound("Accommodation.NotFound", "Accommodation was not found.");
        var checkIn = booking.MarkCheckedIn(); if (checkIn.IsError) return checkIn.Errors;
        await unitOfWork.CommitChangesAsync(ct);
        return new AccommodationBookingQrScanResult(booking.BookingReference, accommodation.Title, booking.CheckInDate, booking.CheckOutDate,
            booking.CustomerName, booking.GuestsCount, booking.TotalAmount, booking.PaidAmount, booking.RemainingAmount, booking.CheckedInAt!.Value);
    }
}
