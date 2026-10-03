using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fayora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameTravelPackageModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyPackageBookings_GuideTourPackages_PackageId",
                table: "CompanyPackageBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageAccommodationLinks_GuideTourPackages_PackageId",
                table: "PackageAccommodationLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageOccurrences_GuideTourPackages_PackageId",
                table: "PackageOccurrences");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "PackageAccommodationGalleryImages");

            migrationBuilder.DropTable(
                name: "PackageActivities");

            migrationBuilder.DropTable(
                name: "PackageMeetingPoints");

            migrationBuilder.DropTable(
                name: "PackageNights");

            migrationBuilder.DropTable(
                name: "UnitOwners");

            migrationBuilder.DropTable(
                name: "PackageAccommodations");

            migrationBuilder.RenameTable(
                name: "GuideTourPackages",
                newName: "TravelPackages");

            migrationBuilder.RenameIndex(
                name: "IX_GuideTourPackages_CompanyId_PackageStatus",
                table: "TravelPackages",
                newName: "IX_TravelPackages_CompanyId_PackageStatus");

            migrationBuilder.DropColumn(name: "ProviderType", table: "TravelPackages");
            migrationBuilder.DropColumn(name: "ActivityIds", table: "TravelPackages");
            migrationBuilder.DropColumn(name: "NightIds", table: "TravelPackages");
            migrationBuilder.DropColumn(name: "OptionalActivities", table: "TravelPackages");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyPackageBookings_TravelPackages_PackageId",
                table: "CompanyPackageBookings",
                column: "PackageId",
                principalTable: "TravelPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageAccommodationLinks_TravelPackages_PackageId",
                table: "PackageAccommodationLinks",
                column: "PackageId",
                principalTable: "TravelPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageOccurrences_TravelPackages_PackageId",
                table: "PackageOccurrences",
                column: "PackageId",
                principalTable: "TravelPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyPackageBookings_TravelPackages_PackageId",
                table: "CompanyPackageBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageAccommodationLinks_TravelPackages_PackageId",
                table: "PackageAccommodationLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PackageOccurrences_TravelPackages_PackageId",
                table: "PackageOccurrences");

            migrationBuilder.DropTable(
                name: "TravelPackages");

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuideTourPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdultPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ArrivalNote = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    AverageRating = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CancellationPolicy = table.Column<int>(type: "int", nullable: false),
                    ChildPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DepositPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", nullable: false),
                    DurationHours = table.Column<int>(type: "int", nullable: false),
                    GroupDiscountMinPeople = table.Column<int>(type: "int", nullable: true),
                    GroupDiscountPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GuestRequirements = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    HasGroupDiscount = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    NumOfDays = table.Column<int>(type: "int", nullable: false),
                    PackageStatus = table.Column<int>(type: "int", nullable: false),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    RefundPolicyDetails = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    TourTypes = table.Column<int>(type: "int", nullable: false),
                    TransportType = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    ActivityIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExcludedItemIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IncludedItemIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NightIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionalActivities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MainImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    MainVideoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideTourPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuideTourPackages_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageAccommodations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amenities = table.Column<int>(type: "int", nullable: false),
                    CheckInTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CheckOutTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Meals = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    MainImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageAccommodations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsOptional = table.Column<bool>(type: "bit", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageNights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HousingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NightDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NightNumber = table.Column<int>(type: "int", nullable: false),
                    PackageAccommodationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageNights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitOwners",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvgResponseTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    CancellationRate = table.Column<float>(type: "real", nullable: false),
                    CommercialName = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    IsSuperHost = table.Column<bool>(type: "bit", nullable: false),
                    OwnerRating = table.Column<float>(type: "real", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    PreferredPayoutMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponseRate = table.Column<float>(type: "real", nullable: false),
                    VerificationStatus = table.Column<int>(type: "int", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOwners", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "PackageMeetingPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Time = table.Column<TimeOnly>(type: "time", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageMeetingPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageMeetingPoints_GuideTourPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "GuideTourPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageAccommodationGalleryImages",
                columns: table => new
                {
                    PackageAccommodationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageAccommodationGalleryImages", x => new { x.PackageAccommodationId, x.Id });
                    table.ForeignKey(
                        name: "FK_PackageAccommodationGalleryImages_PackageAccommodations_PackageAccommodationId",
                        column: x => x.PackageAccommodationId,
                        principalTable: "PackageAccommodations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuideTourPackages_CompanyId_PackageStatus",
                table: "GuideTourPackages",
                columns: new[] { "CompanyId", "PackageStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageMeetingPoints_PackageId",
                table: "PackageMeetingPoints",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOwners_UserId",
                table: "UnitOwners",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitOwners_VerificationStatus",
                table: "UnitOwners",
                column: "VerificationStatus");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyPackageBookings_GuideTourPackages_PackageId",
                table: "CompanyPackageBookings",
                column: "PackageId",
                principalTable: "GuideTourPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageAccommodationLinks_GuideTourPackages_PackageId",
                table: "PackageAccommodationLinks",
                column: "PackageId",
                principalTable: "GuideTourPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackageOccurrences_GuideTourPackages_PackageId",
                table: "PackageOccurrences",
                column: "PackageId",
                principalTable: "GuideTourPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
