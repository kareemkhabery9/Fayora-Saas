using Fayora.Domain.Common.Results;

namespace Fayora.Application.Common.Interfaces.Services.CompanyModule;

public interface ICompanyBookingQrService
{
    CompanyBookingQr Generate(Guid bookingId, Guid companyId, DateTime expiresAt);
    Result<CompanyBookingQrPayload> Validate(string token);
}

public sealed record CompanyBookingQr(string Token, byte[] PngBytes);
public sealed record CompanyBookingQrPayload(Guid BookingId, Guid CompanyId);
