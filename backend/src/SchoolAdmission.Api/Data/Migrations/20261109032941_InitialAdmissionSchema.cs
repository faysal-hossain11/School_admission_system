using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAdmissionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionClasses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TotalSeats = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionClasses", x => x.Id);
                    table.CheckConstraint("CK_AdmissionClasses_TotalSeats_Positive", "\"TotalSeats\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "AdmissionHolds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AdmissionClassId = table.Column<string>(type: "text", nullable: false),
                    StudentRegistrationId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionHolds", x => x.Id);
                    table.CheckConstraint("CK_AdmissionHolds_ExpirationAfterCreation", "\"ExpiresAt\" > \"CreatedAt\"");
                    table.CheckConstraint("CK_AdmissionHolds_Status", "\"Status\" IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_AdmissionHolds_AdmissionClasses_AdmissionClassId",
                        column: x => x.AdmissionClassId,
                        principalTable: "AdmissionClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionHolds_AdmissionClassId_Status",
                table: "AdmissionHolds",
                columns: new[] { "AdmissionClassId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionHolds_AdmissionClassId_StudentRegistrationId",
                table: "AdmissionHolds",
                columns: new[] { "AdmissionClassId", "StudentRegistrationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionHolds");

            migrationBuilder.DropTable(
                name: "AdmissionClasses");
        }
    }
}
