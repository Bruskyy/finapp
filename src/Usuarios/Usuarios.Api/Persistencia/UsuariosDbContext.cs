using Microsoft.EntityFrameworkCore;
using Usuarios.Api.Dominio;

namespace Usuarios.Api.Persistencia;

public class UsuariosDbContext : DbContext
{
    public UsuariosDbContext(DbContextOptions<UsuariosDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.SenhaHash); // nulo para contas Google (ver Usuario.CriarComGoogle)
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.NomeObjetivoPersonalizado).HasMaxLength(200);
            e.Property(x => x.ValorMensalDesejado).HasColumnType("decimal(18,2)");
            e.Property(x => x.ValorAlvoObjetivo).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired(); // SHA-256 em hex = 64 chars
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UsuarioId); // suporta "sair de todos os dispositivos"
        });
    }
}
