using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCanalOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessadoEm",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<int>(
                name: "Canal",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Canal_ProcessadoEm",
                table: "OutboxMessages",
                columns: new[] { "Canal", "ProcessadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Canal_ProcessadoEm",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Canal",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessadoEm",
                table: "OutboxMessages",
                column: "ProcessadoEm");
        }
    }
}
