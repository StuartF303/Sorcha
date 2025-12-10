using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sorcha.Tenant.Service.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IdentityId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Details = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationPermissionConfigurations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBlockchains = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    CanCreateBlockchain = table.Column<bool>(type: "boolean", nullable: false),
                    CanPublishBlueprint = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationPermissionConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subdomain = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatorIdentityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Branding_LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Branding_PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Branding_SecondaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Branding_CompanyTagline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicIdentities",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PassKeyCredentialId = table.Column<byte[]>(type: "bytea", nullable: false),
                    PublicKeyCose = table.Column<byte[]>(type: "bytea", nullable: false),
                    SignatureCounter = table.Column<int>(type: "integer", nullable: false),
                    DeviceType = table.Column<string>(type: "text", nullable: true),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicIdentities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrincipals",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientSecretEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    Scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrincipals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserIdentities",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalIdpUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Roles = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserIdentities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdentityProviderConfigurations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderType = table.Column<string>(type: "text", nullable: false),
                    IssuerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ClientSecretEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    Scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    AuthorizationEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TokenEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityProviderConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityProviderConfigurations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "public",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_EventType",
                schema: "public",
                table: "AuditLogEntries",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_IdentityId",
                schema: "public",
                table: "AuditLogEntries",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_OrganizationId",
                schema: "public",
                table: "AuditLogEntries",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_Timestamp",
                schema: "public",
                table: "AuditLogEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviderConfigurations_OrganizationId",
                schema: "public",
                table: "IdentityProviderConfigurations",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviderConfigurations_ProviderType",
                schema: "public",
                table: "IdentityProviderConfigurations",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationPermissionConfigurations_OrganizationId",
                schema: "public",
                table: "OrganizationPermissionConfigurations",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Status",
                schema: "public",
                table: "Organizations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Subdomain",
                schema: "public",
                table: "Organizations",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicIdentities_PassKeyCredentialId",
                schema: "public",
                table: "PublicIdentities",
                column: "PassKeyCredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipals_ClientId",
                schema: "public",
                table: "ServicePrincipals",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipals_ServiceName",
                schema: "public",
                table: "ServicePrincipals",
                column: "ServiceName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentities_Email",
                schema: "public",
                table: "UserIdentities",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentities_ExternalIdpUserId",
                schema: "public",
                table: "UserIdentities",
                column: "ExternalIdpUserId",
                unique: true,
                filter: "\"ExternalIdpUserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentities_OrganizationId",
                schema: "public",
                table: "UserIdentities",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentities_Status",
                schema: "public",
                table: "UserIdentities",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogEntries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "IdentityProviderConfigurations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OrganizationPermissionConfigurations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PublicIdentities",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ServicePrincipals",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserIdentities",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Organizations",
                schema: "public");
        }
    }
}
