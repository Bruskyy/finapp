using Lancamentos.Application.Relatorios;
using Lancamentos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Persistencia;

public class LancamentosDbContext : DbContext
{
    public LancamentosDbContext(DbContextOptions<LancamentosDbContext> options) : base(options) { }

    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Categoria> Categorias => Set<Categoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lancamento>(e =>
        {
            e.ToTable("Lancamentos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.Property(x => x.Valor).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.Data); // consultas por período são o acesso mais comum
        });

        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("Categorias");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<GastoPorCategoria>(e =>
        {
            e.HasNoKey();
            e.ToView(null); // resultado de procedure (SqlQuery), não é tabela nem view mapeada diretamente
            e.Property(x => x.TotalGasto).HasColumnType("decimal(18,2)");
        });
    }
}