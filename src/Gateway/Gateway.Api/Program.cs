using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// O Gateway e o unico ponto que valida o Bearer token (ver README, secao
// "Decisoes de arquitetura") - os microservicos downstream ainda nao tem
// awareness de auth propria, exceto o Usuarios.Api no seu endpoint /me.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorization(options =>
{
    // "default" é nome reservado internamente pelo YARP - usar outro nome.
    options.AddPolicy("RequerAutenticacao", policy => policy.RequireAuthenticatedUser());
});

// O app Expo em modo web roda em outra origem (porta do Metro bundler) e
// precisa de CORS pra falar com o Gateway - nao se aplica ao app nativo
// (iOS/Android), onde CORS e um conceito exclusivo de navegador.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppMobileWeb", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Em dev o Expo Web pode rodar em qualquer porta/IP da rede local
            // (preview no navegador da máquina, teste no navegador do celular
            // via IP da máquina) - liberar qualquer origem é seguro aqui pois
            // o Gateway não fica exposto à internet nesta fase do projeto.
            policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins("http://localhost:8081", "http://localhost:19006")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

builder.Services.AddHealthChecks();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AppMobileWeb");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapReverseProxy();

app.Run();
