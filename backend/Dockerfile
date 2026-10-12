# Contexto de build: la raíz del repositorio (el build aplica el .editorconfig de la raíz).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY .editorconfig ./
COPY backend/ backend/
RUN dotnet restore backend/DomoNow.Parking.slnx
RUN dotnet publish backend/src/DomoNow.Parking.Api -c Release --no-restore -o /app

# Pruebas (unitarias + integración contra el PostgreSQL del compose): docker compose run --rm tests
FROM build AS tests
ENTRYPOINT ["dotnet", "test", "backend/DomoNow.Parking.slnx"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DomoNow.Parking.Api.dll"]
