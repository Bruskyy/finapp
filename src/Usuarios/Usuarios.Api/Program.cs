using Microsoft.EntityFrameworkCore;
using Usuarios.Api.Persistencia;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UsuariosDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("UsuariosDb")));

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<UsuariosDbContext>("postgres");

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.Run();

public partial class Program { } // visivel para os testes de integracao (WebApplicationFactory)
