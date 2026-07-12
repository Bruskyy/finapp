using Microsoft.EntityFrameworkCore;
using Usuarios.Api.Dominio;
using Usuarios.Api.Persistencia;

namespace Usuarios.Tests;

public class ApoioRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public ApoioRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private UsuariosDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<UsuariosDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new UsuariosDbContext(options);
    }

    private static async Task<Usuario> CriarUsuarioComCriadoEmAsync(UsuariosDbContext db, DateTime criadoEm)
    {
        var usuario = new Usuario("Vitor", $"{Guid.NewGuid()}@teste.com", "hash-fake");
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        // CriadoEm é setado no construtor (DateTime.UtcNow) - sobrescrito via
        // SQL direto pra simular contas antigas sem expor um setter de teste
        // na entidade (invariante de domínio não deve ter setter público).
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Usuarios\" SET \"CriadoEm\" = {criadoEm} WHERE \"Id\" = {usuario.Id}");
        return usuario;
    }

    [Fact]
    public async Task ListarElegiveisAsync_ContaMuitoNova_NaoDeveSerElegivel()
    {
        var agora = DateTime.UtcNow;
        Usuario usuario;
        await using (var db = CriarDbContext())
            usuario = await CriarUsuarioComCriadoEmAsync(db, agora.AddDays(-5)); // só 5 dias de uso

        await using var verificacao = CriarDbContext();
        var repo = new ApoioRepository(verificacao);

        var elegiveis = await repo.ListarElegiveisAsync(agora.AddDays(-30), agora.AddDays(-90), CancellationToken.None);

        Assert.DoesNotContain(usuario.Id, elegiveis);
    }

    [Fact]
    public async Task ListarElegiveisAsync_ContaComTrintaDiasENuncaNotificada_DeveSerElegivel()
    {
        var agora = DateTime.UtcNow;
        Usuario usuario;
        await using (var db = CriarDbContext())
            usuario = await CriarUsuarioComCriadoEmAsync(db, agora.AddDays(-31));

        await using var verificacao = CriarDbContext();
        var repo = new ApoioRepository(verificacao);

        var elegiveis = await repo.ListarElegiveisAsync(agora.AddDays(-30), agora.AddDays(-90), CancellationToken.None);

        Assert.Contains(usuario.Id, elegiveis);
    }

    [Fact]
    public async Task RegistrarEnvioEEnfileirarAsync_DeveGravarCooldownEEnfileirarUmEvento()
    {
        var agora = DateTime.UtcNow;
        Usuario usuario;
        await using (var db = CriarDbContext())
            usuario = await CriarUsuarioComCriadoEmAsync(db, agora.AddDays(-31));

        await using (var db = CriarDbContext())
        {
            var repo = new ApoioRepository(db);
            await repo.RegistrarEnvioEEnfileirarAsync(usuario.Id, CancellationToken.None);
        }

        await using var verificacao = CriarDbContext();
        var cooldown = await verificacao.ApoiosNotificados.SingleAsync(a => a.UsuarioId == usuario.Id);
        Assert.True(cooldown.UltimoEnvioEm > agora.AddMinutes(-1));

        var outbox = await verificacao.OutboxMessages.SingleAsync();
        Assert.Equal("ApoioSolicitadoEvent", outbox.Tipo);
        Assert.Null(outbox.ProcessadoEm);
    }

    [Fact]
    public async Task ListarElegiveisAsync_JaNotificadoDentroDoCooldown_NaoDeveSerElegivelDeNovo()
    {
        var agora = DateTime.UtcNow;
        Usuario usuario;
        await using (var db = CriarDbContext())
            usuario = await CriarUsuarioComCriadoEmAsync(db, agora.AddDays(-40));

        await using (var db = CriarDbContext())
            await new ApoioRepository(db).RegistrarEnvioEEnfileirarAsync(usuario.Id, CancellationToken.None);

        await using var verificacao = CriarDbContext();
        var elegiveis = await new ApoioRepository(verificacao)
            .ListarElegiveisAsync(agora.AddDays(-30), agora.AddDays(-90), CancellationToken.None); // cooldown de 90 dias ainda não passou

        Assert.DoesNotContain(usuario.Id, elegiveis);
    }

    [Fact]
    public async Task ListarElegiveisAsync_CooldownVencido_DeveSerElegivelDeNovo()
    {
        var agora = DateTime.UtcNow;
        Usuario usuario;
        await using (var db = CriarDbContext())
            usuario = await CriarUsuarioComCriadoEmAsync(db, agora.AddDays(-200));

        await using (var db = CriarDbContext())
        {
            await new ApoioRepository(db).RegistrarEnvioEEnfileirarAsync(usuario.Id, CancellationToken.None);
            // simula que o último envio foi há 100 dias (cooldown de 90 já venceu)
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"ApoiosNotificados\" SET \"UltimoEnvioEm\" = {agora.AddDays(-100)} WHERE \"UsuarioId\" = {usuario.Id}");
        }

        await using var verificacao = CriarDbContext();
        var elegiveis = await new ApoioRepository(verificacao)
            .ListarElegiveisAsync(agora.AddDays(-30), agora.AddDays(-90), CancellationToken.None);

        Assert.Contains(usuario.Id, elegiveis);
    }
}
