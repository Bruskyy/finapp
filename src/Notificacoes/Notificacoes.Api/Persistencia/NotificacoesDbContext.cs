using Microsoft.EntityFrameworkCore;
using Notificacoes.Api.Dominio;

namespace Notificacoes.Api.Persistencia;

public class NotificacoesDbContext : DbContext
{
    public NotificacoesDbContext(DbContextOptions<NotificacoesDbContext> options) : base(options) { }

    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notificacao>(e =>
        {
            e.ToTable("Notificacoes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Mensagem).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.EventId).IsUnique(); // idempotencia: um evento so gera uma notificacao
            e.HasIndex(x => x.UsuarioId);
            e.Property(x => x.CategoriaMaiorGasto).HasMaxLength(200);
            e.Property(x => x.NomeObjetivoDestaque).HasMaxLength(200);
            e.Property(x => x.EconomiaVsSemanaAnterior).HasColumnType("decimal(18,2)");
            e.Property(x => x.ValorCategoriaMaiorGasto).HasColumnType("decimal(18,2)");
            e.Property(x => x.PercentualObjetivoDestaque).HasColumnType("decimal(5,1)");
        });
    }
}
