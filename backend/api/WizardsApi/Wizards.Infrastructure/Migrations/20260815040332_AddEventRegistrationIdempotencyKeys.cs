using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wizards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRegistrationIdempotencyKeys : Migration
    {
        private const string CreateInsertLimitTrigger =
            """
            CREATE TRIGGER event_registrations_limit_on_insert
            BEFORE INSERT ON event_registrations
            FOR EACH ROW
            WHEN (SELECT COUNT(*) FROM event_registrations WHERE EventId = NEW.EventId)
                 >= COALESCE((SELECT RegistrationLimit FROM events WHERE Id = NEW.EventId), 30)
            BEGIN
                SELECT RAISE(ABORT, 'Event has reached its registration limit.');
            END;
            """;

        private const string CreateUpdateLimitTrigger =
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
            """;

        private const string DropInsertLimitTrigger =
            "DROP TRIGGER IF EXISTS event_registrations_limit_on_insert;";

        private const string DropUpdateLimitTrigger =
            "DROP TRIGGER IF EXISTS event_registrations_limit_on_update;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every stored registration predates the key, and the column's default would hand them all
            // the same one, which the unique index then refuses. Production reseeds from empty.
            migrationBuilder.Sql("DELETE FROM event_registrations;");

            // SQLite drops a table's triggers whenever the table is rebuilt, which is how EF applies
            // some schema changes on this provider. Dropping and recreating them around the change
            // leaves the limit enforced either way.
            migrationBuilder.Sql(DropUpdateLimitTrigger);
            migrationBuilder.Sql(DropInsertLimitTrigger);

            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "event_registrations",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_event_registrations_EventId_IdempotencyKey",
                table: "event_registrations",
                columns: new[] { "EventId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.Sql(CreateInsertLimitTrigger);
            migrationBuilder.Sql(CreateUpdateLimitTrigger);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DropUpdateLimitTrigger);
            migrationBuilder.Sql(DropInsertLimitTrigger);

            migrationBuilder.DropIndex(
                name: "IX_event_registrations_EventId_IdempotencyKey",
                table: "event_registrations");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "event_registrations");

            migrationBuilder.Sql(CreateInsertLimitTrigger);
            migrationBuilder.Sql(CreateUpdateLimitTrigger);
        }
    }
}
