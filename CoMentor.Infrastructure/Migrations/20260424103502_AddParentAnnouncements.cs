using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoMentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParentAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Announcements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetAudience",
                table: "Announcements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_ParentId",
                table: "Announcements",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Parents_ParentId",
                table: "Announcements",
                column: "ParentId",
                principalTable: "Parents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Parents_ParentId",
                table: "Announcements");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_ParentId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "TargetAudience",
                table: "Announcements");
        }
    }
}
