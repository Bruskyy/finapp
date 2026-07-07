# Build de contexto = raiz do repo (docker build -f src/Lancamentos/Lancamentos.Api/Dockerfile .)
# Precisa do repo inteiro: Lancamentos.Api -> Application/Infrastructure ->
# Domain e BuildingBlocks.Contracts (Clean Architecture, 4 projetos).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src ./src
RUN dotnet restore src/Lancamentos/Lancamentos.Api/Lancamentos.Api.csproj
RUN dotnet publish src/Lancamentos/Lancamentos.Api/Lancamentos.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Lancamentos.Api.dll"]
