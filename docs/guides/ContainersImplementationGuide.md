# Guía de implementación con contenedores (Docker + PostgreSQL + Testcontainers)

> **Sesión:** Implementación de Contenedores
> **Rama sugerida:** `containers` (créala desde `parcial03` o `main`)
> **Objetivo:** ejecutar EventsHub completo (base de datos, API y SPA) con un solo comando y validar el back-end con pruebas que usan una base de datos real y desechable (Testcontainers) en lugar de SQLite (archivo o en memoria).
> **Alcance:** las Partes A a E son el trabajo obligatorio. La **Parte F (HTTPS con proxy inverso) es OPCIONAL** y no afecta a A–E.

---

## Cómo leer esta guía

### Leyenda de iconos

| Icono | Significado |
|-------|-------------|
| 🆕 | **Archivo NUEVO.** Se da la ruta exacta y el contenido completo: créalo y pega todo. |
| ✏️ | **Archivo EXISTENTE.** Se da la ruta, qué **Buscar**, por qué **Reemplazar**, o bien "reemplaza TODO el contenido". |
| ▶️ | Comando a ejecutar. Siempre se indica terminal y carpeta. |
| ⏳ | El comando **bloquea la terminal** (se queda corriendo). Abre una segunda terminal para verificar y detén con `Ctrl+C`. |
| ✅ | Resultado esperado (lo que debes ver). |
| 🔎 | Cómo verificar que el paso quedó bien. |
| ⚠️ | Qué hacer si falla. |

### Convenciones de terminal

- **Usa PowerShell 7 (`pwsh`)**, no Windows PowerShell 5.1 (el que viene con Windows). Varios comandos de esta guía (comillas con `"` dentro de `'...'`, `-replace ... | Set-Content`, `curl.exe`) están pensados para PowerShell 7.
- Cada paso indica una de estas dos ubicaciones:
  - **PowerShell 7 · carpeta raíz del repo**: `EventsHub/` (donde está `EventsHub.slnx`).
  - **PowerShell 7 · carpeta `web`**: `EventsHub/web` (no se usa en esta guía salvo que quieras probar el build del front a mano).
- Escribe siempre `curl.exe` (con `.exe`). En Windows PowerShell 5.1 `curl` es un alias de `Invoke-WebRequest` y se comporta distinto.
- En los bloques de código, `#` son comentarios: no hace falta escribirlos.
- En PowerShell la continuación de línea es el backtick `` ` ``. En un `Dockerfile` la continuación de línea es `\` (sintaxis de Linux, porque el `RUN` se ejecuta dentro de un contenedor Linux). No los mezcles.

### Formato de cada paso

Cada paso empieza con `### Paso X.N — título`, una tabla **Tipo / Terminal** y luego los bloques 🆕 / ✏️ / ▶️ / ✅ / 🔎 / ⚠️ que apliquen.

---

## 0. Decisiones de diseño

| # | Decisión | Elección | Por qué (contexto didáctico) |
|---|----------|----------|------------------------------|
| 1 | Base de datos | **PostgreSQL 17 (alpine)** | Imagen de ~100 MB, arranca en segundos, sin EULA ni licencia, multiplataforma. SQL Server pesa ~1.5 GB, exige ≥ 2 GB de RAM y tarda en arrancar: es incómodo en clase y en CI. |
| 2 | Empaquetado | **3 contenedores independientes**: `db`, `api`, `web` | Back-end y front-end ya son dos apps independientes en el repo (`src` y `web`, sin build compartido). Cada una tiene su ciclo de vida, su imagen y se puede escalar/desplegar por separado. |
| 3 | Orquestación local | **Docker Compose** | Un solo archivo declara los tres servicios, la red, el volumen y el orden de arranque. |
| 4 | Pruebas | **Testcontainers.PostgreSql + NUnit** | Las pruebas de controller y de handlers corren contra el mismo motor que producción (no contra SQLite ni `InMemory`), y el contenedor se crea y destruye solo. Las pruebas de mappers siguen siendo puras y no usan Docker. |

### Arquitectura resultante (Partes A–E)

```text
Navegador (host)
   │
   ├── http://localhost:3000 ──► [ web ]  nginx sirviendo el build de React   (puerto interno 80)
   │                                │
   │                                │ (el navegador, no nginx, llama a la API)
   └── http://localhost:5000 ──► [ api ]  ASP.NET Core 10                     (puerto interno 8080)
                                    │
                                    │ Host=db;Port=5432   (red interna de Compose)
                                    ▼
                                 [ db ]  PostgreSQL 17  ── volumen `eventshub-pgdata`
```

Puntos clave que conviene explicar al presentar:

- **Imagen ≠ contenedor.** Una imagen se construye una vez (`docker build`); un contenedor es una instancia en ejecución de esa imagen.
- **Dentro de la red de Compose los servicios se resuelven por nombre** (`Host=db`), nunca por `localhost`. `localhost` dentro de un contenedor es el propio contenedor.
- **El front-end corre en el navegador**, así que la URL de la API que se "hornea" en el build de Vite debe ser alcanzable **desde el host** (`http://localhost:5000`), no desde la red interna.
- **El volumen** (`eventshub-pgdata`) hace que los datos sobrevivan a `docker compose down`. Solo se pierden con `down -v`.

---

## 1. Prerrequisitos

### Paso 1.1 — Verificar herramientas

| Tipo | Terminal |
|------|----------|
| ▶️ comandos de verificación | PowerShell 7 · carpeta raíz del repo |

Necesitas Docker Desktop (backend WSL 2 en Windows 11) **iniciado**, PowerShell 7, SDK de .NET 10 y Node 24.

```powershell
$PSVersionTable.PSVersion     # Major debe ser 7
docker --version
docker compose version
docker run --rm hello-world
dotnet --version              # 10.x
node --version                # v24.x
dotnet ef --version           # Entity Framework Core .NET Command-line Tools 10.x
```

✅ Esperado: `Major` = 7; `dotnet` empieza con `10.`; `node` empieza con `v24.`; `hello-world` imprime `Hello from Docker!`.

⚠️ Si falla:

- `dotnet ef` no se reconoce: instálalo (la versión coincide con EF Core 10.0.11 del proyecto):

  ```powershell
  dotnet tool install --global dotnet-ef --version 10.0.11
  ```

- `docker` no se reconoce o no conecta con el motor: abre Docker Desktop, espera a que diga *Engine running* y abre una terminal nueva.
- `$PSVersionTable.PSVersion` muestra 5.1: abre `pwsh` (PowerShell 7) o, si no lo tienes, `winget install Microsoft.PowerShell`.

> Testcontainers (Parte E) usa el mismo Docker: si `docker run hello-world` funciona, Testcontainers funcionará.

---

## 2. Parte A — Cambiar de SQLite a PostgreSQL

> **Al terminar esta parte tendrás…** la API corriendo con `dotnet run` contra un PostgreSQL en contenedor, con una migración `InitialCreate` generada para PostgreSQL y las fechas siempre en UTC.
>
> **Archivos tocados:**
> - ✏️ `src/EventsHub.Persistence/EventsHub.Persistence.csproj` (vía `dotnet add/remove`)
> - ✏️ `src/EventsHub.Api/appsettings.json`
> - ✏️ `src/EventsHub.Api/appsettings.Development.json`
> - ✏️ `src/EventsHub.Api/Program.cs`
> - ✏️ `src/EventsHub.Persistence/DbInitializer.cs`
> - ✏️ `src/EventsHub.Persistence/AppDbContext.cs`
> - 🆕 `src/EventsHub.Persistence/Migrations/*` (regenerados)

Hoy `AppDbContext` usa `UseSqlite` y la migración `InitialCreate` contiene tipos de SQLite (`TEXT`, `INTEGER`). Hay que cambiar el *provider* y **regenerar** la migración.

### Paso A.1 — Reemplazar el paquete del provider

| Tipo | Terminal |
|------|----------|
| ✏️ `EventsHub.Persistence.csproj` (lo modifica `dotnet`) | PowerShell 7 · carpeta raíz del repo |

```powershell
dotnet remove src/EventsHub.Persistence package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/EventsHub.Persistence package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
```

✅ Esperado: `dotnet add` termina con `info : PackageReference for package 'Npgsql.EntityFrameworkCore.PostgreSQL' version '10.0.3' added to file '...EventsHub.Persistence.csproj'.`

🔎 Verifica:

```powershell
Select-String -Path src/EventsHub.Persistence/EventsHub.Persistence.csproj -Pattern 'PackageReference'
```

Debe verse `Npgsql.EntityFrameworkCore.PostgreSQL` `Version="10.0.3"` y **ninguna** línea con `Sqlite`.

⚠️ **Aviso importante — NO compiles todavía.** Al quitar el paquete de SQLite, el código sigue usando SQLite en tres lugares: `UseSqlite` en `Program.cs` (que cambias en el Paso A.3) y, en los **dos proyectos de pruebas**, `tests/EventsHub.Api.UnitTests/GlobalTestSetup.cs` (`UseSqlite`) y `tests/EventsHub.Application.UnitTests/Events/HandlerTestBase.cs` (`using Microsoft.Data.Sqlite;` y `UseSqlite`); ambos se cambian en la Parte E. Si corres `dotnet build` ahora verás `error CS1061: "DbContextOptionsBuilder" no contiene una definición para "UseSqlite"` y, en `HandlerTestBase.cs`, `error CS0246` por el namespace `Microsoft.Data.Sqlite`. Es lo esperado en este punto, no un error tuyo.

- La **primera** compilación válida de la API es al final del **Paso A.3**.
- `dotnet build EventsHub.slnx` seguirá fallando hasta terminar la **Parte E**, porque fallan **dos** proyectos de pruebas (`EventsHub.Api.UnitTests` y `EventsHub.Application.UnitTests`; este último **no** queda "sin cambios": su `HandlerTestBase` también usaba SQLite). Mientras tanto, compila solo la API con `dotnet build src/EventsHub.Api`.

### Paso A.2 — Cadena de conexión (DOS archivos)

| Tipo | Terminal |
|------|----------|
| ✏️ dos archivos existentes | Ninguna (editor de código) |

Edita **ambos** archivos:

- `src/EventsHub.Api/appsettings.json`
- `src/EventsHub.Api/appsettings.Development.json`

En cada uno, dentro de `ConnectionStrings`:

**Busca:**

```json
"SqliteConnection": "Data source=eventshub.db"
```

**Reemplaza por** (cambia también el **nombre de la clave**):

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=eventshub;Username=eventshub;Password=eventshub_dev"
```

Resultado en cada archivo:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=eventshub;Username=eventshub;Password=eventshub_dev"
}
```

