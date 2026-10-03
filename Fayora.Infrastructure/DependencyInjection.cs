using Fayora.Application.Common.Abstractions.Caching;
using Fayora.Application.Common.Interfaces.Persistences.AccommodationModule;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Persistences.SharedModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Common.Interfaces.Services.CompanyModule;
using Fayora.Application.Common.Interfaces.Services.SharedModule;
using Fayora.Application.Common.Strategies;
using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Infrastructure.Persistence.Caching;
using Fayora.Infrastructure.Persistence.Repositories;
using Fayora.Infrastructure.Persistence.Repositories.AccommodationModule;
using Fayora.Infrastructure.Persistence.Repositories.IdentityModule;
using Fayora.Infrastructure.Persistence.Repositories.CompanyModule;
using Fayora.Infrastructure.Persistence.Repositories.SharedModule;
using Fayora.Infrastructure.Services.Authentication;
using Fayora.Infrastructure.Services.AuthModule;
using Fayora.Infrastructure.Services.CompanyModule;
using Fayora.Infrastructure.Services.SharedModule;
using Fayora.Infrastructure.Settings;
using Fayora.Infrastructure.Strategies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;


namespace Fayora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddAuthentication(configuration)
            .AddPersistence(configuration)
            .AddService(configuration);
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        var cacheProvider = configuration["Cache:Provider"] ?? "Memory";
        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            var redisConnectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redisConnectionString))
                throw new InvalidOperationException("ConnectionStrings:Redis is required when Cache:Provider is Redis.");

            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        }
        else
        {
            services.AddDistributedMemoryCache();
        }


        // Identity Module
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyPackageRepository, CompanyPackageRepository>();
        services.AddScoped<ICompanyAccommodationRepository, CompanyAccommodationRepository>();
        services.AddScoped<IPublicStorefrontRepository, PublicStorefrontRepository>();
        services.AddScoped<ICompanyPackageBookingRepository, CompanyPackageBookingRepository>();
        services.AddScoped<ICompanyAccommodationBookingRepository, CompanyAccommodationBookingRepository>();
        services.AddScoped<ICompanyCustomerReviewRepository, CompanyCustomerReviewRepository>();
        services.AddScoped<ICompanyDashboardRepository, CompanyDashboardRepository>();
        services.AddScoped<ISuperAdminDashboardRepository, SuperAdminDashboardRepository>();
        services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
        services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();


        // Accommodation Module
        services.AddScoped<IMasterAmenityRepository, MasterAmenityRepository>();

        // Shared Module
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ILocationImageRepository, LocationImageRepository>();

        services.AddSingleton<ICompanyBookingQrService, CompanyBookingQrService>();

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<ICacheService, CacheService>();


        return services;
    }

    public static IServiceCollection AddService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IUserTokenService, UserTokenService>();
        services.AddSingleton<IVerificationCodeService, VerificationCodeService>();
        services.AddSingleton<ICodeHasher, CodeHasher>();
        services.AddSingleton<ITokenHasher, TokenHasher>();
        services.AddSingleton<IMessageGenerator, MessageGenerator>();
        services.AddScoped<IUserDeviceManager, UserDeviceManager>();
        services.AddScoped<IAuthTokenGenerator, AuthTokenGenerator>();
        services.AddScoped<IStorageService, CloudinaryStorageService>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<GoogleSettings>(configuration.GetSection(GoogleSettings.SectionName));
        services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));
        services.Configure<BootstrapSuperAdminSettings>(configuration.GetSection(BootstrapSuperAdminSettings.SectionName));


        services.AddMemoryCache();
        services.AddSingleton<IDailyUploadTracker, RadisDailyUploadTracker>();

        services.AddScoped<IUploadStrategy, ProfileImageUploadStrategy>();
        services.AddScoped<IUploadStrategy, HousingUnitUploadStrategy>();
        services.AddScoped<IUploadStrategy, PackageImageUploadStrategy>();

        services.AddSingleton<IEmailService, EmailService>();
        services.AddScoped<ISuperAdminBootstrapper, SuperAdminBootstrapper>();

        services.AddSingleton<ISocialAuthService, SocialAuthService>();
        services.AddSingleton<ISocialAuthStrategy, GoogleAuthStrategy>();

        services.AddScoped<IFileStorageService, LocalFileService>();
        return services;
    }

    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings();
        configuration.Bind(JwtSettings.Section, jwtSettings);

        services.AddSingleton(Options.Create(jwtSettings));
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                RoleClaimType = "roles",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            });

        return services;
    }

}
