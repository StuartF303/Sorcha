using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sorcha.Wallet.Core.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAddressColumnsToText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ParentWalletAddress",
                schema: "wallet",
                table: "WalletTransactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                schema: "wallet",
                table: "Wallets",
                type: "text",
                nullable: false,
                comment: "Wallet address in Bech32m format (ws1...). Variable length by algorithm: ED25519 ~66 chars, NISTP256 ~107 chars, RSA4096 ~700 chars.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "ParentWalletAddress",
                schema: "wallet",
                table: "WalletAddresses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                schema: "wallet",
                table: "WalletAddresses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "ParentWalletAddress",
                schema: "wallet",
                table: "WalletAccess",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ParentWalletAddress",
                schema: "wallet",
                table: "WalletTransactions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                schema: "wallet",
                table: "Wallets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Wallet address in Bech32m format (ws1...). Variable length by algorithm: ED25519 ~66 chars, NISTP256 ~107 chars, RSA4096 ~700 chars.");

            migrationBuilder.AlterColumn<string>(
                name: "ParentWalletAddress",
                schema: "wallet",
                table: "WalletAddresses",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                schema: "wallet",
                table: "WalletAddresses",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ParentWalletAddress",
                schema: "wallet",
                table: "WalletAccess",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