> Estas credenciales son **solo de desarrollo local**. En el contenedor de la API se sobrescriben con variables de entorno (Parte D), por lo que la `POSTGRES_PASSWORD` de tu `.env` **no** tiene que coincidir con esta.

🔎 Verifica:

```powershell
Select-String -Path src/EventsHub.Api/appsettings*.json -Pattern 'Sqlite|DefaultConnection'
```

Debe mostrar dos líneas con `DefaultConnection` y ninguna con `Sqlite`.

### Paso A.3 — `Program.cs`

| Tipo | Terminal |
|------|----------|
| ✏️ `src/EventsHub.Api/Program.cs` | Ninguna (editor de código) |

**Busca** (líneas 11-14):

```csharp
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"));
});
```

**Reemplaza** solo la línea del `opt.Use...`:

```csharp
opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
```

Queda:

```csharp
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
```

No hace falta ningún `using` nuevo (`UseNpgsql` está en el namespace `Microsoft.EntityFrameworkCore`, que ya importa el archivo).

El resto de `Program.cs` se queda **igual**. En particular el CORS actual, que ya permite el puerto público del contenedor `web` (`localhost:3000`), así que no hay que tocarlo:

```csharp
app.UseCors(opt => opt
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithOrigins("http://localhost:3000", "https://localhost:3000"));
```

🔎 Verifica: `dotnet build src/EventsHub.Api` → `Build succeeded.`

### Paso A.4 — Fechas en UTC en el seed (cambio obligatorio con Npgsql)

| Tipo | Terminal |
|------|----------|
| ✏️ `src/EventsHub.Persistence/DbInitializer.cs` | PowerShell 7 · carpeta raíz del repo (opción B) o editor (opción A) |

Npgsql mapea `DateTime` a `timestamp with time zone` y **lanza excepción** si recibe un `DateTime` con `Kind` `Local` o `Unspecified`. `DbInitializer.cs` usa `DateTime.Now` (Kind `Local`) en **10 lugares**, así que fallaría al sembrar.

**Busca** `DateTime.Now.` y **reemplaza por** `DateTime.UtcNow.` en las 10 apariciones. Por ejemplo:

```csharp
// antes
Date = DateTime.Now.AddMonths(-1),
// después
Date = DateTime.UtcNow.AddMonths(-1),
```

**Opción A — VS Code:** abre `DbInitializer.cs`, `Ctrl+H`, Buscar `DateTime.Now.`, Reemplazar `DateTime.UtcNow.`, pulsa *Reemplazar todo* (10 coincidencias).

**Opción B — PowerShell 7 · carpeta raíz del repo:**

```powershell
(Get-Content src/EventsHub.Persistence/DbInitializer.cs -Raw) -replace 'DateTime\.Now\.', 'DateTime.UtcNow.' | Set-Content src/EventsHub.Persistence/DbInitializer.cs -NoNewline
```

🔎 Verifica (no debe devolver **nada**):

```powershell
Select-String -Path src/EventsHub.Persistence/DbInitializer.cs -Pattern 'DateTime\.Now'
```

⚠️ **No** toques `WeatherForecastController.cs`: usa `DateTime.Now` pero no escribe en la base de datos.

### Paso A.5 — Forzar `Kind=Utc` en `Event.Date` (fix robusto)

| Tipo | Terminal |
|------|----------|
| ✏️ `src/EventsHub.Persistence/AppDbContext.cs` | Ninguna (editor de código) |

**Por qué hace falta (verificado en el repo):** el formulario de la SPA (`web/src/features/activities/form/ActivityForm.tsx`) usa `<TextField name="date" type="date" />` y envía los valores tal cual salen de `FormData`, es decir, el string `"2026-11-07"` **sin hora ni `Z`**. System.Text.Json lo convierte a un `DateTime` con `Kind=Unspecified` y Npgsql lanzaría una excepción al guardar: la API respondería **500** al crear o editar una actividad desde la SPA. Corregirlo en el `DbContext` cubre cualquier cliente, sin depender de que el front mande `toISOString()`.

Esto debe hacerse **antes** de generar la migración (Paso A.6) para que el snapshot lo incluya.

Reemplaza **TODO el contenido** de `src/EventsHub.Persistence/AppDbContext.cs` por:

```csharp
using EventsHub.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventsHub.Persistence;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Npgsql (timestamp with time zone) solo acepta DateTime con Kind=Utc.
        // Al guardar: si no es Utc se marca como Utc. Al leer: siempre Utc.
        modelBuilder.Entity<Event>()
            .Property(e => e.Date)
            .HasConversion(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
```

> `SpecifyKind` **no convierte** la hora, solo etiqueta el valor como UTC. Para una fecha de calendario (`2026-11-07` = medianoche) es el comportamiento deseado.

🔎 Verifica: `dotnet build src/EventsHub.Api` → `Build succeeded.`

### Paso A.6 — Regenerar la migración

| Tipo | Terminal |
|------|----------|
| ▶️ comandos `Remove-Item` y `dotnet ef` | PowerShell 7 · carpeta raíz del repo |

Las migraciones son código generado y en este proyecto **no se editan a mano**. Esta es la **única excepción**: al cambiar de provider se **borran y se regeneran**, no se editan.

`migrations add` solo compila y compara el modelo: **no necesita que la base de datos esté corriendo**.

```powershell
# 1. Borra la migración y el snapshot de SQLite
Remove-Item -Recurse -Force src/EventsHub.Persistence/Migrations

# 2. Borra la base SQLite local (y sus archivos auxiliares); ya no se usa
Remove-Item -Force -ErrorAction SilentlyContinue src/EventsHub.Api/eventshub.db*

# 3. Genera la migración inicial para PostgreSQL
dotnet ef migrations add InitialCreate -p src/EventsHub.Persistence -s src/EventsHub.Api
```

✅ Esperado: el comando termina con `Done.` y aparecen **3 archivos nuevos** en `src/EventsHub.Persistence/Migrations/`:

- `<timestamp>_InitialCreate.cs`
- `<timestamp>_InitialCreate.Designer.cs`
- `AppDbContextModelSnapshot.cs`

🔎 Verifica que sea migración de PostgreSQL:

```powershell
Select-String -Path src/EventsHub.Persistence/Migrations/*_InitialCreate.cs -Pattern 'timestamp with time zone'
```

Debe devolver una línea (la columna `Date`). Si abres el archivo verás también `text` y `boolean`.

⚠️ Si falla:

- `Build failed`: corrige primero el error que muestra (casi siempre un typo en A.3 o A.5) y repite solo el paso 3.
- `Could not execute because the specified command or file was not found` / `dotnet-ef`: revisa el Paso 1.1.
- Si ves `TEXT`/`INTEGER` en la migración, quedó el provider anterior: repite A.1 y A.3.

### Paso A.7 — Probar la API contra un Postgres en contenedor (antes de dockerizar la API)

| Tipo | Terminal |
|------|----------|
| ▶️ `docker run` (no bloquea: `-d`) | Terminal 1 · PowerShell 7 · carpeta raíz del repo |
| ⏳ `dotnet run` (**bloquea**) | Terminal 1 · PowerShell 7 · carpeta raíz del repo |
| 🔎 verificación | Navegador, o Terminal 2 · PowerShell 7 · carpeta raíz del repo |

Requisitos: certificado de desarrollo HTTPS de .NET confiable (una sola vez) y el puerto 5432 libre.

```powershell
dotnet dev-certs https --trust
Get-NetTCPConnection -LocalPort 5432 -ErrorAction SilentlyContinue   # no debe devolver nada
```

⚠️ Si el segundo comando devuelve filas, otro programa (p. ej. un PostgreSQL instalado en Windows) usa el 5432: ciérralo o cambia el mapeo a `-p 5433:5432` **y** el puerto en `DefaultConnection` de `appsettings.Development.json`.

**Terminal 1** — levanta la base y espera a que acepte conexiones:

```powershell
docker run -d --name eventshub-db `
  -e POSTGRES_USER=eventshub `
  -e POSTGRES_PASSWORD=eventshub_dev `
  -e POSTGRES_DB=eventshub `
  -p 5432:5432 `
  postgres:17-alpine

docker exec eventshub-db pg_isready -U eventshub
```

✅ Esperado de `pg_isready`: `/var/run/postgresql:5432 - accepting connections` (si dice `no response`, espera unos segundos y repite).

Ahora arranca la API. ⏳ **Este comando bloquea la terminal**: se queda corriendo hasta que pulses `Ctrl+C`.

```powershell
dotnet run --project src/EventsHub.Api
```

✅ Esperado en el log: `Applying migration '<timestamp>_InitialCreate'`, luego `Now listening on: https://localhost:5001` y **sin** la línea `An error occurred during database migration.`

ℹ️ **Es normal** ver al inicio un `fail: Microsoft.EntityFrameworkCore.Database.Command[20102]` sobre `SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory"`. En una base de datos nueva esa tabla todavía no existe: EF intenta leerla, falla, y enseguida la crea y aplica la migración. Lo que importa es que después aparezca `Applying migration` y `Now listening`.

🔎 **Verifica** abriendo `https://localhost:5001/api/v1/events` en el navegador (deben verse los 10 eventos sembrados), o en una **Terminal 2**:

```powershell
(Invoke-RestMethod https://localhost:5001/api/v1/events).Count
curl.exe -s https://localhost:5001/api/v1/events
```

✅ El primero imprime `10`; el segundo, el JSON.

Cuando termines: en la Terminal 1 pulsa `Ctrl+C` para detener la API y luego elimina el contenedor (el Paso D.2 vuelve a usar el puerto 5432):

```powershell
docker rm -f eventshub-db
```

⚠️ **Gotcha del proyecto:** `Program.cs` captura y solo *registra* cualquier error de migración; la API arranca igual aunque la BD no exista. Si `/events` devuelve 500, revisa el log de arranque buscando `An error occurred during database migration.` (causas típicas: Postgres aún no listo, puerto equivocado, `Kind=Unspecified` si te saltaste A.5).

**Commit sugerido:** `feat(containers): migrate persistence from SQLite to PostgreSQL`

---

## 3. Parte B — Contenedor del back-end (API)

> **Al terminar esta parte tendrás…** la imagen `eventshub-api:dev` construida con un Dockerfile multi-stage que corre como usuario no-root y escucha en el 8080.
>
> **Archivos tocados:**
> - 🆕 `.dockerignore` (raíz del repo)
> - 🆕 `src/EventsHub.Api/Dockerfile`

### Paso B.1 — `.dockerignore` (raíz del repo)

| Tipo | Terminal |
|------|----------|
| 🆕 `.dockerignore` | Ninguna (editor de código) |

