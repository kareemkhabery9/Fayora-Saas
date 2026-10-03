using Fayora.Infrastructure.Persistence.Repositories;
using Fayora.Infrastructure.Services.CompanyModule;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Api.Externals;

public static class WebApplicationExtensions
{
    public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        var superAdminBootstrapper = scope.ServiceProvider.GetRequiredService<ISuperAdminBootstrapper>();
        await superAdminBootstrapper.EnsureCreatedAsync(cancellationToken);
        return app;
    }

}
