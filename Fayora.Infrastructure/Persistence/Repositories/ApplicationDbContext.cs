using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Domain.Entities.AccommodationModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Entities.PackageModule;
using Fayora.Domain.Entities.IdentityModule;
using Fayora.Domain.Entities.SharedModule;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Infrastructure.Persistence.Repositories;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor, IPublisher publisher) : DbContext(options), IUnitOfWork
{
    // Identity Module
    public DbSet<User> Users { get; set; }
    public DbSet<UserIdentity> UserIdentities { get; set; }
    public DbSet<UserTokens> UserTokens { get; set; }
    public DbSet<UserDevice> UserDevices { get; set; }
    public DbSet<VerificationCode> VerificationCodes { get; set; }

    // Company Module
    public DbSet<Company> Companies { get; set; }
    public DbSet<CompanyWhatsAppNumber> CompanyWhatsAppNumbers { get; set; }
    public DbSet<CompanyPaymentMethod> CompanyPaymentMethods { get; set; }
    public DbSet<PackageAccommodationLink> PackageAccommodationLinks { get; set; }
    public DbSet<CompanyPackageBooking> CompanyPackageBookings { get; set; }
    public DbSet<CompanyAccommodationBooking> CompanyAccommodationBookings { get; set; }
    public DbSet<CompanyCustomerReview> CompanyCustomerReviews { get; set; }

    // Accommodation Module
    public DbSet<HousingUnit> HousingUnits { get; set; }
    public DbSet<HousingUnitImage> HousingUnitImages { get; set; }
    public DbSet<MasterAmenity> MasterAmenities { get; set; }


    // Travel package module
    public DbSet<TravelPackage> TravelPackages { get; set; }
    public DbSet<PackageImage> PackageImages { get; set; }
    public DbSet<PackageOccurrence> PackageOccurrences { get; set; }

    // Shared Module
    public DbSet<Location> Locations { get; set; }
    public DbSet<LocationImage> LocationImages { get; set; }

    public async Task CommitChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker.Entries<AggregateRoot>()
            .SelectMany(x => x.Entity.GetDomainEvents())
            .ToList();

        if (IsUserWaitingOnline())
        {
            AddDomainEventsToOfflineProcessingQueue(domainEvents);
        }
        else
        {
            await PublishDomainEvents(publisher, domainEvents);
        }

        await SaveChangesAsync(cancellationToken);
    }

    private static async Task PublishDomainEvents(IPublisher publisher, List<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent);
        }
    }

    private bool IsUserWaitingOnline() => httpContextAccessor.HttpContext is not null;

    private void AddDomainEventsToOfflineProcessingQueue(List<IDomainEvent> domainEvents)
    {
        var domainEventsQueue = httpContextAccessor.HttpContext!.Items
            .TryGetValue("DomainEventsQueue", out var value) && value is Queue<IDomainEvent> existingDomainEvents
                ? existingDomainEvents
                : new Queue<IDomainEvent>();

        domainEvents.ForEach(domainEventsQueue.Enqueue);

        httpContextAccessor.HttpContext!.Items["DomainEventsQueue"] = domainEventsQueue;
    }

    [DbFunction("DIFFERENCE", IsBuiltIn = true)]
    public static int Difference(string stringValue1, string stringValue2) => throw new NotImplementedException();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
