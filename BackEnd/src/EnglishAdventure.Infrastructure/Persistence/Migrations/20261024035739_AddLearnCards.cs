using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hand-edited: words that already exist become learn cards, as new words do (S3.7, default: all words).
            // The model has no default value, so the app always writes the column itself.
            migrationBuilder.AddColumn<bool>(
                name: "ShowInLearnStep",
                table: "content_lesson_words",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowInLearnStep",
                table: "content_lesson_words");
        }
    }
}
