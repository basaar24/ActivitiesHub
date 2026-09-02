# Manual setup — OpenAPI doc generation + typed client for the EventsHub API

This is the full procedure for standing up OpenAPI support for the EventsHub
backend: a standalone doc-generation host (`EventsHub.OpenApi`), an NSwag
codegen config (`nswag/`), and the generated OpenAPI document (`openapi/`).

It is a two-part process: **(1) scaffold the pieces by hand**, then **(2) run the
generation pipeline** to populate the generated content.

All paths below are relative to the **repository root** (the folder containing
`EventsHub.slnx`). The existing backend projects live under `src/`.

Repo layout produced by this procedure:

| Path | Purpose |
|---|---|
| `src/EventsHub.OpenApi/` | Standalone host that serves the live Swagger doc/UI |
| `nswag/EventsHub.nswag` | Checked-in NSwag codegen config |
| `openapi/EventsHub.v1.json` | Generated OpenAPI document (never hand-edited) |
| `src/EventsHub.OpenApi/Generated/EventsHubRpcClient.generated.cs` | Generated C# client (never hand-edited) |

---

## Part 1 — Manually scaffold the pieces

### Step 1: Create `src/EventsHub.OpenApi/` (the standalone doc-generation host)

Run from the repo root:

```powershell
dotnet new web -n EventsHub.OpenApi -o src/EventsHub.OpenApi
dotnet sln EventsHub.slnx add src/EventsHub.OpenApi/EventsHub.OpenApi.csproj --solution-folder src
```

> Note: `EventsHub.slnx` currently lists only `EventsHub.Api`,
> `EventsHub.Application`, `EventsHub.Domain`, and `EventsHub.Persistence` (all
> under the `/src/` solution folder). Adding `EventsHub.OpenApi` to the
> solution is optional — the pipeline in Part 2 targets the `.csproj` directly —
> but keeping it in the solution makes it build with
> `dotnet build EventsHub.slnx`.

Then edit `src/EventsHub.OpenApi/EventsHub.OpenApi.csproj` to match this
shape:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" Version="10.0.11" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="NSwag.AspNetCore" Version="14.7.1" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\EventsHub.Api\EventsHub.Api.csproj" />
  </ItemGroup>

</Project>
```

- `ProjectReference` to `EventsHub.Api` is what lets this host discover the real
  controllers (`WeatherForecastController`, and whatever you add later) via an
  MVC application part.
- `NSwag.AspNetCore` serves the live Swagger doc/UI.
- `Moq` is only needed if a controller gains constructor dependencies that must
  be registered for the host to start (see Step 2). The current
  `WeatherForecastController` has no constructor parameters, so `Moq` is
  effectively unused today — keep it for forward compatibility or drop it.

Then replace `Program.cs` with a minimal host that:

1. Registers `AddOpenApiDocument(...)` with a fixed `DocumentName` / `Title` /
   `Version`.
2. Loads the `EventsHub.Api` assembly and adds it as an MVC application part with
   `AddControllersAsServices()`.
3. Registers any controller-constructor dependencies. The current EventsHub
   controllers take no constructor parameters, so **no mocks are required
   today**. Only add `services.AddSingleton(new Mock<INewService>().Object);`
   lines if a future controller takes real constructor parameters (or resolve
   dependencies lazily from `HttpContext.RequestServices` in a base controller,
   as is common once MediatR is introduced).
4. Calls `app.UseOpenApi()` / `app.UseSwaggerUi()` / `app.MapControllers()`.

Note on the assembly handle: EventsHub's `Program` is an implicit top-level class
in the global namespace and is `internal`, so `typeof(Program)` is not usable
from this project. Reference any **public** type from the `EventsHub.Api`
assembly instead — a controller works:

```csharp
using System.Reflection;
using EventsHub.Api.Controllers;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services
    .AddOpenApiDocument(document =>
    {
        document.DocumentName = "EventsHub";
        document.Title = "EventsHubV1"; // Official interface name. No spaces. PascalCase.
        document.Version = "1.0.0";
        document.DefaultResponseReferenceTypeNullHandling =
            NJsonSchema.Generation.ReferenceTypeNullHandling.NotNull;
    });

var pluginAssembly = Assembly.GetAssembly(typeof(WeatherForecastController));
services.AddMvc()
    .AddApplicationPart(pluginAssembly!)
    .AddControllersAsServices()
    .AddNewtonsoftJson(options =>
    {
        // Match the API's camelCase JSON output.
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    });

