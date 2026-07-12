using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lancamentos.Infrastructure.Migrations
{
    /// <summary>
    /// Cartão de crédito, PR 2 (ITEM-CARTAO-CREDITO.md): a fatura é DERIVADA
    /// por competência via view SQL (nunca materializada), e o saldo por
    /// conta passa a excluir cartões (saldo de cartão não é dinheiro que
    /// você tem - a visão certa dele é fatura + limite disponível).
    /// Views só existem em SQL puro - nenhuma mudança de modelo/snapshot.
    /// </summary>
    [DbContext(typeof(LancamentosDbContext))]
    [Migration("20260711190000_ViewFaturaEExclusaoCartaoDoSaldo")]
    public partial class ViewFaturaEExclusaoCartaoDoSaldo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Receita com competência (estorno lançado no cartão) abate a
            // fatura; pagamento (transferência) NÃO tem competência e por isso
            // não entra aqui - ele abate o saldo devedor total, não uma fatura
            // específica.
            migrationBuilder.Sql(@"
CREATE VIEW vw_FaturaPorCompetencia AS
SELECT
    l.ContaId     AS ContaId,
    c.UsuarioId   AS UsuarioId,
    l.Competencia AS Competencia,
    SUM(CASE WHEN l.Tipo = 2 THEN l.Valor ELSE -l.Valor END) AS TotalCompras,
    COUNT(*)      AS QuantidadeItens
FROM Lancamentos l
JOIN Contas c ON c.Id = l.ContaId
WHERE l.Competencia IS NOT NULL
GROUP BY l.ContaId, c.UsuarioId, l.Competencia;
");

            migrationBuilder.Sql("DROP VIEW vw_SaldoPorConta;");
            migrationBuilder.Sql(@"
CREATE VIEW vw_SaldoPorConta AS
SELECT
    c.Id        AS ContaId,
    c.Nome      AS Conta,
    c.UsuarioId AS UsuarioId,
    ISNULL(SUM(CASE WHEN l.Tipo = 1 THEN l.Valor ELSE -l.Valor END), 0) AS Saldo
FROM Contas c
LEFT JOIN Lancamentos l ON l.ContaId = c.Id
WHERE c.Tipo <> 2
GROUP BY c.Id, c.Nome, c.UsuarioId;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW vw_FaturaPorCompetencia;");

            migrationBuilder.Sql("DROP VIEW vw_SaldoPorConta;");
            migrationBuilder.Sql(@"
CREATE VIEW vw_SaldoPorConta AS
SELECT
    c.Id        AS ContaId,
    c.Nome      AS Conta,
    c.UsuarioId AS UsuarioId,
    ISNULL(SUM(CASE WHEN l.Tipo = 1 THEN l.Valor ELSE -l.Valor END), 0) AS Saldo
FROM Contas c
LEFT JOIN Lancamentos l ON l.ContaId = c.Id
GROUP BY c.Id, c.Nome, c.UsuarioId;
");
        }
    }
}
