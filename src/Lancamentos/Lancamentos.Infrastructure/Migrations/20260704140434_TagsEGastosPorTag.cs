using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TagsEGastosPorTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LancamentoTags",
                columns: table => new
                {
                    LancamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LancamentoTags", x => new { x.LancamentoId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_LancamentoTags_Lancamentos_LancamentoId",
                        column: x => x.LancamentoId,
                        principalTable: "Lancamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LancamentoTags_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LancamentoTags_TagsId",
                table: "LancamentoTags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Nome",
                table: "Tags",
                column: "Nome",
                unique: true);

            // relatório por tag: mesmo padrão de procedure nativa da Etapa 1
            migrationBuilder.Sql(@"
CREATE PROCEDURE sp_GastosPorTag
    @Inicio DATETIME2,
    @Fim    DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        t.Id          AS TagId,
        t.Nome        AS Tag,
        SUM(l.Valor)  AS TotalGasto,
        COUNT(*)      AS Quantidade
    FROM Lancamentos l
    INNER JOIN LancamentoTags lt ON lt.LancamentoId = l.Id
    INNER JOIN Tags t ON t.Id = lt.TagsId
    WHERE l.Tipo = 2 AND l.Data >= @Inicio AND l.Data <= @Fim
    GROUP BY t.Id, t.Nome
    ORDER BY TotalGasto DESC;
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE sp_GastosPorTag;");

            migrationBuilder.DropTable(
                name: "LancamentoTags");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
