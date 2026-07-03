using Notificacoes.Api.Mensageria;
using Notificacoes.Api.Provedores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<INotificacaoProvider, NotificacaoProviderSimulado>();
builder.Services.AddHostedService<ResgateSolicitadoConsumerService>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
