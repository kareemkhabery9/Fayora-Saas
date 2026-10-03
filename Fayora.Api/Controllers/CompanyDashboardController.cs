using Fayora.Application.Features.CompanyModule.CompanyDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;

namespace Fayora.Api.Controllers;

[Authorize(Roles = "Company")]
[Route("api/company/dashboard")]
public sealed class CompanyDashboardController(ISender sender) : ApiController
{
    [HttpGet("home")]
    public async Task<IActionResult> GetHome(CancellationToken ct)
    {
        var result = await sender.Send(new GetCompanyDashboardHomeQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings([FromQuery] string? status, [FromQuery] bool? isDepositPayment, [FromQuery] string? serviceType,
        [FromQuery] Guid? serviceId, [FromQuery] string? search,
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        Fayora.Domain.Enums.CompanyModule.ManualBookingStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<Fayora.Domain.Enums.CompanyModule.ManualBookingStatus>(status, true, out var value)) return BadRequest("Invalid booking status.");
            parsedStatus = value;
        }
        if (!string.IsNullOrWhiteSpace(serviceType) && !serviceType.Equals("Package", StringComparison.OrdinalIgnoreCase) && !serviceType.Equals("Accommodation", StringComparison.OrdinalIgnoreCase)) return BadRequest("Service type must be Package or Accommodation.");
        var result = await sender.Send(new GetCompanyDashboardBookingsQuery(parsedStatus, isDepositPayment, serviceType, serviceId, search, fromDate, toDate, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("bookings/status-summary")]
    public async Task<IActionResult> GetBookingStatusSummary(CancellationToken ct)
    {
        var result = await sender.Send(new GetCompanyDashboardBookingStatusSummaryQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("bookings/{bookingId:guid}")]
    public async Task<IActionResult> GetBooking(Guid bookingId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCompanyDashboardBookingQuery(bookingId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("bookings/export")]
    public async Task<IActionResult> ExportBookings([FromQuery] string? status, [FromQuery] bool? isDepositPayment, [FromQuery] string? serviceType,
        [FromQuery] Guid? serviceId, [FromQuery] string? search,
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        Fayora.Domain.Enums.CompanyModule.ManualBookingStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<Fayora.Domain.Enums.CompanyModule.ManualBookingStatus>(status, true, out var value)) return BadRequest("Invalid booking status.");
            parsedStatus = value;
        }
        if (!string.IsNullOrWhiteSpace(serviceType) && !serviceType.Equals("Package", StringComparison.OrdinalIgnoreCase) && !serviceType.Equals("Accommodation", StringComparison.OrdinalIgnoreCase)) return BadRequest("Service type must be Package or Accommodation.");
        var result = await sender.Send(new ExportCompanyDashboardBookingsQuery(parsedStatus, isDepositPayment, serviceType, serviceId, search, fromDate, toDate), ct);
        return result.Match(CreateExcelFile, Problem);
    }

    private static IActionResult CreateExcelFile(IReadOnlyList<CompanyDashboardBookingDetailsResult> bookings)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("الحجوزات");
        sheet.RightToLeft = true;
        var headers = new[] { "رقم الحجز", "نوع الخدمة", "اسم الخدمة", "تاريخ الخدمة", "اسم العميل", "رقم الهاتف", "البريد الإلكتروني",
            "بالغون", "أطفال", "إجمالي الأفراد", "نوع الدفع", "إجمالي الحجز", "المدفوع", "المتبقي", "وسيلة الدفع",
            "حساب الشركة", "حساب/رقم المرسل", "رابط الإيصال", "حالة الحجز", "ملاحظة الشركة", "تاريخ الحجز" };
        for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column];
        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#155E75");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        for (var row = 0; row < bookings.Count; row++)
        {
            var b = bookings[row];
            var cells = sheet.Row(row + 2);
            cells.Cell(1).Value = b.BookingReference;
            cells.Cell(2).Value = b.ServiceType == "Accommodation" ? "إقامة" : "رحلة";
            cells.Cell(3).Value = b.ServiceName;
            cells.Cell(4).Value = b.StartDate.ToDateTime(TimeOnly.MinValue);
            cells.Cell(5).Value = b.CustomerName;
            cells.Cell(6).Value = b.CustomerPhoneNumber;
            cells.Cell(7).Value = b.CustomerEmail;
            cells.Cell(8).Value = b.AdultsCount;
            cells.Cell(9).Value = b.ChildrenCount;
            cells.Cell(10).Value = b.GuestsCount;
            cells.Cell(11).Value = b.IsDepositPayment ? "عربون" : "دفع كامل";
            cells.Cell(12).Value = b.TotalAmount;
            cells.Cell(13).Value = b.PaidAmount;
            cells.Cell(14).Value = b.RemainingAmount;
            cells.Cell(15).Value = b.PaymentMethodType;
            cells.Cell(16).Value = b.PaymentAccountIdentifier;
            cells.Cell(17).Value = b.SenderPaymentIdentifier;
            cells.Cell(18).Value = b.PaymentReceiptUrl;
            cells.Cell(19).Value = b.Status;
            cells.Cell(20).Value = b.CompanyReviewNote ?? string.Empty;
            cells.Cell(21).Value = b.CreatedAt.DateTime;
        }
        if (bookings.Count > 0)
        {
            sheet.Range(2, 4, bookings.Count + 1, 4).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Range(2, 12, bookings.Count + 1, 14).Style.NumberFormat.Format = "#,##0.00";
            sheet.Range(2, 21, bookings.Count + 1, 21).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        }
        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(1, bookings.Count + 1), headers.Length).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Columns().AdjustToContents();
        sheet.Column(18).Width = 38;
        sheet.Column(20).Width = 30;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = $"fayora-bookings-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx"
        };
    }
}
