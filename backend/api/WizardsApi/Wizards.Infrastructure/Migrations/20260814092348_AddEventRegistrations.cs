using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wizards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_registrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_registrations_events_EventId",
                        column: x => x.EventId,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_event_registrations_EventId",
                table: "event_registrations",
                column: "EventId");

            // The limit is a count across rows, which no column-level constraint can express, so it is
            // enforced by triggers that abort the statement instead. What they compare against is the
            // event's own limit, so lowering an event's limit lowers what it will accept. The standard
            // 30 stands in when the event cannot be read, which the foreign key should already have
            // ruled out, so that a hole in the cap is never the way that failure shows up.
            //
            // SQLite drops a table's triggers when the table is rebuilt, which is how EF applies most
            // schema changes on this provider. A later migration that alters event_registrations has to
            // recreate these.
            migrationBuilder.Sql(
                """
                CREATE TRIGGER event_registrations_limit_on_insert
                BEFORE INSERT ON event_registrations
                FOR EACH ROW
                WHEN (SELECT COUNT(*) FROM event_registrations WHERE EventId = NEW.EventId)
                     >= COALESCE((SELECT RegistrationLimit FROM events WHERE Id = NEW.EventId), 30)
                BEGIN
                    SELECT RAISE(ABORT, 'Event has reached its registration limit.');
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER event_registrations_limit_on_update
                BEFORE UPDATE OF EventId ON event_registrations
                FOR EACH ROW
                WHEN NEW.EventId <> OLD.EventId
                     AND (SELECT COUNT(*) FROM event_registrations WHERE EventId = NEW.EventId)
                         >= COALESCE((SELECT RegistrationLimit FROM events WHERE Id = NEW.EventId), 30)
                BEGIN
                    SELECT RAISE(ABORT, 'Event has reached its registration limit.');
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS event_registrations_limit_on_update;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS event_registrations_limit_on_insert;");

            migrationBuilder.DropTable(
                name: "event_registrations");
        }
    }
}
