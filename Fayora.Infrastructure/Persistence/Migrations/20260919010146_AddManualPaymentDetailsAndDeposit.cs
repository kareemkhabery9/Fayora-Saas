using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fayora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualPaymentDetailsAndDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercentage",
                table: "GuideTourPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InstaPayQrCodeUrl",
                table: "CompanyPaymentMethods",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "CompanyPackageBookings",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(320)",
                oldMaxLength: 320,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingReference",
                table: "CompanyPackageBookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckedInAt",
                table: "CompanyPackageBookings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDepositPayment",
                table: "CompanyPackageBookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "CompanyPackageBookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAmount",
                table: "CompanyPackageBookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SenderPaymentIdentifier",
                table: "CompanyPackageBookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPackageBookings_BookingReference",
                table: "CompanyPackageBookings",
                column: "BookingReference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyPackageBookings_BookingReference",
                table: "CompanyPackageBookings");

            migrationBuilder.DropColumn(
                name: "DepositPercentage",
                table: "GuideTourPackages");

            migrationBuilder.DropColumn(
                name: "InstaPayQrCodeUrl",
                table: "CompanyPaymentMethods");

            migrationBuilder.DropColumn(
                name: "BookingReference",
                table: "CompanyPackageBookings");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "CompanyPackageBookings");

            migrationBuilder.DropColumn(
                name: "IsDepositPayment",
                table: "CompanyPackageBookings");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "CompanyPackageBookings");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "CompanyPackageBookings");

            migrationBuilder.DropColumn(
                name: "SenderPaymentIdentifier",
                table: "CompanyPackageBookings");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "CompanyPackageBookings",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(320)",
                oldMaxLength: 320);
        }
    }
}
