using Fayora.Application.Features.CompanyModule.SuperAdmin.Auth;
using Fayora.Application.Features.AuthModule.Logout;
using Fayora.Application.Features.AuthModule.RefreshSession;
using Fayora.Contracts.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fayora.Api.Controllers;

[Route("api/super-admin/auth")]
[EnableRateLimiting("public-write")]
public sealed class SuperAdminAuthController(ISender sender) : ApiController
{
    [HttpPost("login/email")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithEmail(
        [FromBody] SuperAdminEmailLoginRequest request,
        [FromHeader(Name = AppHeaders.DeviceId)] string deviceId,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Language>(request.DeviceLanguage, true, out var language))
            return BadRequest("Invalid device language.");

        var result = await sender.Send(
            new LoginSuperAdminWithEmailCommand(request.Email, request.Password, deviceId, language),
            cancellationToken);

        return result.Match(
            value => Ok(new SuperAdminLoginResponse(value.UserId, value.Email, value.FullName,
                value.AccessToken, value.RefreshToken, value.ExpiresIn)),
            Problem);
    }

    [HttpPost("password/forgot")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] CompanyForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (Email.Create(request.Email).IsError)
            return BadRequest("Invalid email.");

        await sender.Send(new RequestSuperAdminPasswordResetCommand(request.Email), cancellationToken);
        return NoContent();
    }

    [HttpPost("password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] CompanyResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetSuperAdminPasswordCommand(request.Email, request.Code, request.NewPassword), cancellationToken);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("token/refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshSession(
        [FromBody] AuthRefreshTokenRequest request,
        [FromHeader(Name = AppHeaders.DeviceId)] string deviceId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshSessionCommand(request.RefreshToken, deviceId, Role.SuperAdmin), cancellationToken);
        return result.Match(value => Ok(new AuthTokenResponse(value.AccessToken, value.RefreshToken, value.ExpiresIn)), Problem);
    }

    [HttpPost("logout")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Logout(
        [FromHeader(Name = AppHeaders.DeviceId)] string deviceId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
            return Unauthorized();

        await sender.Send(new LogoutCommand(userId, deviceId), cancellationToken);
        return NoContent();
    }
}
