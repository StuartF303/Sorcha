using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sorcha.Wallet.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayConfigJson",
                schema: "wallet",
                table: "Credentials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPresentations",
                schema: "wallet",
                table: "Credentials",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PresentationCount",
                schema: "wallet",
                table: "Credentials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatusListIndex",
                schema: "wallet",
                table: "Credentials",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusListUrl",
                schema: "wallet",
                table: "Credentials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsagePolicy",
                schema: "wallet",
                table: "Credentials",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayConfigJson",
                schema: "wallet",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "MaxPresentations",
                schema: "wallet",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "PresentationCount",
                schema: "wallet",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "StatusListIndex",
                schema: "wallet",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "StatusListUrl",
                schema: "wallet",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "UsagePolicy",
                schema: "wallet",
                table: "Credentials");
        }
    }
}
