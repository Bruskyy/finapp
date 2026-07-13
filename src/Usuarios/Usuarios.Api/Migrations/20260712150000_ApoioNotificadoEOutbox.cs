using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Usuarios.Api.Persistencia;

#nullable disable

namespace Usuarios.Api.Migrations
{
    /// <summary>
    /// Convite de apoio (BACKLOG-PRODUTO.md, Sprint 7): ApoiosNotificados
    /// (cooldown upsert por usuário) + OutboxMessages (primeira vez em
    /// Usuarios.Api). Migration escrita à mão (ambiente sem dotnet ef) - sem
    /// arquivo Designer, atributos na própria classe (mesmo padrão adotado
    /// nas migrations do cartão de crédito em Lancamentos).
    /// </summary>
    [DbContext(typeof(UsuariosDbContext))]
    [Migration("20260712150000_ApoioNotificadoEOutbox")]
    public partial class ApoioNotificadoEOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApoiosNotificados",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UltimoEnvioEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApoiosNotificados", x => x.UsuarioId);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ApoiosNotificados");
            migrationBuilder.DropTable(name: "OutboxMessages");
        }
    }
}
