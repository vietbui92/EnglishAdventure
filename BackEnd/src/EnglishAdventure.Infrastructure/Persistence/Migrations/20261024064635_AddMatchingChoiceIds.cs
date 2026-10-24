using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishAdventure.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingChoiceIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hand-edited: every existing pair gets its own new identity (an empty Guid would give them all the same one).
            // The model has no default value, so the app always writes the column itself.
            migrationBuilder.AddColumn<Guid>(
                name: "ChoiceId",
                table: "content_matching_pairs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChoiceId",
                table: "content_matching_pairs");
        }
    }
}
