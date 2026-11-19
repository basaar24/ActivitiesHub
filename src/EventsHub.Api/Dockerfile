# ---------- Etapa 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Husky (git hooks) se dispara después del restore y aquí no hay git: desactívalo.
ENV HUSKY=0

# Copia solo los .csproj primero para aprovechar la caché de capas del restore
COPY src/EventsHub.Domain/EventsHub.Domain.csproj           src/EventsHub.Domain/
COPY src/EventsHub.Persistence/EventsHub.Persistence.csproj src/EventsHub.Persistence/
COPY src/EventsHub.Application/EventsHub.Application.csproj src/EventsHub.Application/
COPY src/EventsHub.Api/EventsHub.Api.csproj                 src/EventsHub.Api/
RUN dotnet restore src/EventsHub.Api/EventsHub.Api.csproj

# Ahora sí, el código fuente
COPY src/ src/
RUN dotnet publish src/EventsHub.Api/EventsHub.Api.csproj \
    -c Release -o /app/publish --no-restore

# ---------- Etapa 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Las imágenes de .NET 8+ escuchan por defecto en el 8080 (HTTP)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# La imagen aspnet incluye el usuario "app" (UID en $APP_UID) pero NO lo activa:
# sin esta línea el proceso correría como root.
USER $APP_UID

ENTRYPOINT ["dotnet", "EventsHub.Api.dll"]
