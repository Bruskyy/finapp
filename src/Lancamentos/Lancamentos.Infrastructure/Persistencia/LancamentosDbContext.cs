using Lancamentos.Application.Relatorios;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Importacao;
using Lancamentos.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Persistencia;

public class LancamentosDbContext : DbContext
{
    public LancamentosDbContext(DbContextOptions<LancamentosDbContext> options) : base(options) { }

    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<LancamentoRecorrente> Recorrencias => Set<LancamentoRecorrente>();
    public DbSet<Objetivo> Objetivos => Set<Objetivo>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<CompraParcelada> ComprasParceladas => Set<CompraParcelada>();
    public DbSet<ImportacaoExtrato> Importacoes => Set<ImportacaoExtrato>();
    public DbSet<ExtratoArquivo> ExtratosArquivos => Set<ExtratoArquivo>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ResumoSemanalGerado> ResumosSemanaisGerados => Set<ResumoSemanalGerado>();
    public DbSet<AlertaOrcamentoEnviado> AlertasOrcamentoEnviados => Set<AlertaOrcamentoEnviado>();
    public DbSet<AlertaRecorrenciaEnviado> AlertasRecorrenciaEnviados => Set<AlertaRecorrenciaEnviado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lancamento>(e =>
        {
            e.ToTable("Lancamentos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.Property(x => x.Valor).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.Data); // consultas por período são o acesso mais comum
            e.HasIndex(x => x.ContaId); // saldo por conta agrupa por ContaId
            e.HasIndex(x => x.UsuarioId); // todo endpoint de leitura filtra por dono
            // a fatura do cartão é SUM por (ContaId, Competencia) - índice composto dedicado
            e.HasIndex(x => new { x.ContaId, x.Competencia });
            e.HasOne<Conta>().WithMany().HasForeignKey(x => x.ContaId);
            // NoAction: Lancamento já cascateia de Conta - um segundo caminho de
            // cascade (Conta -> CompraParcelada -> Lancamento) é rejeitado pelo
            // SQL Server ("multiple cascade paths"); a exclusão da compra-mãe
            // remove as parcelas explicitamente no endpoint.
            e.HasOne<CompraParcelada>().WithMany().HasForeignKey(x => x.CompraParceladaId).OnDelete(DeleteBehavior.NoAction);

            // N:N com tabela de juncao implicita (LancamentoTags) — skip navigation
            e.HasMany(x => x.Tags)
                .WithMany()
                .UsingEntity("LancamentoTags");
        });

        modelBuilder.Entity<Tag>(e =>
        {
            e.ToTable("Tags");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(60).IsRequired();
            // unicidade por usuário, não mais global - dois usuários podem ter
            // cada um a sua tag "#viagem" sem colidir.
            e.HasIndex(x => new { x.UsuarioId, x.Nome }).IsUnique();
        });

        modelBuilder.Entity<Conta>(e =>
        {
            e.ToTable("Contas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            e.Property(x => x.Limite).HasColumnType("decimal(18,2)");
            // idem: unicidade por usuário (dois usuários podem ter "Carteira").
            e.HasIndex(x => new { x.UsuarioId, x.Nome }).IsUnique();
        });

        modelBuilder.Entity<CompraParcelada>(e =>
        {
            e.ToTable("ComprasParceladas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.Property(x => x.ValorTotal).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.UsuarioId);
            // NoAction pelos mesmos caminhos múltiplos de cascade do SQL Server.
            e.HasOne<Conta>().WithMany().HasForeignKey(x => x.ContaId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<LancamentoRecorrente>(e =>
        {
            e.ToTable("Recorrencias");
            e.HasKey(x => x.Id);
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.Property(x => x.Valor).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.UsuarioId);
            e.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId);
            e.HasOne<Conta>().WithMany().HasForeignKey(x => x.ContaId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Objetivo>(e =>
        {
            e.ToTable("Objetivos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.ValorAlvo).HasColumnType("decimal(18,2)");
            e.Property(x => x.ValorAcumulado).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.UsuarioId);
        });

        modelBuilder.Entity<RecorrenciaExecucao>(e =>
        {
            e.ToTable("RecorrenciaExecucoes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Competencia).HasMaxLength(7).IsRequired(); // "2026-07"
            // idempotencia do worker: uma materializacao por recorrencia por mes
            e.HasIndex(x => new { x.RecorrenciaId, x.Competencia }).IsUnique();
        });

        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("Categorias");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            // unicidade por usuário - NULL (categoria global) não colide entre
            // si no SQL Server (cada NULL é distinto pra fins de unique index),
            // então os defaults seedados continuam únicos por si só.
            e.HasIndex(x => new { x.UsuarioId, x.Nome }).IsUnique();
        });

        modelBuilder.Entity<Orcamento>(e =>
        {
            e.ToTable("Orcamentos");
            e.HasKey(x => x.Id);
            e.Property(x => x.ValorLimite).HasColumnType("decimal(18,2)");
            // um teto por categoria POR USUÁRIO, não mais global.
            e.HasIndex(x => new { x.UsuarioId, x.CategoriaId }).IsUnique();
            e.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId);
        });

        modelBuilder.Entity<ImportacaoExtrato>(e =>
        {
            e.ToTable("Importacoes");
            e.HasKey(x => x.Id);
            e.Property(x => x.NomeArquivo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Erro).HasMaxLength(1000);
            e.HasIndex(x => x.UsuarioId);
            e.Ignore(x => x.ChaveS3);
            e.Ignore(x => x.JaFoiProcessada);
        });

        modelBuilder.Entity<GastoPorCategoria>(e =>
        {
            e.HasNoKey();
            e.ToView(null); // resultado de procedure (SqlQuery), não é tabela nem view mapeada diretamente
            e.Property(x => x.TotalGasto).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ResumoMensal>(e =>
        {
            e.HasNoKey();
            e.ToView(null); // lido via SqlQuery sobre vw_ResumoMensal
            e.Property(x => x.ValorTotal).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<SaldoPorConta>(e =>
        {
            e.HasNoKey();
            e.ToView(null); // lido via SqlQuery sobre vw_SaldoPorConta
            e.Property(x => x.Saldo).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ExtratoArquivo>(e =>
        {
            e.ToTable("ExtratosArquivos");
            // A chave já é única por construção (derivada do Id da importação,
            // ver ImportacaoExtrato.ChaveS3) - serve direto de PK.
            e.HasKey(x => x.Chave);
            e.Property(x => x.Chave).HasMaxLength(100);
            e.Property(x => x.Conteudo).IsRequired(); // nvarchar(max): o endpoint limita a 1 MB
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Payload).IsRequired();
            e.HasIndex(x => new { x.Canal, x.ProcessadoEm }); // cada publicador consulta pendentes do próprio canal
        });

        modelBuilder.Entity<ResumoSemanalGerado>(e =>
        {
            e.ToTable("ResumosSemanaisGerados");
            e.HasKey(x => x.UsuarioId); // uma linha por usuário (upsert), não histórico
        });

        modelBuilder.Entity<AlertaOrcamentoEnviado>(e =>
        {
            e.ToTable("AlertasOrcamentoEnviados");
            e.HasKey(x => x.Id);
            e.Property(x => x.Competencia).HasMaxLength(7).IsRequired(); // "2026-07"
            // idempotencia: um alerta por orcamento, por competencia, por limiar (80/100)
            e.HasIndex(x => new { x.OrcamentoId, x.Competencia, x.Limiar }).IsUnique();
        });

        modelBuilder.Entity<AlertaRecorrenciaEnviado>(e =>
        {
            e.ToTable("AlertasRecorrenciaEnviados");
            e.HasKey(x => x.Id);
            e.Property(x => x.Competencia).HasMaxLength(7).IsRequired();
            // idempotencia: um alerta por recorrencia por competencia de vencimento
            e.HasIndex(x => new { x.RecorrenciaId, x.Competencia }).IsUnique();
        });
    }
}