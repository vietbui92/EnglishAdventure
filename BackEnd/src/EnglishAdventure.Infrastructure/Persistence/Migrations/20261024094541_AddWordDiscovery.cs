using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWordDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Movement",
                table: "content_lesson_words",
                type: "int",
                nullable: false,
                // Hand-edited: existing words stand still (WordMovement.Still = 1), not the undefined 0.
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SoundUrl",
                table: "content_lesson_words",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Movement",
                table: "content_lesson_words");

            migrationBuilder.DropColumn(
                name: "SoundUrl",
                table: "content_lesson_words");
        }
    }
}
