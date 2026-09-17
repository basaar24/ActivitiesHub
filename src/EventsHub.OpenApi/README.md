# EventsHub.OpenApi

A standalone ASP.NET Core host whose only purpose is to produce an OpenAPI
document for `EventsHub.Api` and drive NSwag's typed-client codegen from it.
**It is not part of the running application** — it never serves real
traffic and isn't started alongside `EventsHub.Api`.

## How it works

`Program.cs` loads `EventsHub.Api`'s assembly as an MVC "application part"
via reflection, registers an NSwag `AddOpenApiDocument`, and serves the
resulting document + Swagger UI:

```csharp
var pluginAssembly = Assembly.GetAssembly(typeof(WeatherForecastController));
services.AddMvc().AddApplicationPart(pluginAssembly!) /* ... */;
```

It references `typeof(WeatherForecastController)` (not `typeof(Program)`) to
get the `EventsHub.Api` assembly handle, because `Program` in that project
is an internal top-level type and can't be referenced from here.

Runs on its own launch profile (`https://localhost:5011;http://localhost:5010`)
specifically so it doesn't collide with the real API on `5001`.

## Regenerating the OpenAPI document + typed client

Run whenever a controller route or an entity/DTO shape changes in
`EventsHub.Api`:

```powershell
# 1. Start this host
dotnet run --project src/EventsHub.OpenApi/EventsHub.OpenApi.csproj --no-launch-profile --urls http://127.0.0.1:5011

# 2. Fetch the document (separate terminal)
Invoke-WebRequest -Uri http://127.0.0.1:5011/swagger/EventsHub/swagger.json -OutFile src/openapi/EventsHub.v1.json

# 3. Stop the host
Get-Process -Name "EventsHub.OpenApi" | Stop-Process -Force

# 4. Generate the client
dotnet tool run nswag run src/nswag/EventsHub.nswag

# 5. Confirm everything still compiles
dotnet build EventsHub.slnx
```

The NSwag CLI itself is a local tool (`.config/dotnet-tools.json`,
`nswag.consolecore` 14.7.1) — no global install needed, `dotnet tool run
nswag` picks it up.

## Generated output — never hand-edit

- `src/openapi/EventsHub.v1.json` — the OpenAPI 3.0 document (step 2 above)
- `src/EventsHub.OpenApi/Generated/EventsHubRpcClient.generated.cs` — the
  typed client (step 4 above): `EventsRpcClient` / `WeatherForecastRpcClient`
  plus interfaces, an `ApiException` hierarchy, and the `Event` DTO, all in
  namespace `EventsHub.OpenApi.Client`

Codegen shape (client class naming, output path, JSON library) is configured
in `src/nswag/EventsHub.nswag` — edit that file for config changes, not the
generated output.

Full historical context for how this project and pipeline were built is in
[`docs/BackendBuildSteps.md`](../../docs/BackendBuildSteps.md#step-8--openapi-document-host--nswag-rpc-client-e416730).
