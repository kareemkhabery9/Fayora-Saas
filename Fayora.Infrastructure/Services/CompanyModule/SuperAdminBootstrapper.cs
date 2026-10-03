using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Domain.Entities.IdentityModule;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.ValueObjects;
using Fayora.Infrastructure.Persistence.Repositories;
using Fayora.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fayora.Infrastructure.Services.CompanyModule;

public interface ISuperAdminBootstrapper
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}

internal sealed class SuperAdminBootstrapper(
    ApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IOptions<BootstrapSuperAdminSettings> settings,
    ILogger<SuperAdminBootstrapper> logger) : ISuperAdminBootstrapper
{
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        var config = settings.Value;
        if (!config.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(config.Email) || string.IsNullOrWhiteSpace(config.Password))
        {
            logger.LogWarning("Super admin bootstrap is enabled but its email or password is not configured. No account was created.");
            return;
        }

        var configuredEmail = Email.Create(config.Email);
        if (configuredEmail.IsError)
            throw new InvalidOperationException("BootstrapSuperAdmin:Email is invalid.");

        var existingUser = await context.Users
            .FirstOrDefaultAsync(user => user.PrimaryEmail == configuredEmail.Value, cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.Roles?.HasFlag(Role.SuperAdmin) == true)
                return;

            throw new InvalidOperationException("BootstrapSuperAdmin:Email already belongs to a non-super-admin account.");
        }

        var userResult = User.CreateSuperAdmin(
            config.FirstName,
            config.LastName,
            config.Email,
            config.Password,
            passwordHasher);

        if (userResult.IsError)
            throw new InvalidOperationException($"Bootstrap super admin is invalid: {userResult.Errors.First().Description}");

        context.Users.Add(userResult.Value);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Initial super admin account was created for {Email}.", config.Email);
    }
}
