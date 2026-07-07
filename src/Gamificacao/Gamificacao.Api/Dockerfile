# Build de contexto = raiz do repo (docker build -f src/Gamificacao/Gamificacao.Api/Dockerfile .)
# Precisa do repo inteiro porque Gamificacao.Api referencia
# BuildingBlocks.Contracts (fora da propria pasta do servico).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src ./src
RUN dotnet restore src/Gamificacao/Gamificacao.Api/Gamificacao.Api.csproj
RUN dotnet publish src/Gamificacao/Gamificacao.Api/Gamificacao.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Gamificacao.Api.dll"]
