# Build de contexto = raiz do repo (docker build -f src/Usuarios/Usuarios.Api/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src ./src
RUN dotnet restore src/Usuarios/Usuarios.Api/Usuarios.Api.csproj
RUN dotnet publish src/Usuarios/Usuarios.Api/Usuarios.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Usuarios.Api.dll"]