Ruta: `EventsHub/.dockerignore` (junto a `EventsHub.slnx`). Evita copiar binarios, `node_modules`, secretos y documentación al contexto de build. Es sintaxis de `.dockerignore` (parecida, pero no idéntica, a `.gitignore`).

```text
**/bin
**/obj
**/node_modules
**/.vs
.git
.husky
graphify-out
docs
openspec
web
tests
.env
certs
*.db
```

🔎 Verifica: `Test-Path .dockerignore` (PowerShell 7 · raíz) → `True`.

### Paso B.2 — Dockerfile de la API

| Tipo | Terminal |
|------|----------|
| 🆕 `src/EventsHub.Api/Dockerfile` | Ninguna (editor de código) |

Ruta: `EventsHub/src/EventsHub.Api/Dockerfile` (sin extensión). Build **multi-stage**: el SDK (pesado) compila; la imagen final solo lleva el runtime de ASP.NET.

```dockerfile
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
```

Notas didácticas:

- La `\` al final de la línea del `RUN dotnet publish` es la **continuación de línea de Linux dentro del Dockerfile**, no el backtick de PowerShell.
- El orden `COPY .csproj → restore → COPY código` hace que `dotnet restore` solo se repita cuando cambian los paquetes, no en cada edición de código.
- `USER $APP_UID` va **antes** del `ENTRYPOINT`; el puerto 8080 (> 1024) se puede abrir sin ser root.
- En contenedor se usa **HTTP** (8080). El HTTPS de desarrollo (`https://localhost:5001`) es una conveniencia local; en producción lo normal es terminar TLS en un proxy inverso (ver Parte F, opcional).
- `EventsHub.OpenApi` no se incluye: es un host auxiliar solo para generar el cliente tipado.

### Paso B.3 — Construir y probar la imagen de la API

| Tipo | Terminal |
|------|----------|
| ▶️ `docker build` (termina solo) | PowerShell 7 · carpeta raíz del repo |
| ⏳ `docker run` (**bloquea**) | PowerShell 7 · carpeta raíz del repo |

El contexto de build es la raíz del repo (por eso el `.` final).

```powershell
docker build -f src/EventsHub.Api/Dockerfile -t eventshub-api:dev .
```

✅ Esperado: la última línea de salida incluye `naming to docker.io/library/eventshub-api:dev`.

🔎 Verifica:

```powershell
docker images eventshub-api
```

Debe aparecer `eventshub-api` con tag `dev`.

**Prueba mínima de arranque.** ⏳ Bloquea la terminal hasta `Ctrl+C`:

```powershell
docker run --rm -p 5000:8080 eventshub-api:dev
```

✅ Esperado: aparece `Now listening on: http://[::]:8080` y también un error de conexión a la base de datos (`An error occurred during database migration.` con un `Npgsql`/`Connection refused`). **Es esperado**: este contenedor corre solo, sin `db` (en Parte D se conectan). Las migraciones se intentan antes de que Kestrel arranque, así que el error puede salir antes que la línea `Now listening`. Detén con `Ctrl+C` (`--rm` borra el contenedor).

⚠️ Si falla:

- `restore` intenta ejecutar Husky o `dotnet tool`: confirma `ENV HUSKY=0` en el Dockerfile.
- `COPY failed: file not found`: estás ejecutando desde una carpeta distinta de la raíz, o el `.dockerignore` excluye algo que el build necesita (no excluyas `src`).
- `port is already allocated` en el 5000: cambia a `-p 5050:8080`.

**Commit sugerido:** `feat(containers): add API Dockerfile`

---

## 4. Parte C — Contenedor del front-end (SPA)

> **Al terminar esta parte tendrás…** los tres archivos necesarios para construir la imagen de la SPA (build de Vite + nginx). Se construirá y probará junto con el resto en la Parte D.
>
> **Archivos tocados:**
> - 🆕 `web/.dockerignore`
> - 🆕 `web/nginx.conf`
> - 🆕 `web/Dockerfile`
>
> `web/vite.config.ts` **no se toca**.

### Paso C.1 — `web/.dockerignore`

| Tipo | Terminal |
|------|----------|
| 🆕 `web/.dockerignore` | Ninguna (editor de código) |

Ruta: `EventsHub/web/.dockerignore`.

```text
node_modules
dist
.env*
```

### Paso C.2 — `web/nginx.conf`

| Tipo | Terminal |
|------|----------|
| 🆕 `web/nginx.conf` | Ninguna (editor de código) |

Ruta: `EventsHub/web/nginx.conf`. La app usa `react-router`, así que cualquier ruta (`/activities/123`) debe devolver `index.html`:

```nginx
server {
  listen 80;
  root /usr/share/nginx/html;
  index index.html;

  location / {
    try_files $uri $uri/ /index.html;
  }

  # index.html NO lleva hash en el nombre: que el navegador lo revalide siempre,
  # para que un despliegue nuevo se vea sin vaciar la caché.
  location = /index.html {
    add_header Cache-Control "no-cache";
  }

  # Los assets de Vite llevan hash en el nombre: se pueden cachear "para siempre"
  location /assets/ {
    expires 1y;
    add_header Cache-Control "public, immutable";
  }
}
```

### Paso C.3 — `web/Dockerfile`

| Tipo | Terminal |
|------|----------|
| 🆕 `web/Dockerfile` | Ninguna (editor de código) |

Ruta: `EventsHub/web/Dockerfile` (sin extensión).

```dockerfile
# ---------- Etapa 1: build de Vite ----------
FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# VITE_* se incrusta en el JS durante el build (no existe en runtime).
ARG VITE_API_URL=http://localhost:5000/api/v1
ENV VITE_API_URL=$VITE_API_URL

RUN npm run build

# ---------- Etapa 2: nginx ----------
FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
```

Notas didácticas:

- **`VITE_API_URL` es de *build*, no de *runtime*.** Cambiar la URL de la API implica reconstruir la imagen (`--build-arg`). Es una limitación real de las SPA; mencionarla en la presentación. (La alternativa es que un proxy haga `/api → api:8080` y la SPA use una URL relativa: es justo lo que hace la Parte F, opcional.)
- El `.env.development` actual (`https://localhost:5001/api/v1`) **no** entra a la imagen (está en `web/.dockerignore`): el valor de contenedor se define con el `ARG`.
- `vite.config.ts` incluye `vite-plugin-mkcert`, pero ese plugin solo actúa con `vite` (dev server, `apply: 'serve'`) y **no corre en `vite build`**: no hay que modificar `vite.config.ts` para el contenedor.

🔎 Verifica que existen los tres archivos (PowerShell 7 · raíz del repo):

```powershell
Test-Path web/.dockerignore, web/nginx.conf, web/Dockerfile
```

✅ Esperado: `True` tres veces.

**Commit sugerido:** `feat(containers): add web Dockerfile and nginx config`

---

## 5. Parte D — Orquestación con Docker Compose

> **Al terminar esta parte tendrás…** `docker compose up --build -d` levantando `db` (healthy), `api` (puerto 5000) y `web` (puerto 3000), con datos persistentes en un volumen.
>
> **Archivos tocados:**
> - 🆕 `.env.example`
> - 🆕 `.env` (copia local, **no** se versiona)
> - 🆕 `docker-compose.yml`

### Paso D.1 — Variables de entorno

| Tipo | Terminal |
|------|----------|
| 🆕 `.env.example` | Ninguna (editor de código) |
| ▶️ copiar a `.env` | PowerShell 7 · carpeta raíz del repo |

Ruta: `EventsHub/.env.example` (se versiona).

```dotenv
POSTGRES_USER=eventshub
POSTGRES_PASSWORD=change_me
POSTGRES_DB=eventshub
```

Luego crea tu copia local `.env`:

```powershell
Copy-Item .env.example .env
```

> `POSTGRES_PASSWORD` **no tiene que coincidir** con la `Password=eventshub_dev` de `appsettings`: dentro del contenedor la API recibe la cadena de conexión por variable de entorno (Paso D.2), que manda sobre el `appsettings.json`.

🔎 Verifica que `.env` está ignorado por git y `.env.example` no:

```powershell
git check-ignore -v .env
```

✅ Esperado: `.gitignore:12:*.env	.env` (el número de línea puede variar). `git check-ignore -v .env.example` no debe imprimir nada.

### Paso D.2 — `docker-compose.yml`

| Tipo | Terminal |
|------|----------|
| 🆕 `docker-compose.yml` | Ninguna (editor de código) |
| 🔎 `docker compose config` | PowerShell 7 · carpeta raíz del repo |

Ruta: `EventsHub/docker-compose.yml`.

```yaml
name: eventshub

services:
  db:
    image: postgres:17-alpine
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    ports:
      - "5432:5432"            # opcional: solo para conectar con DBeaver/pgAdmin desde el host
    volumes:
      - eventshub-pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 5s
      timeout: 3s
      retries: 10

  api:
    build:
      context: .
      dockerfile: src/EventsHub.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      # "__" en variables de entorno equivale a ":" en la configuración de .NET
      ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
    ports:
      - "5000:8080"
    depends_on:
      db:
        condition: service_healthy   # no arrancar la API hasta que Postgres acepte conexiones

  web:
    build:
      context: ./web
      args:
        VITE_API_URL: http://localhost:5000/api/v1
    ports:
      - "3000:80"
    depends_on:
      - api

volumes:
  eventshub-pgdata:
```

Por qué cada pieza:

- `healthcheck` + `condition: service_healthy`: `Program.cs` ejecuta `MigrateAsync()` al arrancar y **traga** los errores; sin esperar a la BD, la API arrancaría sin esquema y fallaría en silencio.
- `ConnectionStrings__DefaultConnection` sobrescribe el `appsettings.json` sin tocar el código: así la misma imagen sirve en cualquier entorno.
- `3000:80` es deliberado: el CORS de la API ya permite `http://localhost:3000`.
- Con `name: eventshub`, Compose antepone el nombre del proyecto al volumen: el volumen **real** se llama `eventshub_eventshub-pgdata` (compruébalo con `docker volume ls`).

🔎 Verifica la sintaxis y la sustitución de variables:

```powershell
docker compose config
```

✅ Esperado: imprime el YAML ya resuelto, con los valores de `.env` sustituidos (por ejemplo `POSTGRES_USER: eventshub`) y sin errores.

⚠️ Si dice `variable is not set` o muestra valores vacíos: no existe `.env` en la raíz (repite D.1). Si el puerto 5432 está ocupado al arrancar, asegúrate de haber ejecutado `docker rm -f eventshub-db` (Paso A.7).

### Paso D.3 — Levantar todo

| Tipo | Terminal |
|------|----------|
| ▶️ `up` y `ps` (terminan solos) | PowerShell 7 · carpeta raíz del repo |
| ⏳ `logs -f` (**bloquea**) | PowerShell 7 · carpeta raíz del repo |

