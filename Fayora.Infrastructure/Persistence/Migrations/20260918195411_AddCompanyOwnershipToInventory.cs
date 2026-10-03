using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fayora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyOwnershipToInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "HousingUnits",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "GuideTourPackages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CompanyId_TargetId_TargetType",
                table: "Reviews",
                columns: new[] { "CompanyId", "TargetId", "TargetType" });

            migrationBuilder.CreateIndex(
                name: "IX_HousingUnits_CompanyId_Status",
                table: "HousingUnits",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GuideTourPackages_CompanyId_PackageStatus",
                table: "GuideTourPackages",
                columns: new[] { "CompanyId", "PackageStatus" });

            migrationBuilder.AddForeignKey(
                name: "FK_GuideTourPackages_Companies_CompanyId",
                table: "GuideTourPackages",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HousingUnits_Companies_CompanyId",
                table: "HousingUnits",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Companies_CompanyId",
                table: "Reviews",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuideTourPackages_Companies_CompanyId",
                table: "GuideTourPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_HousingUnits_Companies_CompanyId",
                table: "HousingUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Companies_CompanyId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_CompanyId_TargetId_TargetType",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_HousingUnits_CompanyId_Status",
                table: "HousingUnits");

            migrationBuilder.DropIndex(
                name: "IX_GuideTourPackages_CompanyId_PackageStatus",
                table: "GuideTourPackages");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "HousingUnits");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "GuideTourPackages");
        }
    }
}
