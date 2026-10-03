using Fayora.Api.Services;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace Fayora.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddControllers()
          .AddJsonOptions(options =>
          {
              options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
              options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
          });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddProblemDetails();
        var publicWritePermitLimit = configuration.GetValue<int?>("RateLimiting:PublicWritePermitLimit") ?? 12;
        var publicWriteWindowSeconds = configuration.GetValue<int?>("RateLimiting:PublicWriteWindowSeconds") ?? 60;
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("public-write", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = publicWritePermitLimit,
                    Window = TimeSpan.FromSeconds(publicWriteWindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });

        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length == 0)
            throw new InvalidOperationException("Cors:AllowedOrigins must define at least one frontend origin.");

        services.AddCors(options => options.AddPolicy("frontend", policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        services.AddScoped<IClientContextProvider, ClientContextProvider>();

        return services;
    }
}