```powershell
docker compose up --build -d
docker compose ps
```

La primera vez tarda varios minutos (descarga imágenes, `dotnet restore`, `npm ci`).

✅ Esperado en `docker compose ps`: `db` con estado `Up ... (healthy)`; `api` y `web` con `Up`.

Para seguir el arranque de la API (⏳ bloquea la terminal):

```powershell
docker compose logs -f api
```

✅ Esperado: aparece `Now listening on: http://[::]:8080` y **no** aparece `An error occurred during database migration.`. Con `Ctrl+C` sales del seguimiento de logs; **los contenedores siguen corriendo** (no se detienen).

⚠️ Si falla:

- `api` reinicia o `/events` da 500: `docker compose logs api` y busca el error de migración.
- `port is already allocated` (3000, 5000 o 5432): algo ya usa ese puerto (¿`npm run dev`, `dotnet run` o el contenedor `eventshub-db` de A.7?). Detenlo y repite.
- `web` falla en `npm ci`: confirma que existe `web/package-lock.json`.

### Paso D.4 — Verificación (demostración)

| Tipo | Terminal |
|------|----------|
| 🔎 comandos de la tabla | PowerShell 7 · carpeta raíz del repo (o navegador) |

| Qué | Cómo | Resultado esperado |
|-----|------|--------------------|
| Base de datos | `docker compose exec db psql -U eventshub -d eventshub -c 'SELECT count(*) FROM "Events";'` | `count` = `10` |
| API | `curl.exe -s http://localhost:5000/api/v1/events` o `(Invoke-RestMethod http://localhost:5000/api/v1/events).Count` | JSON con 10 eventos / `10` |
| SPA | Abrir `http://localhost:3000` | Se muestra la aplicación (la lista de actividades está en `http://localhost:3000/activities`) |
| Rutas SPA | Copia un `id` del JSON de la API y abre `http://localhost:3000/activities/<id>`; luego pulsa F5 | Muestra el detalle y **no** da 404 al recargar (gracias a `try_files`) |
| Persistencia | Ver procedimiento abajo | El dato creado sigue ahí |
| Reinicio limpio | `docker compose down -v` → `docker compose up -d` | BD vacía y vuelve a sembrar 10 eventos |

La salida esperada de `psql` es:

```text
 count
-------
    10
(1 row)
```

**Cómo hacer la prueba de persistencia:**

1. Abre `http://localhost:3000/createActivity` (formulario de la SPA), llena los campos y pulsa *Submit*. La SPA te lleva al detalle de la actividad creada.
2. Verifica que ahora hay 11: `docker compose exec db psql -U eventshub -d eventshub -c 'SELECT count(*) FROM "Events";'` → `11`.
3. `docker compose down` y luego `docker compose up -d`.
4. Repite el `SELECT`: debe seguir en `11` y tu actividad debe aparecer en `http://localhost:3000/activities`.

> Las respuestas de la SPA tardan ~1 s a propósito: `web/src/lib/api/agent.ts` agrega un `sleep(1000)` en el interceptor. No es un error del contenedor.

### Comandos útiles

▶️ PowerShell 7 · carpeta raíz del repo:

```powershell
docker compose logs -f           # logs de todos los servicios (bloquea; Ctrl+C para salir)
docker compose build api         # reconstruir solo la API
docker compose restart api
docker compose down              # detiene y elimina contenedores (conserva el volumen)
docker compose down -v           # además elimina el volumen (borra los datos)
docker images | Select-String eventshub
```

**Commit sugerido:** `feat(containers): add docker-compose for db, api and web`

---

## 6. Parte E — Pruebas con Testcontainers

> **Al terminar esta parte tendrás…** las pruebas de `EventsHub.Api.UnitTests` (8) y los handlers de `EventsHub.Application.UnitTests` (12) corriendo contra un PostgreSQL efímero que se crea y destruye solo, las 20 pruebas de `Mapping/` intactas y sin Docker, `dotnet test EventsHub.slnx` en verde con 40 pruebas y `dotnet build EventsHub.slnx` compilando de nuevo.
>
> **Archivos tocados:**
> - ✏️ `tests/EventsHub.Api.UnitTests/EventsHub.Api.UnitTests.csproj` (vía `dotnet add`)
> - ✏️ `tests/EventsHub.Api.UnitTests/GlobalTestSetup.cs`
> - ✏️ `tests/EventsHub.Application.UnitTests/EventsHub.Application.UnitTests.csproj` (vía `dotnet add`)
> - 🆕 `tests/EventsHub.Application.UnitTests/Events/PostgresFixture.cs`
> - ✏️ `tests/EventsHub.Application.UnitTests/Events/HandlerTestBase.cs`
>
> **Archivos que NO se tocan (cero cambios):** `tests/EventsHub.Api.UnitTests/Controllers/EventsControllerTests.cs`, los cinco archivos de handlers (`CreateEventTests.cs`, `DeleteEventTests.cs`, `EditEventTests.cs`, `GetEventDetailsTests.cs`, `GetEventListTests.cs`) y todo `tests/EventsHub.Application.UnitTests/Mapping/`.

### Qué cambia

Hoy hay dos proyectos de pruebas que tocan la base de datos, y ambos usaban SQLite. Después del Paso A.1 ninguno de los dos compila (`UseSqlite` y `Microsoft.Data.Sqlite` ya no existen), así que esta parte los migra a PostgreSQL con el cambio mínimo.

| Proyecto / carpeta | Antes | Después |
|--------------------|-------|---------|
| `Api.UnitTests` (8 pruebas de controller) | `GlobalTestSetup` crea el archivo `eventshub.db` (SQLite), migra y siembra con `DbInitializer`; un `AppDbContext` estático compartido | `GlobalTestSetup` levanta **un contenedor PostgreSQL compartido** por toda la corrida, migra y siembra igual que antes. El `AppDbContext` estático se conserva. `EventsControllerTests` **no cambia** |
| `Application.UnitTests/Events` (12 pruebas de handlers) | `HandlerTestBase` abre una conexión SQLite `:memory:` por prueba y usa `EnsureCreatedAsync` | Un **contenedor compartido por proyecto** (`PostgresFixture`) y, dentro de él, una **base de datos nueva por prueba** (creada con `MigrateAsync`, borrada al terminar). Los cinco archivos de pruebas **no cambian** |
| `Application.UnitTests/Mapping` (20 pruebas) | Pruebas puras, sin base de datos | **Sin cambios y sin Docker**: siguen siendo pruebas unitarias puras |

Total: 20 de mappers + 12 de handlers + 8 de controller = **40 pruebas**.

#### Por qué una base de datos NUEVA por prueba (handlers)

Los handlers se probaban con "BD vacía por prueba": `GetEventListTests` exige que, sin sembrar nada, el resultado esté vacío (0 filas), y que después de sembrar tres eventos devuelva **exactamente 3**. Con un contenedor compartido hay que seguir garantizando ese aislamiento. Se evaluaron estas alternativas y se descartaron:

| Alternativa | Por qué se descartó |
|-------------|---------------------|
| `TRUNCATE` / `ExecuteDelete` en cada `[SetUp]` | Hay que acordarse de limpiar cada tabla nueva que se agregue al modelo, y una prueba que falle a medias puede dejar basura para la siguiente |
| Respawn (paquete que limpia tablas) | Una dependencia más para resolver un problema que se arregla con una línea (`EnsureDeletedAsync`) |
| Transacción con rollback por prueba | En PostgreSQL, tras un error como la violación de llave única (`23505`, la que provoca `Create` con un `Id` duplicado), la transacción queda **abortada** y cualquier consulta posterior falla con `current transaction is aborted`. Además, el rollback oculta bugs de `SaveChanges` |
| Base de datos plantilla (`CREATE DATABASE ... TEMPLATE`) | Más código y más SQL a mano; el beneficio de velocidad no se nota con 12 pruebas |

La base nueva por prueba se crea con `MigrateAsync()` y **no** con `EnsureCreatedAsync()` por dos razones: valida la **migración real** (la que generaste en A.6) en lugar de crear el esquema a partir del modelo, y Npgsql **crea la base de datos si no existe** como parte de `MigrateAsync`, así que no hace falta ejecutar `CREATE DATABASE` a mano.

#### Por qué cada proyecto levanta su propio contenedor

Cada proyecto de pruebas es un ensamblado distinto y `dotnet test` corre cada ensamblado en su propio proceso (`testhost`): no pueden compartir un `[SetUpFixture]`. Compartir un contenedor entre procesos exigiría `WithReuse(true)`, que en Testcontainers es una función **experimental**. Por eso cada proyecto levanta el suyo. Al correr `dotnet test EventsHub.slnx` hay entonces **dos contenedores `postgres:17-alpine` y dos contenedores Ryuk** en paralelo; no chocan porque Testcontainers asigna puertos aleatorios del host.

#### Por qué `PostgresFixture` vive en el namespace `...UnitTests.Events`

Un `[SetUpFixture]` de NUnit solo aplica a las pruebas de **su mismo namespace y de los namespaces hijos**. `PostgresFixture` se declara a propósito en `EventsHub.Application.UnitTests.Events`: así el contenedor se levanta solo para los handlers, y las pruebas de `EventsHub.Application.UnitTests.Mapping` siguen sin necesitar Docker. No lo muevas a la raíz del proyecto, o los mappers también arrancarían un contenedor.

### Paso E.1 — Paquetes (dos proyectos)

| Tipo | Terminal |
|------|----------|
| ✏️ `EventsHub.Api.UnitTests.csproj` y `EventsHub.Application.UnitTests.csproj` (los modifica `dotnet`) | PowerShell 7 · carpeta raíz del repo |

```powershell
dotnet add tests/EventsHub.Api.UnitTests package Testcontainers.PostgreSql --version 4.16.0
dotnet add tests/EventsHub.Application.UnitTests package Testcontainers.PostgreSql --version 4.16.0
```

✅ Esperado (una línea por proyecto): `info : PackageReference for package 'Testcontainers.PostgreSql' version '4.16.0' added to file '...EventsHub.Api.UnitTests.csproj'.` y lo mismo para `...EventsHub.Application.UnitTests.csproj`.

