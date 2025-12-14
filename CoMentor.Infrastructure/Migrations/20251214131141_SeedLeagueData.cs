using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoMentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedLeagueData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Leagues",
                columns: new[] { "Id", "Icon", "LeagueColor", "MaxXp", "MinXp", "Name", "RankOrder" },
                values: new object[,]
                {
                    { 1, "🥉", "#CD7F32", 999, 0, "Bronz", 1 },
                    { 2, "🥈", "#C0C0C0", 4999, 1000, "Gümüş", 2 },
                    { 3, "🥇", "#FFD700", 14999, 5000, "Altın", 3 },
                    { 4, "💎", "#E5E4E2", 49999, 15000, "Platin", 4 },
                    { 5, "👑", "#B9F2FF", null, 50000, "Elmas", 5 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Leagues",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Leagues",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Leagues",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Leagues",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Leagues",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
