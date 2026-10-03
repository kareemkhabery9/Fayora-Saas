using Fayora.Application.Features.CompanyModule.Commands.RegisterCompanyWithEmail;
using Fayora.Application.Features.CompanyModule.Commands.LoginCompanyWithGoogle;
using Fayora.Application.Features.CompanyModule.Commands.LoginCompanyWithEmail;
using Fayora.Application.Features.CompanyModule.Commands.RequestCompanyPasswordReset;
using Fayora.Application.Features.CompanyModule.Commands.ResetCompanyPassword;
using Fayora.Application.Features.CompanyModule.Commands.ResendCompanyEmailVerification;
using Fayora.Application.Features.CompanyModule.Commands.VerifyCompanyEmail;
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

[Route("api/company-auth")]
[EnableRateLimiting("public-write")]
public sealed class CompanyAuthController(ISender sender) : ApiController
{
    [HttpPost("login/email")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithEmail([FromBody] CompanyEmailLoginRequest request,
        [FromHeader(Name = AppHeaders.DeviceId)] string deviceId, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Language>(request.DeviceLanguage, true, out var language)) return BadRequest("Invalid device language.");
        var result = await sender.Send(new LoginCompanyWithEmailCommand(request.Email, request.Password, deviceId, language), cancellationToken);
        return result.Match(value => Ok(new CompanyLoginResponse(
            value.CompanyId,
            value.UserId,
            value.Email,
            value.CompanyName,
            value.Slug,
            value.IsOnboardingCompleted,
            value.AccessToken,
            value.RefreshToken,
            value.ExpiresIn)), Problem);
    }

    [HttpPost("login/google")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithGoogle(
        [FromBody] CompanyGoogleLoginRequest request,
        [FromHeader(Name = AppHeaders.DeviceId)] string deviceId,
        CancellationToken cancellationToken)
    {
        var (isValidLanguage, deviceLanguage) = EnumParser.TryParseEnum<Language>(request.DeviceLanguage);
        if (!isValidLanguage)
            return BadRequest("Invalid device language.");

        var result = await sender.Send(new LoginCompanyWithGoogleCommand(
            request.GoogleIdToken,
            deviceId,
            deviceLanguage,
            request.CompanyName,
            request.PhoneNumber), cancellationToken);

        return result.Match(
            value => Ok(new CompanyGoogleLoginResponse(
                value.CompanyId,
                value.UserId,
                value.Email,
                value.IsFirstLogin,
                value.AccessToken,
                value.RefreshToken,
                value.ExpiresIn)),
            Problem);
    }

    [HttpPost("register/email")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterWithEmail(
        [FromBody] RegisterCompanyWithEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterCompanyWithEmailCommand(
            request.CompanyName,
            request.OwnerFirstName,
            request.OwnerLastName,
            request.Email,
            request.PhoneNumber,
            request.Password), cancellationToken);

        return result.Match(
            value => Created(string.Empty, new RegisterCompanyWithEmailResponse(value.CompanyId, value.UserId, value.Email)),
            Problem);
    }

    [HttpPost("email/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] CompanyEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyCompanyEmailCommand(request.Email, request.Code), cancellationToken);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("email/resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendEmailVerification(
        [FromBody] CompanyForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (Email.Create(request.Email).IsError)
            return BadRequest("Invalid email.");

        await sender.Send(new ResendCompanyEmailVerificationCommand(request.Email), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Requests a one-time password-reset code for a company account.
    /// Always returns 204 so callers cannot discover whether an email is registered.
    /// </summary>
    [HttpPost("password/forgot")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] CompanyForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (Email.Create(request.Email).IsError)
            return BadRequest("Invalid email.");

        await sender.Send(new RequestCompanyPasswordResetCommand(request.Email), cancellationToken);
        return NoContent();
    }

    [HttpPost("password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] CompanyResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ResetCompanyPasswordCommand(request.Email, request.Code, request.NewPassword),
            cancellationToken);

        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("token/refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshSession(
        [FromBody] AuthRefreshTokenRequest request,
        [FromHeader(Name = AppHeaders.DeviceId)] string deviceId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshSessionCommand(request.RefreshToken, deviceId, Role.Company), cancellationToken);
        return result.Match(value => Ok(new AuthTokenResponse(value.AccessToken, value.RefreshToken, value.ExpiresIn)), Problem);
    }

    [HttpPost("logout")]
    [Authorize(Roles = "Company")]
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
