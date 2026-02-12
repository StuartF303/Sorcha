// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sorcha.Wallet.Core.Migrations;

/// <inheritdoc />
public partial class AddCredentialStore : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Credentials",
            schema: "wallet",
            columns: table => new
            {
                Id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                IssuerDid = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                SubjectDid = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ClaimsJson = table.Column<string>(type: "jsonb", nullable: false),
                IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RawToken = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                IssuanceTxId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                IssuanceBlueprintId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                WalletAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Credentials", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Credentials_WalletAddress",
            schema: "wallet",
            table: "Credentials",
            column: "WalletAddress");

        migrationBuilder.CreateIndex(
            name: "IX_Credentials_Type",
            schema: "wallet",
            table: "Credentials",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_Credentials_Wallet_Type",
            schema: "wallet",
            table: "Credentials",
            columns: new[] { "WalletAddress", "Type" });

        migrationBuilder.CreateIndex(
            name: "IX_Credentials_Status",
            schema: "wallet",
            table: "Credentials",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Credentials_IssuerDid",
            schema: "wallet",
            table: "Credentials",
            column: "IssuerDid");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Credentials",
            schema: "wallet");
    }
}
