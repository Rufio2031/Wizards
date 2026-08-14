using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wizards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTypeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_type_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    MinValue = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxValue = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultValue = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_type_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_game_type_settings_game_types_GameTypeId",
                        column: x => x.GameTypeId,
                        principalTable: "game_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_type_setting_options",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameTypeSettingId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_type_setting_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_game_type_setting_options_game_type_settings_GameTypeSettingId",
                        column: x => x.GameTypeSettingId,
                        principalTable: "game_type_settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_type_setting_options_GameTypeSettingId_Value",
                table: "game_type_setting_options",
                columns: new[] { "GameTypeSettingId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_type_settings_GameTypeId_Key",
                table: "game_type_settings",
                columns: new[] { "GameTypeId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_type_setting_options");

            migrationBuilder.DropTable(
                name: "game_type_settings");
        }
    }
}
