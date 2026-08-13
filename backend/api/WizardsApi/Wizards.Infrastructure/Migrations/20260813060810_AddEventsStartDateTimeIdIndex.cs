using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wizards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsStartDateTimeIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_events_StartDateTime_Id",
                table: "events",
                columns: new[] { "StartDateTime", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_events_StartDateTime_Id",
                table: "events");
        }
    }
}
