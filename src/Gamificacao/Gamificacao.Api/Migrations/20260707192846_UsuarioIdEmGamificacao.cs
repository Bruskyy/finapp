using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gamificacao.Api.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioIdEmGamificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Resgates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "MovimentosMoedas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resgates_UsuarioId",
                table: "Resgates",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosMoedas_UsuarioId",
                table: "MovimentosMoedas",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resgates_UsuarioId",
                table: "Resgates");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosMoedas_UsuarioId",
                table: "MovimentosMoedas");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Resgates");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "MovimentosMoedas");
        }
    }
}
