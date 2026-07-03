var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// O app Expo em modo web roda em outra origem (porta do Metro bundler) e
// precisa de CORS pra falar com o Gateway - nao se aplica ao app nativo
// (iOS/Android), onde CORS e um conceito exclusivo de navegador.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppMobileWeb", policy =>
        policy.WithOrigins("http://localhost:8081", "http://localhost:19006")
              .AllowAnyHeader()
              .AllowAnyMethod());
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

app.MapHealthChecks("/health");

app.MapReverseProxy();

app.Run();
