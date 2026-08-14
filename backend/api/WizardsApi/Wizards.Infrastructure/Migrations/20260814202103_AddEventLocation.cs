using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wizards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Events created before this column existed carry no location, and the entity treats an
            // empty one as invalid, so they are backfilled with a placeholder rather than an empty
            // string. The column default only ever serves that backfill: every write states a location,
            // because the entity refuses to be created without one.
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "events",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "To be announced");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "events");
        }
    }
}