**Por qué solo este paquete en cada proyecto** (verificado en los `.csproj`): `EventsHub.Api.UnitTests` referencia Api, Application y Persistence; `EventsHub.Application.UnitTests` referencia Application, Domain y Persistence. En ambos casos `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 llega **transitivamente** por la referencia a `EventsHub.Persistence` (y con él el paquete `Npgsql`, que contiene `NpgsqlConnectionStringBuilder`, el namespace `Npgsql`). MediatR llega por `EventsHub.Application`. No hace falta agregar referencias de proyecto ni paquetes de inyección de dependencias.

🔎 Verifica:

```powershell
dotnet list tests/EventsHub.Api.UnitTests package --include-transitive | Select-String 'Npgsql|Testcontainers'
dotnet list tests/EventsHub.Application.UnitTests package --include-transitive | Select-String 'Npgsql|Testcontainers'
```

Para cada proyecto deben aparecer `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3` y `Testcontainers.PostgreSql 4.16.0` (y `Npgsql` como paquete transitivo).

⚠️ Si prefieres dependencias **explícitas** (opcional), agrega `Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3` con `dotnet add` a cada proyecto de pruebas. Si el `using Npgsql;` del Paso E.4 no se resuelve (`CS0246`), es la señal de que ese paquete no está llegando de forma transitiva: agrégalo así.

### Paso E.2 — `GlobalTestSetup` de Api.UnitTests (cambio mínimo)

| Tipo | Terminal |
|------|----------|
| ✏️ `tests/EventsHub.Api.UnitTests/GlobalTestSetup.cs` — **reemplaza TODO el contenido** | Ninguna (editor); verificación en PowerShell 7 · raíz del repo |

El archivo es corto, así que se muestra completo. Respecto a la versión con SQLite hay **cuatro cambios**; todo lo demás (el `AppDbContext` estático, `MigrateAsync`, `SeedDataAsync`, el namespace) queda igual:

1. `using Testcontainers.PostgreSql;`
2. el campo `private static PostgreSqlContainer _postgres = null!;`
3. al inicio de `Setup()`, crear y arrancar el contenedor; y `.UseNpgsql(_postgres.GetConnectionString())` en lugar de `.UseSqlite("Data source=eventshub.db")`
4. en `TearDown()`, después de liberar el contexto, `await _postgres.DisposeAsync();`

```csharp
using EventsHub.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventsHub.Api.UnitTests;

[SetUpFixture]
public class GlobalTestSetup
{
    private static PostgreSqlContainer _postgres = null!;

