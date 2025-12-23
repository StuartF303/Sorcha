using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sorcha.Tenant.Service.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBrandingToJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Branding_CompanyTagline",
                schema: "public",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Branding_LogoUrl",
                schema: "public",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Branding_PrimaryColor",
                schema: "public",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Branding_SecondaryColor",
                schema: "public",
                table: "Organizations");

            migrationBuilder.AddColumn<string>(
                name: "Branding",
                schema: "public",
                table: "Organizations",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Branding",
                schema: "public",
                table: "Organizations");

            migrationBuilder.AddColumn<string>(
                name: "Branding_CompanyTagline",
                schema: "public",
                table: "Organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Branding_LogoUrl",
                schema: "public",
                table: "Organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Branding_PrimaryColor",
                schema: "public",
                table: "Organizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Branding_SecondaryColor",
                schema: "public",
                table: "Organizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
