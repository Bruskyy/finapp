using Gamificacao.Api.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Gamificacao.Api.Persistencia;

public class GamificacaoDbContext : DbContext
{
    public GamificacaoDbContext(DbContextOptions<GamificacaoDbContext> options) : base(options) { }

    public DbSet<MovimentoMoedas> Movimentos => Set<MovimentoMoedas>();
    public DbSet<Resgate> Resgates => Set<Resgate>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Conquista> Conquistas => Set<Conquista>();
    public DbSet<UsuarioConquista> UsuariosConquistas => Set<UsuarioConquista>();
    public DbSet<ContadorConquista> ContadoresConquista => Set<ContadorConquista>();
    public DbSet<SequenciaUsuario> SequenciasUsuario => Set<SequenciaUsuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MovimentoMoedas>(e =>
        {
            e.ToTable("MovimentosMoedas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Motivo).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.EventId).IsUnique(); // idempotencia: um evento so gera um movimento
            e.HasIndex(x => x.UsuarioId);
        });

        modelBuilder.Entity<Resgate>(e =>
        {
            e.ToTable("Resgates");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UsuarioId);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Payload).IsRequired();
            e.HasIndex(x => x.ProcessadoEm);
        });

        modelBuilder.Entity<Conquista>(e =>
        {
            e.ToTable("Conquistas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Codigo).IsUnique();
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(300).IsRequired();
            e.Property(x => x.Icone).HasMaxLength(50).IsRequired();

            // catálogo fixo, mesmo padrão de seed das Categorias em
            // Lancamentos.Infrastructure (Guids memoráveis, um dígito
            // repetido por linha).
            e.HasData(
                new
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Codigo = Regras.ConquistaCodigos.PrimeiroSalario,
                    Nome = "Primeiro salário",
                    Descricao = "Registrou seu primeiro lançamento de salário.",
                    Icone = "cash-outline",
                },
                new
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Codigo = Regras.ConquistaCodigos.Lancamentos10,
                    Nome = "10 lançamentos",
                    Descricao = "Registrou 10 lançamentos no Cofrin.",
                    Icone = "receipt-outline",
                },
                new
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Codigo = Regras.ConquistaCodigos.Lancamentos100,
                    Nome = "100 lançamentos",
                    Descricao = "Registrou 100 lançamentos no Cofrin.",
                    Icone = "receipt-outline",
                },
                new
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Codigo = Regras.ConquistaCodigos.Lancamentos1000,
                    Nome = "1000 lançamentos",
                    Descricao = "Registrou 1000 lançamentos no Cofrin.",
                    Icone = "receipt-outline",
                },
                new
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Codigo = Regras.ConquistaCodigos.PrimeiraMetaConcluida,
                    Nome = "Primeira meta concluída",
                    Descricao = "Concluiu sua primeira meta de poupança.",
                    Icone = "trophy-outline",
                },
                new
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Codigo = Regras.ConquistaCodigos.MetasConcluidas5,
                    Nome = "5 metas concluídas",
                    Descricao = "Concluiu 5 metas de poupança.",
                    Icone = "trophy-outline",
                },
                // Roadmap Cofrin 1.0, Sprint 2 - catálogo de 6 para 15,
                // continuando o mesmo padrão de Guid memorável (dígito
                // hexadecimal repetido por linha, seguindo de onde 1-6 parou).
                new
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Codigo = Regras.ConquistaCodigos.Sequencia7,
                    Nome = "7 dias de sequência",
                    Descricao = "Usou o Cofrin por 7 dias seguidos.",
                    Icone = "flame-outline",
                },
                new
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Codigo = Regras.ConquistaCodigos.Sequencia30,
                    Nome = "30 dias de sequência",
                    Descricao = "Usou o Cofrin por 30 dias seguidos.",
                    Icone = "flame-outline",
                },
                new
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Codigo = Regras.ConquistaCodigos.Sequencia100,
                    Nome = "100 dias de sequência",
                    Descricao = "Usou o Cofrin por 100 dias seguidos.",
                    Icone = "flame-outline",
                },
                new
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Codigo = Regras.ConquistaCodigos.Sequencia365,
                    Nome = "365 dias de sequência",
                    Descricao = "Usou o Cofrin por 365 dias seguidos.",
                    Icone = "flame-outline",
                },
                new
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Codigo = Regras.ConquistaCodigos.Lancamentos1,
                    Nome = "Primeiro lançamento",
                    Descricao = "Registrou seu primeiro lançamento no Cofrin.",
                    Icone = "receipt-outline",
                },
                new
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Codigo = Regras.ConquistaCodigos.Lancamentos50,
                    Nome = "50 lançamentos",
                    Descricao = "Registrou 50 lançamentos no Cofrin.",
                    Icone = "receipt-outline",
                },
                new
                {
                    Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    Codigo = Regras.ConquistaCodigos.Lancamentos500,
                    Nome = "500 lançamentos",
                    Descricao = "Registrou 500 lançamentos no Cofrin.",
                    Icone = "receipt-outline",
                },
                new
                {
                    Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    Codigo = Regras.ConquistaCodigos.MetasConcluidas10,
                    Nome = "10 metas concluídas",
                    Descricao = "Concluiu 10 metas de poupança.",
                    Icone = "trophy-outline",
                },
                new
                {
                    Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    Codigo = Regras.ConquistaCodigos.MetasConcluidas25,
                    Nome = "25 metas concluídas",
                    Descricao = "Concluiu 25 metas de poupança.",
                    Icone = "trophy-outline",
                });
        });

        modelBuilder.Entity<UsuarioConquista>(e =>
        {
            e.ToTable("UsuariosConquistas");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UsuarioId, x.ConquistaId }).IsUnique(); // idempotencia
        });

        modelBuilder.Entity<ContadorConquista>(e =>
        {
            e.ToTable("ContadoresConquista");
            e.HasKey(x => new { x.UsuarioId, x.Chave });
            e.Property(x => x.Chave).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<SequenciaUsuario>(e =>
        {
            e.ToTable("SequenciasUsuario");
            e.HasKey(x => x.UsuarioId); // uma sequência por usuário (upsert), não histórico
        });
    }
}
