using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicQueue.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreSpecializations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "Id", "FullName", "IsActive" },
                values: new object[,]
                {
                    { 4, "ד\"ר רונית שמש", true },
                    { 5, "ד\"ר אורי בן דוד", true },
                    { 6, "ד\"ר נועה גולן", true },
                    { 7, "ד\"ר איתן מזרחי", true },
                    { 8, "ד\"ר תמר ברק", true }
                });

            migrationBuilder.InsertData(
                table: "Specializations",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 4, "רפואת עור" },
                    { 5, "אורתופדיה" },
                    { 6, "קרדיולוגיה" },
                    { 7, "אף אוזן גרון" },
                    { 8, "רפואת שיניים" }
                });

            migrationBuilder.InsertData(
                table: "DoctorSpecializations",
                columns: new[] { "DoctorId", "SpecializationId" },
                values: new object[,]
                {
                    { 4, 4 },
                    { 5, 5 },
                    { 6, 6 },
                    { 7, 7 },
                    { 8, 8 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DoctorSpecializations",
                keyColumns: new[] { "DoctorId", "SpecializationId" },
                keyValues: new object[] { 4, 4 });

            migrationBuilder.DeleteData(
                table: "DoctorSpecializations",
                keyColumns: new[] { "DoctorId", "SpecializationId" },
                keyValues: new object[] { 5, 5 });

            migrationBuilder.DeleteData(
                table: "DoctorSpecializations",
                keyColumns: new[] { "DoctorId", "SpecializationId" },
                keyValues: new object[] { 6, 6 });

            migrationBuilder.DeleteData(
                table: "DoctorSpecializations",
                keyColumns: new[] { "DoctorId", "SpecializationId" },
                keyValues: new object[] { 7, 7 });

            migrationBuilder.DeleteData(
                table: "DoctorSpecializations",
                keyColumns: new[] { "DoctorId", "SpecializationId" },
                keyValues: new object[] { 8, 8 });

            migrationBuilder.DeleteData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Specializations",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Specializations",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Specializations",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Specializations",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Specializations",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