var app = builder.Build();
app.UseOpenApi();
app.UseSwaggerUi();
app.MapControllers();
app.Run();
```

> The `DocumentName` ("EventsHub") is what forms the Swagger JSON URL in
> Part 2: `/swagger/EventsHub/swagger.json`.

Finally, set `src/EventsHub.OpenApi/Properties/launchSettings.json` to this
project's name and a free port pair (the main API uses `https://localhost:5001`):

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "EventsHub.OpenApi": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "https://localhost:5011;http://localhost:5010"
    }
  }
}
```

### Step 2: Create `openapi/` (generated output folder)

```powershell
mkdir openapi
```

Leave it empty — `EventsHub.v1.json` is pure generated output (Part 2) and must
never be hand-authored.

### Step 3: Create `nswag/EventsHub.nswag` (checked-in codegen config)

```powershell
mkdir nswag
```

This file **is** hand-authored / checked in (unlike the JSON doc or the
generated `.cs`). Create `nswag/EventsHub.nswag` pointing at the not-yet-existing
OpenAPI doc as input and the `src/EventsHub.OpenApi/Generated/` folder as
output (paths are relative to the `nswag/` folder):

```json
{
  "runtime": "Net100",
  "documentGenerator": {
    "fromDocument": {
      "url": "../openapi/EventsHub.v1.json"
    }
  },
  "codeGenerators": {
    "openApiToCSharpClient": {
      "generateClientClasses": true,
      "generateClientInterfaces": true,
      "generateExceptionClasses": true,
      "exceptionClass": "ApiException",
      "className": "{controller}RpcClient",
      "operationGenerationMode": "MultipleClientsFromOperationId",
      "namespace": "EventsHub.OpenApi.Client",
      "jsonLibrary": "NewtonsoftJson",
      "output": "../src/EventsHub.OpenApi/Generated/EventsHubRpcClient.generated.cs"
    }
  }
}
```

Two gotchas:

- `"runtime"` must match the SDK you actually run NSwag with — `"Net100"` for
  `net10.0`. NSwag throws `InvalidOperationException` if it doesn't match the
  running process.
- `generateExceptionClasses` must be `true`. Leaving it `false` compiles fine
  against an empty controller surface but breaks once real operations exist and
  the generated client references an `ApiException` type that was never emitted.

### Step 4: Register the NSwag CLI as a local tool

Run from the repo root (there is no `.config/dotnet-tools.json` in this repo
yet):

```powershell
dotnet new tool-manifest
dotnet tool install nswag.consolecore --version 14.7.1
dotnet tool restore
```

This produces `.config/dotnet-tools.json` with an `nswag` command — a local (not
global) tool, so the pipeline is reproducible per-machine / CI without mutating
global state.

---

## Part 2 — Populate the generated content

Run from the repo root.

1. **Start the host** (separate terminal, or background it):

```powershell
dotnet run --project src/EventsHub.OpenApi/EventsHub.OpenApi.csproj --no-launch-profile --urls http://127.0.0.1:5011
```

Wait for `Now listening on: http://127.0.0.1:5011`.

2. **Fetch the document into the `openapi/` folder:**

```powershell
Invoke-WebRequest -Uri http://127.0.0.1:5011/swagger/EventsHub/swagger.json -OutFile src/openapi/EventsHub.v1.json
```

3. **Stop the host** (so it doesn't lock its own `.exe` during the rebuild):

```powershell
Get-Process -Name "EventsHub.OpenApi" | Stop-Process -Force
```

4. **Generate the C# client** from that JSON, using `nswag/EventsHub.nswag`:

```powershell
dotnet tool run nswag run src/nswag/EventsHub.nswag
```

If `dotnet tool run nswag` reports the tool isn't available even after `dotnet tool restore`, invoke the NSwag console DLL directly instead:

```powershell
dotnet "$env:NUGET_PACKAGES\nswag.consolecore\14.7.1\tools\net10.0\any\dotnet-nswag.dll" run src\nswag\API.nswag
```

This writes
`src/EventsHub.OpenApi/Generated/EventsHubRpcClient.generated.cs`.

5. **Rebuild the solution to confirm everything compiles:**

```powershell
dotnet build EventsHub.slnx
```

---

## Summary of what gets created vs. hand-maintained

| Path | Origin | Maintenance |
|---|---|---|
| `src/EventsHub.OpenApi/*.csproj`, `Program.cs`, `launchSettings.json` | Manual (Part 1) | Hand-edit when adding new controller dependencies to register |
| `nswag/EventsHub.nswag` | Manual (Part 1) | Hand-edit only for codegen config changes (e.g. runtime bump) |
| `openapi/EventsHub.v1.json` | Generated (Part 2, step 2) | **Never hand-edit** — overwritten by the pipeline |
| `src/EventsHub.OpenApi/Generated/*.generated.cs` | Generated (Part 2, step 4) | **Never hand-edit** — overwritten by the pipeline |

---

## Current state on this branch

None of the pieces exist yet — `src/EventsHub.OpenApi/`, `nswag/`,
`openapi/`, and `.config/dotnet-tools.json` are all absent. Start from Part 1,
Step 1 and work straight through.

When wiring `Program.cs`, remember EventsHub's differences from a typical
Clean-Architecture template:

- The API's `Program` class is an internal top-level program in the global
  namespace — use `Assembly.GetAssembly(typeof(WeatherForecastController))` (or
  any other public `EventsHub.Api` type), not `typeof(Program)`.
- There is no `BaseApiController` and no MediatR yet, so no `IMediator`
  registration or mocks are needed. Revisit Step 2's dependency-registration
  guidance once controllers gain constructor parameters.
