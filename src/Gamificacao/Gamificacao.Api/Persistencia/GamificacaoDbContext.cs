using Gamificacao.Api.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Gamificacao.Api.Persistencia;

public class GamificacaoDbContext : DbContext
{
    public GamificacaoDbContext(DbContextOptions<GamificacaoDbContext> options) : base(options) { }

    public DbSet<MovimentoMoedas> Movimentos => Set<MovimentoMoedas>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MovimentoMoedas>(e =>
        {
            e.ToTable("MovimentosMoedas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Motivo).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.EventId).IsUnique(); // idempotencia: um evento so gera um movimento
        });
    }
}
