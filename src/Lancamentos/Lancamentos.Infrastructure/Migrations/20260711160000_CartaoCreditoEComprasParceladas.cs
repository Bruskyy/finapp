using System;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <summary>
    /// Cartão de crédito (ITEM-CARTAO-CREDITO.md): Conta ganha o
    /// discriminador Tipo + campos de cartão; Lancamento ganha Competencia
    /// (mês da fatura, derivada - a fatura nunca é materializada) e o vínculo
    /// de parcela; tabela nova ComprasParceladas (compra-mãe do parcelamento).
    /// Migration escrita à mão (ambiente remoto sem dotnet ef) - por isso os
    /// atributos [DbContext]/[Migration] estão na própria classe, sem
    /// arquivo Designer.
    /// </summary>
    [DbContext(typeof(LancamentosDbContext))]
    [Migration("20260711160000_CartaoCreditoEComprasParceladas")]
    public partial class CartaoCreditoEComprasParceladas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Contas",
                type: "int",
                nullable: false,
                defaultValue: 1); // TipoConta.Corrente pra todas as contas existentes

            migrationBuilder.AddColumn<decimal>(
                name: "Limite",
                table: "Contas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiaFechamento",
                table: "Contas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiaVencimento",
                table: "Contas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Competencia",
                table: "Lancamentos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompraParceladaId",
                table: "Lancamentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroParcela",
                table: "Lancamentos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComprasParceladas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NumeroParcelas = table.Column<int>(type: "int", nullable: false),
                    ContaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataCompra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprasParceladas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprasParceladas_Contas_ContaId",
                        column: x => x.ContaId,
                        principalTable: "Contas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComprasParceladas_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprasParceladas_UsuarioId",
                table: "ComprasParceladas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasParceladas_ContaId",
                table: "ComprasParceladas",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprasParceladas_CategoriaId",
                table: "ComprasParceladas",
                column: "CategoriaId");

            // a fatura é SUM por (ContaId, Competencia) - índice dedicado
            migrationBuilder.CreateIndex(
                name: "IX_Lancamentos_ContaId_Competencia",
                table: "Lancamentos",
                columns: new[] { "ContaId", "Competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_Lancamentos_CompraParceladaId",
                table: "Lancamentos",
                column: "CompraParceladaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lancamentos_ComprasParceladas_CompraParceladaId",
                table: "Lancamentos",
                column: "CompraParceladaId",
                principalTable: "ComprasParceladas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lancamentos_ComprasParceladas_CompraParceladaId",
                table: "Lancamentos");

            migrationBuilder.DropTable(
                name: "ComprasParceladas");

            migrationBuilder.DropIndex(
                name: "IX_Lancamentos_ContaId_Competencia",
                table: "Lancamentos");

            migrationBuilder.DropIndex(
                name: "IX_Lancamentos_CompraParceladaId",
                table: "Lancamentos");

            migrationBuilder.DropColumn(name: "Competencia", table: "Lancamentos");
            migrationBuilder.DropColumn(name: "CompraParceladaId", table: "Lancamentos");
            migrationBuilder.DropColumn(name: "NumeroParcela", table: "Lancamentos");

            migrationBuilder.DropColumn(name: "Tipo", table: "Contas");
            migrationBuilder.DropColumn(name: "Limite", table: "Contas");
            migrationBuilder.DropColumn(name: "DiaFechamento", table: "Contas");
            migrationBuilder.DropColumn(name: "DiaVencimento", table: "Contas");
        }
    }
}
