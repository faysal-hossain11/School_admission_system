using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceActiveApplicationUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdmissionHolds_AdmissionClassId_StudentRegistrationId",
                table: "AdmissionHolds");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionHolds_AdmissionClassId_StudentRegistrationId",
                table: "AdmissionHolds",
                columns: new[] { "AdmissionClassId", "StudentRegistrationId" },
                unique: true,
                filter: "\"Status\" IN ('HELD', 'CONFIRMED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdmissionHolds_AdmissionClassId_StudentRegistrationId",
                table: "AdmissionHolds");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionHolds_AdmissionClassId_StudentRegistrationId",
                table: "AdmissionHolds",
                columns: new[] { "AdmissionClassId", "StudentRegistrationId" });
        }
    }
}
