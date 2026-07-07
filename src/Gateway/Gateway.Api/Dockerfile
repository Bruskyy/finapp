# Build de contexto = raiz do repo (docker build -f src/Gateway/Gateway.Api/Dockerfile .)
# porque Gateway.Api nao tem ProjectReference nenhuma, mas o padrao e o
# mesmo dos outros 4 Dockerfiles do monorepo, por consistencia.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src ./src
RUN dotnet restore src/Gateway/Gateway.Api/Gateway.Api.csproj
RUN dotnet publish src/Gateway/Gateway.Api/Gateway.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render (free tier) termina TLS no proxy dele e repassa a requisicao em
# HTTP puro pro container nesta porta - ver UseForwardedHeaders() no
# Program.cs pra ASP.NET Core reconhecer que a origem era HTTPS.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
