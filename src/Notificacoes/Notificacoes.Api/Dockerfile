# Build de contexto = raiz do repo (docker build -f src/Notificacoes/Notificacoes.Api/Dockerfile .)
# Precisa do repo inteiro porque Notificacoes.Api referencia
# BuildingBlocks.Contracts (fora da propria pasta do servico).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src ./src
RUN dotnet restore src/Notificacoes/Notificacoes.Api/Notificacoes.Api.csproj
RUN dotnet publish src/Notificacoes/Notificacoes.Api/Notificacoes.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Notificacoes.Api.dll"]
