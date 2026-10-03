using Fayora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fayora.Infrastructure.Persistence.Migrations;

/// <summary>
/// Brings the physical HousingUnits table in line with the mapped optional
/// verification-document value object. Earlier SaaS migrations mapped this
/// column but did not create it.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261002210000_AddHousingUnitVerificationDocumentUrl")]
public partial class AddHousingUnitVerificationDocumentUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "VerificationDocumentUrl",
            table: "HousingUnits",
            type: "nvarchar(2048)",
            maxLength: 2048,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "VerificationDocumentUrl",
            table: "HousingUnits");
    }
}
