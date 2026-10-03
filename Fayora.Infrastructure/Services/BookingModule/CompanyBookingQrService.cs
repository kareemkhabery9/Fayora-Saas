using Fayora.Application.Common.Interfaces.Services.CompanyModule;
using Fayora.Domain.Common.Results;
using Fayora.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QRCoder;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Fayora.Infrastructure.Services.CompanyModule;

public sealed class CompanyBookingQrService(IOptions<JwtSettings> jwtSettings) : ICompanyBookingQrService
{
    private const string Purpose = "company-package-checkin";

    public CompanyBookingQr Generate(Guid bookingId, Guid companyId, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Value.Secret));
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim("purpose", Purpose),
                new Claim("bookingId", bookingId.ToString()),
                new Claim("companyId", companyId.ToString())
            ],
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(serializedToken, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(12);
        return new CompanyBookingQr(serializedToken, png);
    }

    public Result<CompanyBookingQrPayload> Validate(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Value.Secret));
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            if (principal.FindFirstValue("purpose") != Purpose ||
                !Guid.TryParse(principal.FindFirstValue("bookingId"), out var bookingId) ||
                !Guid.TryParse(principal.FindFirstValue("companyId"), out var companyId))
                return Error.Failure("Invalid QR token.");
            return new CompanyBookingQrPayload(bookingId, companyId);
        }
        catch { return Error.Failure("Invalid or expired QR token."); }
    }
}
