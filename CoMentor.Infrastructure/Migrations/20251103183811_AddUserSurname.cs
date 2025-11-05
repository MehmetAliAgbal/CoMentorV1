using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoMentor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSurname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Surname",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Surname",
                table: "Users");
        }
    }
}
