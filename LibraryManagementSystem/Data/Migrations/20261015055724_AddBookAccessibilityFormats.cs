using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Data.Migrations
{
    public partial class AddBookAccessibilityFormats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAudioBook",
                table: "Items",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLargePrint",
                table: "Items",
                type: "bit",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [Items]
                SET [IsAudioBook] = 0,
                    [IsLargePrint] = 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAudioBook",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsLargePrint",
                table: "Items");
        }
    }
}