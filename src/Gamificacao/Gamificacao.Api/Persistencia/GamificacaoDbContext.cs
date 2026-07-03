using Gamificacao.Api.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Gamificacao.Api.Persistencia;

public class GamificacaoDbContext : DbContext
{
    public GamificacaoDbContext(DbContextOptions<GamificacaoDbContext> options) : base(options) { }

    public DbSet<MovimentoMoedas> Movimentos => Set<MovimentoMoedas>();
    public DbSet<Resgate> Resgates => Set<Resgate>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MovimentoMoedas>(e =>
        {
            e.ToTable("MovimentosMoedas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Motivo).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.EventId).IsUnique(); // idempotencia: um evento so gera um movimento
        });

        modelBuilder.Entity<Resgate>(e =>
        {
            e.ToTable("Resgates");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Payload).IsRequired();
            e.HasIndex(x => x.ProcessadoEm);
        });
    }
}
