using Fayora.Application.Features.CompanyModule.SuperAdmin;
using Fayora.Domain.Enums.CompanyModule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fayora.Api.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("api/super-admin/companies")]
public sealed class SuperAdminCompaniesController(ISender sender) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        CompanyStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status)) { if (!Enum.TryParse<CompanyStatus>(status, true, out var value)) return BadRequest("Invalid company status."); parsedStatus = value; }
        var result = await sender.Send(new GetSuperAdminCompaniesQuery(parsedStatus, search, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{companyId:guid}")]
    public async Task<IActionResult> GetById(Guid companyId, CancellationToken ct)
    {
        var result = await sender.Send(new GetSuperAdminCompanyQuery(companyId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{companyId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid companyId, CancellationToken ct)
    {
        var result = await sender.Send(new ActivateCompanyCommand(companyId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPatch("{companyId:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid companyId, CancellationToken ct)
    {
        var result = await sender.Send(new SuspendCompanyCommand(companyId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("dashboard/home")]
    public async Task<IActionResult> GetDashboardHome([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var result = await sender.Send(new GetSuperAdminDashboardHomeQuery(fromDate, toDate), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("dashboard/bookings")]
    public async Task<IActionResult> GetBookings([FromQuery] Guid? companyId, [FromQuery] string? status, [FromQuery] string? serviceType,
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        ManualBookingStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status)) { if (!Enum.TryParse<ManualBookingStatus>(status, true, out var value)) return BadRequest("Invalid booking status."); parsedStatus = value; }
        if (!string.IsNullOrWhiteSpace(serviceType) && !serviceType.Equals("Package", StringComparison.OrdinalIgnoreCase) && !serviceType.Equals("Accommodation", StringComparison.OrdinalIgnoreCase)) return BadRequest("Service type must be Package or Accommodation.");
        var result = await sender.Send(new GetSuperAdminBookingsQuery(companyId, parsedStatus, serviceType, fromDate, toDate, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{companyId:guid}/services")]
    public async Task<IActionResult> GetServices(Guid companyId, [FromQuery] string? serviceType, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(serviceType) && !serviceType.Equals("Package", StringComparison.OrdinalIgnoreCase) && !serviceType.Equals("Accommodation", StringComparison.OrdinalIgnoreCase)) return BadRequest("Service type must be Package or Accommodation.");
        var result = await sender.Send(new GetSuperAdminCompanyServicesQuery(companyId, serviceType, page, pageSize), ct);
        return result.Match(Ok, Problem);
    }
}