    public static AppDbContext AppDbContext { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        // Levanta PostgreSQL en un contenedor efímero (puerto aleatorio, credenciales generadas)
        _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _postgres.StartAsync();

        DbContextOptions<AppDbContext> options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;
        AppDbContext = new AppDbContext(options);

        // Aplica las migraciones REALES y siembra, igual que antes
        await AppDbContext.Database.MigrateAsync();
        await DbInitializer.SeedDataAsync(AppDbContext);
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await AppDbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
```

> `new PostgreSqlBuilder("postgres:17-alpine")` es la forma de las versiones recientes de Testcontainers 4.x (la que se fijó en E.1). No pude probar aquí otras versiones: si usas una anterior y ese constructor no existe, usa `new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build()`.

`EventsControllerTests.cs` **no se edita**: ya arma su propio `ServiceCollection`, usa `GlobalTestSetup.AppDbContext` y registra en `_ownedEventIds` los eventos que crea para borrarlos en `TearDown`. Como el contexto sigue siendo el mismo objeto estático, no necesita enterarse de que el motor ahora es PostgreSQL.

🔎 Verifica:

```powershell
dotnet build tests/EventsHub.Api.UnitTests
```

✅ Esperado: `Build succeeded.`

### Paso E.3 — `PostgresFixture` de Application.UnitTests

| Tipo | Terminal |
|------|----------|
| 🆕 `tests/EventsHub.Application.UnitTests/Events/PostgresFixture.cs` | Ninguna (editor de código) |

Ruta: `EventsHub/tests/EventsHub.Application.UnitTests/Events/PostgresFixture.cs`. Levanta **un** contenedor para todos los handlers y expone su cadena de conexión (que apunta a la base `postgres` del contenedor; `HandlerTestBase` la usa como plantilla para crear una base por prueba).

```csharp
using Testcontainers.PostgreSql;

namespace EventsHub.Application.UnitTests.Events;

// INTENCIONAL: este [SetUpFixture] vive en el namespace ...UnitTests.Events.
// NUnit solo lo ejecuta para las pruebas de ese namespace (y sus hijos), así que
// las pruebas de Mapping/ siguen sin necesitar Docker.
[SetUpFixture]
public class PostgresFixture
{
    private static PostgreSqlContainer _postgres = null!;

    /// <summary>Cadena de conexión del contenedor compartido por los handlers.</summary>
    public static string ConnectionString { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _postgres.DisposeAsync();
    }
}
```

🔎 Verifica: aún no compiles el proyecto completo; `HandlerTestBase` se arregla en el siguiente paso.

### Paso E.4 — `HandlerTestBase` sobre PostgreSQL

| Tipo | Terminal |
|------|----------|
| ✏️ `tests/EventsHub.Application.UnitTests/Events/HandlerTestBase.cs` — **reemplaza TODO el contenido** | Ninguna (editor); verificación en PowerShell 7 · raíz del repo |

Se quitan `using Microsoft.Data.Sqlite;` y la conexión `:memory:`. Se agrega `using Npgsql;` (el tipo `NpgsqlConnectionStringBuilder` vive en el namespace `Npgsql` y llega con el paquete transitivo de E.1). `NewEvent` y `SeedAsync` quedan **idénticos**, por eso los cinco archivos de pruebas no necesitan cambios.

```csharp
using EventsHub.Domain;
using EventsHub.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventsHub.Application.UnitTests.Events;

/// <summary>
/// Gives every test its own empty PostgreSQL database inside the container shared by the whole project
/// (see <see cref="PostgresFixture"/>), so handler tests never share state and never depend on the
/// seed data used by the API unit tests.
/// </summary>
public abstract class HandlerTestBase
{
    protected AppDbContext Context { get; private set; } = null!;

    [SetUp]
    public async Task BaseSetUp()
    {
        // Same server as the shared container, but a brand-new database named after a GUID.
        var connectionString = new NpgsqlConnectionStringBuilder(PostgresFixture.ConnectionString)
        {
            Database = $"test_{Guid.NewGuid():N}"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        Context = new AppDbContext(options);

        // MigrateAsync creates the database if it does not exist and applies the REAL migrations.
        await Context.Database.MigrateAsync();
    }

    [TearDown]
    public async Task BaseTearDown()
    {
        // Drop this test's database so nothing accumulates in the container, then release the context.
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }

    protected static Event NewEvent(string? id = null, string title = "Test event") => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Title = title,
        Description = "Test description",
        Category = "music",
        City = "Merida",
        Venue = "Test venue",
        Date = DateTime.UtcNow.AddDays(7),
    };

    /// <summary>
    /// Inserts an event with the given title and detaches it, so the handler under test
    /// loads the row from the database instead of from the change tracker.
    /// </summary>
    protected async Task<Event> SeedAsync(string title)
    {
        var @event = NewEvent(title: title);
        Context.Events.Add(@event);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        return @event;
    }
}
```

> `EnsureDeletedAsync` cierra las conexiones del *pool* de esa base antes de borrarla; por eso se llama **antes** de `DisposeAsync` sin problema. El nombre `test_<32 hex>` tiene 37 caracteres, por debajo del límite de 63 de PostgreSQL.

🔎 Verifica:

```powershell
dotnet build tests/EventsHub.Application.UnitTests
```

✅ Esperado: `Build succeeded.`

⚠️ Si falla:

- `CS0246 ... 'Npgsql'`: el paquete no llega de forma transitiva; agrega `Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3` al proyecto (ver E.1).
- `CS0246 ... 'Sqlite'` o `'SqliteConnection'`: quedó algún resto de la versión anterior; confirma que el archivo se reemplazó **completo**.

### Paso E.5 — Compilar toda la solución

| Tipo | Terminal |
|------|----------|
| ▶️ `dotnet build` | PowerShell 7 · carpeta raíz del repo |

```powershell
dotnet build EventsHub.slnx
```

✅ Esperado: `Build succeeded.` Con esto desaparecen los errores que dejó el Paso A.1 (en `GlobalTestSetup.cs` y en `HandlerTestBase.cs`).

### Paso E.6 — Qué podría comportarse distinto en PostgreSQL

Las pruebas pasaban con SQLite. Esto es lo que podría cambiar al pasar a PostgreSQL y por qué no es un problema (no hay que editar nada; es para que sepas qué mirar si una falla en tu primera corrida):

| Prueba / situación | Posible diferencia | Por qué no hay problema |
|--------------------|--------------------|--------------------------|
| `CreateEventTests.Handle_WhenIdAlreadyExists_ThrowsDbUpdateException` | SQLite y PostgreSQL reportan la llave duplicada con errores distintos | EF Core envuelve ambos en `DbUpdateException`; en PostgreSQL la causa interna es `PostgresException` con código `23505`. La prueba solo afirma `DbUpdateException`, así que se cumple igual |
| `FindAsync` en las pruebas de handlers y de controller | Podría devolver la entidad rastreada en lugar de leer la BD | `SeedAsync` ejecuta `ChangeTracker.Clear()` después de guardar, así que el handler va a la base de datos |
| `GetEventListTests` (0 filas y exactamente 3) | Con un contenedor compartido podrían verse filas de otras pruebas | Cada prueba tiene su **propia base de datos** vacía, por eso el conteo exacto sigue siendo válido |
| `Handle_WhenTokenIsAlreadyCancelled_ThrowsOperationCanceledException` | El comportamiento de cancelación depende del proveedor | El handler captura la cancelación de su bucle y sigue; luego `ToListAsync(cancellationToken)` lanza `OperationCanceledException` (o `TaskCanceledException`, que deriva de ella). `Throws.InstanceOf<OperationCanceledException>()` acepta ambas. Es el único caso que conviene **verificar en la primera corrida** |
| Api: `FirstAsync()` sin `OrderBy` | PostgreSQL no garantiza el orden de las filas | La prueba toma una fila cualquiera y compara el resultado **contra esa misma fila**, no contra una fila concreta del seed |
| Fechas (`Date`) | `timestamptz` guarda microsegundos; `DateTime.UtcNow` tiene precisión de 100 ns | Ninguna prueba compara fechas. Si agregas una que sí, usa `Is.EqualTo(esperado).Within(TimeSpan.FromMilliseconds(1))` |

### Paso E.7 — Ejecutar

| Tipo | Terminal |
|------|----------|
| ▶️ `dotnet test` (terminan solos, pueden tardar) | Terminal 1 · PowerShell 7 · carpeta raíz del repo |
| 🔎 `docker ps` durante la corrida | Terminal 2 · PowerShell 7 · carpeta raíz del repo |

Docker Desktop debe estar iniciado. Antes de la primera corrida, limpia los binarios de Api.UnitTests para eliminar el `eventshub.db` viejo de SQLite que pudo quedar en `bin`:

```powershell
dotnet clean tests/EventsHub.Api.UnitTests
```

**Tiempos aproximados (estimaciones, no medidas):** la primera corrida tarda entre 30 s y 2 min porque descarga `postgres:17-alpine` y `testcontainers/ryuk`; las siguientes, unos 5 a 10 s por proyecto.

**1. Application.UnitTests** (20 de mappers sin Docker + 12 de handlers con contenedor):

```powershell
dotnet test tests/EventsHub.Application.UnitTests
```

✅ Esperado al final (el espaciado exacto puede variar):

```text
Passed! - Failed: 0, Passed: 32, Skipped: 0, Total: 32
```

**2. Api.UnitTests** (8 de controller):

```powershell
dotnet test tests/EventsHub.Api.UnitTests
```

✅ Esperado:

```text
Passed! - Failed: 0, Passed: 8, Skipped: 0, Total: 8
```

**3. Toda la solución** (40 pruebas en total; `dotnet test` imprime un resumen por proyecto: 8 y 32):

```powershell
dotnet test EventsHub.slnx
```

Esta corrida levanta **dos contenedores en paralelo**. Si tu equipo va justo de memoria o prefieres ver los proyectos uno por uno, serializa con `-m:1` (verifícalo en tu SDK, porque el comportamiento de las opciones de paralelismo varía entre versiones):

```powershell
dotnet test EventsHub.slnx -m:1
```

Puede aparecer una **advertencia de licencia de MediatR**: es normal y no afecta el resultado.

🔎 Mientras corre (Terminal 2):

```powershell
docker ps
```

Verás un contenedor `postgres:17-alpine` y otro `testcontainers/ryuk` por cada proyecto que esté corriendo (**dos de cada uno** al correr la solución completa). Al terminar la corrida desaparecen solos.

**Filtros útiles:**

```powershell
# Solo mappers (20 pruebas, sin Docker)
dotnet test tests/EventsHub.Application.UnitTests --filter "FullyQualifiedName~EventsHub.Application.UnitTests.Mapping"

# Solo handlers (12 pruebas, con contenedor)
dotnet test tests/EventsHub.Application.UnitTests --filter "FullyQualifiedName~EventsHub.Application.UnitTests.Events"

# Un fixture de handlers
dotnet test tests/EventsHub.Application.UnitTests --filter "FullyQualifiedName~GetEventListTests"

# Un fixture de controller
dotnet test tests/EventsHub.Api.UnitTests --filter "FullyQualifiedName~EventsControllerTests"

# Una sola prueba
dotnet test tests/EventsHub.Api.UnitTests --filter "Name=GetEventDetailAsync_WhenEventDoesntExist_ThrowsActivityNotFound"
```

⚠️ Si falla:

- `Docker is either not running or misconfigured`: inicia Docker Desktop. Fallarán las 20 pruebas que usan el contenedor (8 de Api.UnitTests y 12 de handlers); las 20 de `Mapping/` siguen en verde porque no usan Docker.
- `The model for context 'AppDbContext' has pending changes` (`PendingModelChangesWarning`): cambiaste el modelo (por ejemplo, el conversor de A.5) y no regeneraste la migración. **Todas** las pruebas que usan `MigrateAsync` fallan con `InvalidOperationException`. Regenera la migración como en el Paso A.6 (después del A.5).
- `Cannot write DateTime with Kind=...`: repasa los pasos A.4 y A.5 y regenera la migración (A.6).
- `relation "Events" does not exist`: la migración no se aplicó; confirma que existe `src/EventsHub.Persistence/Migrations/*_InitialCreate.cs`.
- `CS0246`/`CS1061` por `Sqlite`/`UseSqlite` al compilar: alguno de los archivos de E.2 o E.4 no se reemplazó por completo.

### Notas finales de la Parte E

- **Nota terminológica para el reporte:** las pruebas de `Api.UnitTests` y los handlers de `Application.UnitTests` ahora son técnicamente de *integración* (usan una BD real). Se conservan los nombres de los proyectos por consistencia con el repo. Si el profesor lo pide, se puede marcar esas pruebas con `[Category("Integration")]` (opcional) y filtrar con `--filter "Category=Integration"`, o renombrar los proyectos.
- **El caso "no encontrado":** las pruebas de "no existe" (por ejemplo `GetEventDetailAsync_WhenEventDoesntExist_ThrowsActivityNotFound`) validan el comportamiento **real**: los handlers lanzan `new Exception("Activity not found")` y no hay middleware que lo convierta en 404. Es un buen punto de discusión: mejorar la API con un middleware de excepciones o `Result<T>` para devolver 404 y entonces afirmar `NotFound` en la prueba.
- **Limpieza si algo queda huérfano:** normalmente Ryuk borra los contenedores aunque interrumpas la corrida; los comandos de limpieza manual están en la sección 7.

**Commit sugerido:** `test(containers): run Api and handler tests against Testcontainers PostgreSQL`

---

## 7. Aspectos transversales

### 7.1 Seguridad básica

- Nunca versiones `.env` con contraseñas reales (ya está ignorado). Versiona solo `.env.example`.
- `ports: "5432:5432"` expone Postgres al host; elimínalo si no necesitas un cliente SQL externo.
- La API corre como usuario no-root **solo porque el Dockerfile incluye `USER $APP_UID`** (la imagen `aspnet` trae el usuario `app`, pero no lo activa por sí sola). El contenedor de nginx sirve solo archivos estáticos.

### 7.2 Problemas frecuentes

| Síntoma | Causa probable | Solución |
|---------|---------------|----------|
| `docker: command not found` | Docker Desktop no está iniciado / terminal vieja | Iniciar Docker Desktop y abrir terminal nueva |
| API arranca pero `/events` da 500 | Migración falló y el error solo está en el log | `docker compose logs api` y buscar `error occurred during database migration` |
| `Cannot write DateTime with Kind=Local/Unspecified to PostgreSQL 'timestamp with time zone'` | Se usó `DateTime.Now` o una fecha sin `Z`, y falta el conversor del Paso A.5 | Usar `DateTime.UtcNow`; revisar `AppDbContext.OnModelCreating` (A.5) |
| `password authentication failed for user "eventshub"` después de cambiar `POSTGRES_PASSWORD` | El volumen ya existe y conserva la contraseña con la que se creó la base; la variable solo se aplica la primera vez | `docker compose down -v` (borra los datos) y `docker compose up --build -d` |
| Front carga pero no muestra datos (error de red/CORS en consola) | `VITE_API_URL` apunta a otro puerto o el front no corre en `localhost:3000` | Reconstruir `web` con el `VITE_API_URL` correcto; el origen debe estar en el CORS de `Program.cs` |
| Recargar una ruta del SPA da 404 | Falta `try_files ... /index.html` en nginx | Revisar `web/nginx.conf` |
| `dotnet restore` falla en el build de la API intentando correr Husky | Falta `ENV HUSKY=0` | Ver Paso B.2 |
| Pruebas: `Docker is either not running or misconfigured` | Docker Desktop apagado. Fallan las 20 pruebas que usan contenedor (8 de `Api.UnitTests` y 12 de handlers en `Application.UnitTests`); las 20 de `Mapping/` siguen en verde porque no usan Docker | Iniciar Docker Desktop y repetir `dotnet test` |
| `dotnet build src/EventsHub.Api` falla con `CS1061 ... "UseSqlite"` en `Program.cs` | Compilaste antes de terminar el Paso A.3 (el paquete de SQLite ya se quitó en A.1) | Haz el Paso A.3 (`UseSqlite` → `UseNpgsql`) y vuelve a compilar |
| `dotnet build EventsHub.slnx` falla con `UseSqlite` no encontrado en `GlobalTestSetup.cs` (Api.UnitTests) o con `CS0246` por `Microsoft.Data.Sqlite` en `HandlerTestBase.cs` (Application.UnitTests) | Aún no se hizo la Parte E: los dos proyectos de pruebas usaban SQLite y el paquete se quitó en A.1 | Terminar la Parte E (pasos E.2 a E.4), o compilar solo la API con `dotnet build src/EventsHub.Api` |
| Pruebas: `InvalidOperationException` con `The model for context 'AppDbContext' has pending changes` (`PendingModelChangesWarning`) | Cambiaste el modelo (p. ej. el conversor de A.5) y no regeneraste la migración; **todas** las pruebas que usan `MigrateAsync` fallan (también la API al arrancar) | Regenerar la migración (Paso A.6) después de cualquier cambio del modelo (A.5) |
| Pruebas: `CS0246` por `Npgsql` en `HandlerTestBase.cs` | El paquete `Npgsql` no llega de forma transitiva por `EventsHub.Persistence` | `dotnet add tests/EventsHub.Application.UnitTests package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3` |
| Cambio de código en la API no se refleja | Imagen vieja | `docker compose up --build -d` |
| (Parte F) `Bind for 0.0.0.0:443 failed: port is already allocated` (o puerto 80) | Otro programa usa 80/443 (IIS, otro servidor local) | `Get-NetTCPConnection -LocalPort 443,80`, detén ese programa y repite |
| (Parte F) El navegador muestra `NET::ERR_CERT_AUTHORITY_INVALID` | La CA de mkcert no está instalada en el navegador (p. ej. Firefox) | `mkcert -install`; Firefox requiere además `certutil`/NSS (ver F.1) |
| (Parte F) `502 Bad Gateway` en `https://localhost/api/...` | La API aún no arranca o el proxy no la alcanza | `docker compose -f docker-compose.yml -f docker-compose.https.yml logs proxy api` |
| (Parte F) La SPA sigue llamando a `http://localhost:5000` | Se levantó sin `--build`; `VITE_API_URL` se incrusta en el build | Repetir con `up --build -d` |
| (Parte F) `additional property ports ... !reset` / error al interpretar el overlay | Docker Compose anterior a 2.24 | Actualizar Docker Desktop; comprobar con `docker compose version` |

**Contenedores de pruebas (Testcontainers):** Testcontainers lanza un contenedor auxiliar, **Ryuk** (`testcontainers/ryuk`), que borra los contenedores de la corrida aunque interrumpas `dotnet test`. Al correr `dotnet test EventsHub.slnx` verás **dos** `postgres:17-alpine` y **dos** `ryuk` (uno por proyecto de pruebas) mientras dura la corrida. Si Ryuk no puede usarse en tu entorno (algunas redes corporativas o configuraciones de Docker), puedes desactivarlo con la variable de entorno `TESTCONTAINERS_RYUK_DISABLED=true`; en ese caso **tú** debes limpiar. Para ver y borrar contenedores huérfanos:

```powershell
docker ps -a --filter label=org.testcontainers=true
docker rm -f $(docker ps -aq --filter label=org.testcontainers=true)
```

### 7.3 Qué NO cubre esta guía (posibles extensiones)

- Hot-reload en contenedores (`docker compose watch` / `docker-compose.override.yml` para desarrollo).
- CI (GitHub Actions) ejecutando `dotnet test`: los runners de Ubuntu ya traen Docker, así que Testcontainers funciona sin configuración extra.
- Publicar imágenes en un registro (Docker Hub / GHCR).
- HTTPS y proxy inverso como único punto de entrada: ver la **Parte F (opcional)**.

---

## 8. Checklist de entrega

> Los pasos de la Parte F son opcionales y no forman parte de esta lista.

- [ ] `docker compose up --build -d` levanta `db` (healthy), `api` y `web` sin intervención.
- [ ] `http://localhost:3000` muestra las actividades obtenidas de la API en contenedor.
- [ ] Los datos persisten tras `docker compose down` + `up`.
- [ ] No queda ninguna referencia a SQLite en código fuente y pruebas: `git grep -i sqlite -- src tests` no devuelve nada. Una vez aplicadas las Partes A y E, lo único que seguirá apareciendo son tres README que **debes actualizar** a mano: `src/EventsHub.Persistence/README.md` (menciona `UseSqlite`, el paquete `Microsoft.EntityFrameworkCore.Sqlite`, `Date` como `TEXT`, `DateTime.Now`; debe decir PostgreSQL/Npgsql: `UseNpgsql`, `timestamp with time zone`, `DateTime.UtcNow`, el `OnModelCreating` de A.5), `src/EventsHub.Api/README.md` (menciona "backed by SQLite", `UseSqlite(ConnectionStrings:SqliteConnection)`, el archivo `eventshub.db` y pruebas "against a real SQLite `AppDbContext`"; debe decir `UseNpgsql(ConnectionStrings:DefaultConnection)` y contenedor PostgreSQL con Testcontainers) y `src/EventsHub.Domain/README.md` (línea 12, "matching the SQLite schema").
- [ ] `dotnet test tests/EventsHub.Api.UnitTests` pasa (8 pruebas) usando un contenedor PostgreSQL efímero (visible en `docker ps` durante la corrida).
- [ ] `dotnet test tests/EventsHub.Application.UnitTests` pasa (32 pruebas: 20 de `Mapping/` sin Docker y 12 de handlers con una base de datos nueva por prueba sobre un contenedor compartido).
- [ ] `dotnet test EventsHub.slnx` pasa (40 pruebas en total; levanta dos contenedores en paralelo).
- [ ] `dotnet build EventsHub.slnx` compila completo.
- [ ] `.env` **no** está en git; `.env.example` sí.
- [ ] Colección Bruno: crea el entorno `tests/EventsHub.IntegrationTests/environments/docker.yml` con la API en contenedor. El formato se copió del `local.yml` existente (que está versionado en git):

  ```yaml
  name: docker
  variables:
    - name: baseUrl
      value: http://localhost:5000/api/v1
      description: ""
  ```

- [ ] `CLAUDE.md` actualizado: SQLite → PostgreSQL (`UseNpgsql`, Npgsql), comandos de Docker/Compose y la nueva convención de pruebas (Testcontainers). Concretamente: (a) comandos de prueba con las rutas reales `tests/EventsHub.Api.UnitTests` y `tests/EventsHub.Application.UnitTests`; (b) quita la nota de que `Api.UnitTests` "no compila"; (c) el filtro de ejemplo `GetEventDetailAsync_WhenEventDoesntExist_ReturnsNotFound` **deja de existir**: el nombre real es `GetEventDetailAsync_WhenEventDoesntExist_ThrowsActivityNotFound`; (d) `EventsHub.Application` ya **no** es un proyecto vacío (tiene los handlers de MediatR que usan los controllers); (e) convención de pruebas: Api.UnitTests usa un contenedor compartido sembrado una vez (`GlobalTestSetup`) y borra lo que crea con `_ownedEventIds`; los handlers de Application.UnitTests usan una base de datos vacía por prueba dentro de un contenedor compartido (`PostgresFixture` + `HandlerTestBase`); `Mapping/` no usa BD ni Docker. Las ediciones concretas propuestas están al final de esta lista (sección "CLAUDE.md del repo").

### Tabla resumen de archivos

| Icono | Archivo | Paso |
|-------|---------|------|
| ✏️ | `src/EventsHub.Persistence/EventsHub.Persistence.csproj` | A.1 |
| ✏️ | `src/EventsHub.Api/appsettings.json` | A.2 |
| ✏️ | `src/EventsHub.Api/appsettings.Development.json` | A.2 |
| ✏️ | `src/EventsHub.Api/Program.cs` | A.3 |
| ✏️ | `src/EventsHub.Persistence/DbInitializer.cs` | A.4 |
| ✏️ | `src/EventsHub.Persistence/AppDbContext.cs` | A.5 |
| 🆕 | `src/EventsHub.Persistence/Migrations/*` (regenerados) | A.6 |
| 🆕 | `.dockerignore` | B.1 |
| 🆕 | `src/EventsHub.Api/Dockerfile` | B.2 |
| 🆕 | `web/.dockerignore` | C.1 |
| 🆕 | `web/nginx.conf` | C.2 |
| 🆕 | `web/Dockerfile` | C.3 |
| 🆕 | `.env.example` (y `.env` local, no versionado) | D.1 |
| 🆕 | `docker-compose.yml` | D.2 |
| ✏️ | `tests/EventsHub.Api.UnitTests/EventsHub.Api.UnitTests.csproj` | E.1 |
| ✏️ | `tests/EventsHub.Application.UnitTests/EventsHub.Application.UnitTests.csproj` | E.1 |
| ✏️ | `tests/EventsHub.Api.UnitTests/GlobalTestSetup.cs` | E.2 |
| 🆕 | `tests/EventsHub.Application.UnitTests/Events/PostgresFixture.cs` | E.3 |
| ✏️ | `tests/EventsHub.Application.UnitTests/Events/HandlerTestBase.cs` | E.4 |
| ✏️ | `src/EventsHub.Persistence/README.md`, `src/EventsHub.Api/README.md`, `src/EventsHub.Domain/README.md`, `CLAUDE.md` | Checklist |
| 🆕 | `tests/EventsHub.IntegrationTests/environments/docker.yml` | Checklist |
| ✏️ | `.gitignore` (agrega `certs/`) | F.2 (opcional) |
| 🆕 | `proxy/Caddyfile` | F.3 (opcional) |
| 🆕 | `docker-compose.https.yml` | F.4 (opcional) |
| ✏️ | `src/EventsHub.Api/Program.cs` (solo opción explícita) | F.6 (opcional) |

---

## 9. Parte F (OPCIONAL) — HTTPS con proxy inverso (Caddy + mkcert)

> **Esta parte es OPCIONAL y no afecta a las Partes A–E.** Puedes entregar el proyecto sin hacerla. Todo se agrega con un archivo *overlay* de Compose: para volver al modo HTTP basta con no usarlo.
>
> **Al terminar esta parte tendrás…** `https://localhost/` sirviendo la SPA y `https://localhost/api/*` sirviendo la API, con un certificado de confianza (sin advertencias del navegador) y sin CORS.
>
> **Archivos tocados:**
> - ✏️ `.gitignore` (agrega `certs/`)
> - 🆕 `certs/localhost.pem` y `certs/localhost-key.pem` (generados con mkcert, **no** se versionan)
> - 🆕 `proxy/Caddyfile`
> - 🆕 `docker-compose.https.yml`
> - ✏️ `src/EventsHub.Api/Program.cs` (solo si eliges la opción explícita de F.6)

**Patrón de mismo origen:** un único punto de entrada HTTPS reparte el tráfico por ruta. La SPA usa una URL **relativa** (`/api/v1`), así que navegador, SPA y API comparten origen (`https://localhost`) y **no hay CORS**.

```text
Navegador (host)
   │  https://localhost  (443)        http://localhost (80) ──► redirige 308 a https
   ▼
[ proxy ]  Caddy + certificado de mkcert
   │
   ├── /api/*  ──► [ api ]  ASP.NET Core  (api:8080, solo red interna)
   │                  │
   │                  ▼
   │               [ db ]  PostgreSQL    (sin puerto publicado)
   │
   └── /*      ──► [ web ]  nginx + build de React  (web:80, solo red interna)
```

En este modo solo el `proxy` publica puertos (80 y 443): `api`, `web` y `db` quedan accesibles únicamente dentro de la red de Compose.

### Paso F.1 — Instalar mkcert y generar el certificado

| Tipo | Terminal |
|------|----------|
| ▶️ comandos (terminan solos; `mkcert -install` abre un diálogo de Windows) | PowerShell 7 · carpeta raíz del repo |

```powershell
winget install FiloSottile.mkcert
```

**Cierra y vuelve a abrir la terminal** para que `mkcert` quede en el `PATH`. Luego:

```powershell
mkcert -install
```

Windows mostrará un diálogo de seguridad para instalar la CA local: **acéptalo**.

```powershell
New-Item -ItemType Directory -Force certs | Out-Null
mkcert -cert-file certs/localhost.pem -key-file certs/localhost-key.pem localhost 127.0.0.1 ::1
```

✅ Esperado: `mkcert -install` indica que la CA local quedó instalada en el almacén de confianza del sistema, y el segundo comando muestra algo similar a `Created a new certificate valid for the following names` (`localhost`, `127.0.0.1`, `::1`), la ruta de los dos archivos y su fecha de expiración.

🔎 Verifica:

```powershell
Get-ChildItem certs
```

Deben aparecer `localhost.pem` y `localhost-key.pem`.

Notas:

- La CA de mkcert es **distinta** de la que usa `vite-plugin-mkcert`; esta última sigue sirviendo para `npm run dev` y no se ve afectada.
- **Firefox** usa su propio almacén de certificados: necesita `certutil` (paquete NSS) disponible cuando ejecutas `mkcert -install`. Con Chrome/Edge no hace falta nada más.
- La **llave privada** `localhost-key.pem` es un secreto local: nunca la subas a git (siguiente paso).

⚠️ Si `mkcert` no se reconoce: no reabriste la terminal, o reinicia la sesión de Windows.

### Paso F.2 — Ignorar `certs/` en git

| Tipo | Terminal |
|------|----------|
| ✏️ `.gitignore` (raíz del repo) | PowerShell 7 · carpeta raíz del repo (o editor) |

`.dockerignore` ya contiene `certs` (Paso B.1), por lo que los certificados nunca entran al contexto de build.

**Ubicación:** al **final** de `.gitignore`. Agrega estas líneas (el archivo termina sin salto de línea; asegúrate de escribirlas en líneas nuevas):

```gitignore
# mkcert certificates (local only)
certs/
```

Con PowerShell:

```powershell
Add-Content -Path .gitignore -Value "`n# mkcert certificates (local only)`ncerts/"
```

🔎 Verifica:

```powershell
git check-ignore -v certs/localhost-key.pem
```

✅ Esperado: una línea del estilo `.gitignore:<n>:certs/	certs/localhost-key.pem`.

### Paso F.3 — Configuración del proxy (Caddy)

| Tipo | Terminal |
|------|----------|
| 🆕 `proxy/Caddyfile` | Ninguna (editor de código; crea la carpeta `proxy`) |

Ruta: `EventsHub/proxy/Caddyfile` (sin extensión).

```caddyfile
{
    auto_https disable_certs
}

localhost {
    tls /certs/localhost.pem /certs/localhost-key.pem

    # NO usar handle_path: la API espera el prefijo /api/v1
    handle /api/* {
        reverse_proxy api:8080
    }

    handle {
        reverse_proxy web:80
    }
}
```

Cómo se lee:

- `auto_https disable_certs`: Caddy **no** intenta obtener certificados por su cuenta; usa los de mkcert (el redirect HTTP→HTTPS sigue activo). Si esta opción diera problemas con tu versión de Caddy, **quita el bloque global** (las 3 primeras líneas): con `tls <cert> <key>` explícito Caddy usa tus archivos de todos modos.
- `handle` (y no `handle_path`) **conserva** el prefijo `/api`: la API responde en `/api/v1/...`.
- Caddy agrega por defecto `X-Forwarded-For` y `X-Forwarded-Proto` hacia el upstream.

#### Variante opcional: nginx como proxy

Si prefieres nginx (mismo patrón), crea `proxy/nginx.conf`:

```nginx
server {
  listen 80;
  server_name localhost;
  return 308 https://$host$request_uri;
}

server {
  listen 443 ssl;
  http2 on;
  server_name localhost;

  ssl_certificate     /certs/localhost.pem;
  ssl_certificate_key /certs/localhost-key.pem;

  proxy_set_header Host              $host;
  proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
  proxy_set_header X-Forwarded-Proto $scheme;

  # Sin "/" final en proxy_pass: así se conserva el prefijo /api/
  location /api/ {
    proxy_pass http://api:8080;
  }

  location / {
    proxy_pass http://web:80;
  }
}
```

Y en F.4, reemplaza el servicio `proxy` del overlay por:

```yaml
  proxy:
    image: nginx:1.27-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./proxy/nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - ./certs:/certs:ro
    depends_on:
      - web
      - api
```

(con nginx ya no se necesita el volumen `caddy-data` ni su declaración). El resto de la Parte F aplica igual; solo los logs vienen de nginx en vez de Caddy.

### Paso F.4 — Overlay de Compose para HTTPS

| Tipo | Terminal |
|------|----------|
| 🆕 `docker-compose.https.yml` | Ninguna (editor de código) |
| ▶️ comandos `docker compose` | PowerShell 7 · carpeta raíz del repo |

Requiere **Docker Compose ≥ 2.24** (por la etiqueta `!reset`). Compruébalo:

```powershell
docker compose version
```

Ruta del archivo: `EventsHub/docker-compose.https.yml`. Se **combina** con `docker-compose.yml` (no lo reemplaza):

```yaml
services:
  db:
    ports: !reset []                 # la BD deja de publicarse en el host

  api:
    ports: !reset []                 # ya no se publica el 5000
    expose:
      - "8080"
    environment:
      # Hace que ASP.NET Core confíe en X-Forwarded-For/Proto del proxy (ver F.6)
      ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"

  web:
    build:
      args:
        VITE_API_URL: /api/v1        # URL relativa: mismo origen que la SPA
    ports: !reset []                 # ya no se publica el 3000
    expose:
      - "80"

  proxy:
    image: caddy:2.10-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./proxy/Caddyfile:/etc/caddy/Caddyfile:ro
      - ./certs:/certs:ro
      - caddy-data:/data
    depends_on:
      - web
      - api

volumes:
  caddy-data:
```

Levantar en modo HTTPS (primero detén lo que estaba corriendo; la BD y su volumen se conservan):

```powershell
docker compose down
docker compose -f docker-compose.yml -f docker-compose.https.yml up --build -d
docker compose -f docker-compose.yml -f docker-compose.https.yml ps
```

> El `--build` es **obligatorio**: `VITE_API_URL` se incrusta en el JavaScript durante el build de la SPA. Sin `--build` Compose reutilizaría la imagen anterior (que apunta a `http://localhost:5000`).

✅ Esperado en `ps`: `db` (healthy), `api`, `web` y `proxy` con `Up`; solo `proxy` muestra puertos publicados (`0.0.0.0:80->80/tcp`, `0.0.0.0:443->443/tcp`).

🔎 Verifica la configuración combinada (debe verse `VITE_API_URL: /api/v1` y ningún `5000`/`3000`/`5432` publicado):

```powershell
docker compose -f docker-compose.yml -f docker-compose.https.yml config
```

⚠️ Si dice que `!reset` no se reconoce, actualiza Docker Desktop (Compose < 2.24). Si Caddy no encuentra los certificados, revisa que existan `certs/localhost.pem` y `certs/localhost-key.pem` (F.1).

### Paso F.5 — Qué NO cambia en la SPA ni en el CORS

| Tipo | Terminal |
|------|----------|
| Solo lectura (no se edita nada) | — |

- `web/src/lib/api/agent.ts` **no cambia**: sigue usando `baseURL: import.meta.env.VITE_API_URL`; lo único que cambia es el valor con el que se construye la imagen (`/api/v1`).
- `web/.env.development` **no cambia**: `npm run dev` sigue usando `https://localhost:5001/api/v1`.
- **No hace falta CORS** en este modo: el navegador ve un solo origen (`https://localhost`). El `UseCors` actual de `Program.cs` se queda para el desarrollo local (SPA en `localhost:3000` → API en otro puerto) y simplemente no interviene aquí.

### Paso F.6 — `Program.cs`: encabezados reenviados (X-Forwarded-*)

| Tipo | Terminal |
|------|----------|
| Opción recomendada: ninguna acción | — |
| Opción explícita (didáctica): ✏️ `src/EventsHub.Api/Program.cs` | Ninguna (editor de código) |

Detrás de un proxy, la API ve peticiones HTTP internas. Para que sepa que el cliente original usó HTTPS (y su IP real) debe leer `X-Forwarded-For` y `X-Forwarded-Proto`.

**Opción recomendada — ningún cambio de código.** La variable `ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"` que ya está en el overlay (F.4) activa ese comportamiento sin tocar `Program.cs`. **No agregues `UseHttpsRedirection`**: la API habla HTTP dentro de la red y el proxy ya redirige 80→443.

**Opción explícita (para mostrar el mecanismo en clase).** Edita `src/EventsHub.Api/Program.cs`:

1. **Busca** la primera línea del archivo (`using EventsHub.Application.Core;`) y **agrega arriba**:

   ```csharp
   using Microsoft.AspNetCore.HttpOverrides;
   ```

2. **Busca** `var app = builder.Build();` y **reemplaza por** (el bloque `Configure` va **antes** de `Build()`):

   ```csharp
   builder.Services.Configure<ForwardedHeadersOptions>(o =>
   {
       o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
       // El proxy está en otra IP de la red de Docker: no restringir a loopback
       o.KnownIPNetworks.Clear();
       o.KnownProxies.Clear();
   });

   var app = builder.Build();

   app.UseForwardedHeaders();
   ```

   (`app.UseForwardedHeaders();` debe ser lo **primero** del pipeline, antes de `app.UseCors(...)`.)

Si eliges esta opción, **quita** `ASPNETCORE_FORWARDEDHEADERS_ENABLED` del overlay para no configurarlo dos veces, y reconstruye (`up --build -d`).

> `KnownIPNetworks` es la propiedad vigente en .NET 10; no pude ejecutar este código aquí, así que verifica con `dotnet build src/EventsHub.Api` (debe dar `Build succeeded.`).

### Paso F.7 — Verificación y vuelta al modo HTTP

| Tipo | Terminal |
|------|----------|
| 🔎 comprobaciones | PowerShell 7 · carpeta raíz del repo (y navegador) |

| Qué | Cómo | Resultado esperado |
|-----|------|--------------------|
| Puertos 80/443 libres (antes de levantar) | `Get-NetTCPConnection -LocalPort 80,443 -ErrorAction SilentlyContinue` | Sin salida. Si hay filas, otro programa los usa |
| Redirección HTTP → HTTPS | `curl.exe -sI http://localhost` | Línea de estado `308` y `Location: https://localhost/` |
| API por HTTPS, sin `-k` | `curl.exe -s https://localhost/api/v1/events` | JSON con los eventos |
| Certificado de confianza | Abrir `https://localhost` y pulsar el candado | Sin advertencias; certificado emitido por la CA de mkcert |
| SPA y ruta profunda | Abrir `https://localhost/activities`, entrar a una actividad (`https://localhost/activities/<id>`) y pulsar F5 | Se muestra el detalle; sin 404 al recargar |
| Sin CORS | DevTools → pestaña *Network*, recargar | Las llamadas a `/api/v1/events` van a `https://localhost` y **no** hay peticiones `OPTIONS` (preflight) |
| Escritura | Crear una actividad en `https://localhost/createActivity` | Se guarda y abre su detalle (confirma que A.5 funciona) |
| API ya no expuesta | `curl.exe -s http://localhost:5000/api/v1/events` | **Falla** (`Failed to connect` / `Connection refused`) |
| Logs del proxy | `docker compose -f docker-compose.yml -f docker-compose.https.yml logs proxy` | Entradas de las peticiones, sin errores de certificado |

⚠️ Si `curl.exe` se queja de la comprobación de revocación del certificado (`CRYPT_E_NO_REVOCATION_CHECK` / `revocation function was unable to check revocation`), repite con `--ssl-no-revoke`; es una particularidad de `curl` con Schannel y certificados locales, no un problema del proxy.

**Volver al modo HTTP (Partes A–E):**

```powershell
docker compose -f docker-compose.yml -f docker-compose.https.yml down
docker compose up --build -d
```

> De nuevo `--build`: ambas variantes comparten el nombre de imagen de `web`, y la que quedó construida tiene `VITE_API_URL=/api/v1`. Reconstruir restituye `http://localhost:5000/api/v1`. Los datos de la BD se conservan (mismo volumen).

**Commit sugerido:** `feat(containers): add optional HTTPS reverse proxy (Caddy + mkcert)`
