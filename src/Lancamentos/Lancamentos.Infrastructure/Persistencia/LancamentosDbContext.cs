using Lancamentos.Application.Relatorios;
using Lancamentos.Domain.Entidades;
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
    public DbSet<ImportacaoExtrato> Importacoes => Set<ImportacaoExtrato>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

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
            e.HasOne<Conta>().WithMany().HasForeignKey(x => x.ContaId);

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
            e.HasIndex(x => x.Nome).IsUnique(); // normalizacao + unicidade evitam duplicatas
        });

        modelBuilder.Entity<Conta>(e =>
        {
            e.ToTable("Contas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Nome).IsUnique(); // sem contas duplicadas
        });

        modelBuilder.Entity<LancamentoRecorrente>(e =>
        {
            e.ToTable("Recorrencias");
            e.HasKey(x => x.Id);
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.Property(x => x.Valor).HasColumnType("decimal(18,2)");
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
            e.HasIndex(x => x.Nome).IsUnique(); // sem categorias duplicadas
        });

        modelBuilder.Entity<Orcamento>(e =>
        {
            e.ToTable("Orcamentos");
            e.HasKey(x => x.Id);
            e.Property(x => x.ValorLimite).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.CategoriaId).IsUnique(); // um teto por categoria
            e.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId);
        });

        modelBuilder.Entity<ImportacaoExtrato>(e =>
        {
            e.ToTable("Importacoes");
            e.HasKey(x => x.Id);
            e.Property(x => x.NomeArquivo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Erro).HasMaxLength(1000);
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

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Payload).IsRequired();
            e.HasIndex(x => x.ProcessadoEm); // o publicador consulta por pendentes (ProcessadoEm IS NULL)
        });
    }
}