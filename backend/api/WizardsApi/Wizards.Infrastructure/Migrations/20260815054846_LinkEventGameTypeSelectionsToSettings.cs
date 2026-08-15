using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wizards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkEventGameTypeSelectionsToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameTypeSettingId",
                table: "event_game_type_selections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE event_game_type_selections
                SET GameTypeSettingId = (
                    SELECT s.Id
                    FROM game_type_settings AS s
                    INNER JOIN events AS e ON e.GameTypeId = s.GameTypeId
                    WHERE e.Id = event_game_type_selections.EventId
                        AND s.Key = event_game_type_selections.Key);
                """);

            migrationBuilder.Sql(
                "DELETE FROM event_game_type_selections WHERE GameTypeSettingId IS NULL;");

            migrationBuilder.DropIndex(
                name: "IX_event_game_type_selections_EventId_Key",
                table: "event_game_type_selections");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "event_game_type_selections");

            migrationBuilder.AlterColumn<int>(
                name: "GameTypeSettingId",
                table: "event_game_type_selections",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_game_type_selections_EventId_GameTypeSettingId",
                table: "event_game_type_selections",
                columns: new[] { "EventId", "GameTypeSettingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_game_type_selections_GameTypeSettingId",
                table: "event_game_type_selections",
                column: "GameTypeSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_event_game_type_selections_game_type_settings_GameTypeSettingId",
                table: "event_game_type_selections",
                column: "GameTypeSettingId",
                principalTable: "game_type_settings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Lossy: selections whose key named no setting were deleted on the way up and do not come back.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_event_game_type_selections_game_type_settings_GameTypeSettingId",
                table: "event_game_type_selections");

            migrationBuilder.DropIndex(
                name: "IX_event_game_type_selections_EventId_GameTypeSettingId",
                table: "event_game_type_selections");

            migrationBuilder.DropIndex(
                name: "IX_event_game_type_selections_GameTypeSettingId",
                table: "event_game_type_selections");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "event_game_type_selections",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.Sql(
                """
                UPDATE event_game_type_selections
                SET Key = COALESCE(
                    (SELECT s.Key
                     FROM game_type_settings AS s
                     WHERE s.Id = event_game_type_selections.GameTypeSettingId),
                    '');
                """);

            migrationBuilder.DropColumn(
                name: "GameTypeSettingId",
                table: "event_game_type_selections");

            migrationBuilder.CreateIndex(
                name: "IX_event_game_type_selections_EventId_Key",
                table: "event_game_type_selections",
                columns: new[] { "EventId", "Key" },
                unique: true);
        }
    }
}
