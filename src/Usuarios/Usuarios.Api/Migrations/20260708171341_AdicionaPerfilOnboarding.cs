using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Usuarios.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaPerfilOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaiorDificuldade",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaiorObjetivo",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MomentoDeVida",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeObjetivoPersonalizado",
                table: "Usuarios",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnboardingConcluido",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorAlvoObjetivo",
                table: "Usuarios",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorMensalDesejado",
                table: "Usuarios",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaiorDificuldade",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MaiorObjetivo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MomentoDeVida",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NomeObjetivoPersonalizado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "OnboardingConcluido",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ValorAlvoObjetivo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ValorMensalDesejado",
                table: "Usuarios");
        }
    }
}
