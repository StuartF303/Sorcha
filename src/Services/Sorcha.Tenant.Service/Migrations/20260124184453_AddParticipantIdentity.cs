using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sorcha.Tenant.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParticipantIdentities",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantIdentities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LinkedWalletAddresses",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    Algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedWalletAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkedWalletAddresses_ParticipantIdentities_ParticipantId",
                        column: x => x.ParticipantId,
                        principalSchema: "public",
                        principalTable: "ParticipantIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantAuditEntries",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActorId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OldValues = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    NewValues = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantAuditEntries_ParticipantIdentities_ParticipantId",
                        column: x => x.ParticipantId,
                        principalSchema: "public",
                        principalTable: "ParticipantIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WalletLinkChallenges",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Challenge = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletLinkChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletLinkChallenges_ParticipantIdentities_ParticipantId",
                        column: x => x.ParticipantId,
                        principalSchema: "public",
                        principalTable: "ParticipantIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletLink_Address",
                schema: "public",
                table: "LinkedWalletAddresses",
                column: "WalletAddress",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_WalletLink_Participant",
                schema: "public",
                table: "LinkedWalletAddresses",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_Audit_Actor_Time",
                schema: "public",
                table: "ParticipantAuditEntries",
                columns: new[] { "ActorId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Audit_Participant_Time",
                schema: "public",
                table: "ParticipantAuditEntries",
                columns: new[] { "ParticipantId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Participant_Org_Status",
                schema: "public",
                table: "ParticipantIdentities",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantIdentities_UserId",
                schema: "public",
                table: "ParticipantIdentities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Participant_User_Org",
                schema: "public",
                table: "ParticipantIdentities",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Challenge_Address_Status",
                schema: "public",
                table: "WalletLinkChallenges",
                columns: new[] { "WalletAddress", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Challenge_Participant_Status",
                schema: "public",
                table: "WalletLinkChallenges",
                columns: new[] { "ParticipantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinkedWalletAddresses",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ParticipantAuditEntries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "WalletLinkChallenges",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ParticipantIdentities",
                schema: "public");
        }
    }
}
