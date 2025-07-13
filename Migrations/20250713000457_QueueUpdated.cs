using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QobuzDiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class QueueUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SongQueue",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<long>(
                name: "Duration",
                table: "DownloadedTracks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddForeignKey(
                name: "FK_SongQueue_DownloadedTracks_Id",
                table: "SongQueue",
                column: "Id",
                principalTable: "DownloadedTracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SongQueue_DownloadedTracks_Id",
                table: "SongQueue");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "DownloadedTracks");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SongQueue",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);
        }
    }
}
