using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MontenegroFiscalization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalizationAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    IIC = table.Column<string>(type: "TEXT", nullable: true),
                    FIC = table.Column<string>(type: "TEXT", nullable: true),
                    UUID = table.Column<string>(type: "TEXT", nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    RequestXml = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseXml = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BusinessUnit = table.Column<string>(type: "TEXT", nullable: true),
                    TCRCode = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalizationAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    TenantName = table.Column<string>(type: "TEXT", nullable: false),
                    TIN = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BusinessUnitCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TCRCode = table.Column<string>(type: "TEXT", nullable: false),
                    SoftwareCode = table.Column<string>(type: "TEXT", nullable: false),
                    MaintainerCode = table.Column<string>(type: "TEXT", nullable: false),
                    IsInVAT = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CertificateVaultPath = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Key",
                table: "ApiKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_TenantId",
                table: "ApiKeys",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalizationAudits_CreatedAt",
                table: "FiscalizationAudits",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalizationAudits_FIC",
                table: "FiscalizationAudits",
                column: "FIC");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalizationAudits_IIC",
                table: "FiscalizationAudits",
                column: "IIC");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalizationAudits_InvoiceNumber",
                table: "FiscalizationAudits",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalizationAudits_TenantId",
                table: "FiscalizationAudits",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConfigurations_TenantId",
                table: "TenantConfigurations",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "FiscalizationAudits");

            migrationBuilder.DropTable(
                name: "TenantConfigurations");
        }
    }
}
