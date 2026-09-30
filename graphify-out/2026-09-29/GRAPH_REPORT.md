# Graph Report - EventsHub  (2026-09-29)

## Corpus Check
- Corpus is ~47,699 words - fits in a single context window. You may not need a graph.

## Summary
- 542 nodes · 711 edges · 23 communities (21 shown, 2 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 64 edges (avg confidence: 0.86)
- Token cost: 328,976 input · 0 output

## Community Hubs (Navigation)
- Solution Namespaces
- CQRS Handlers & Mapping
- Project Conventions (CLAUDE.md)
- Frontend Dependencies
- Clean Architecture Layers
- EventsHub Architecture Doc
- Architecture Theory & Authors
- NuGet Packages
- Events API Controller
- Git & Tooling Guides
- TS App Config
- Frontend Dev Tooling
- TS Node Config
- EF Core DbContext Concepts
- Api Launch Settings
- Unit Test Setup
- OpenApi Launch Settings
- WeatherForecast Sample
- EF Model Snapshot
- Frontend Template Assets
- EF Query Loading
- TS Root Config
- Frontend Types

## God Nodes (most connected - your core abstractions)
1. `Event` - 25 edges
2. `compilerOptions` - 18 edges
3. `compilerOptions` - 15 edges
4. `Software Testing Fundamentals` - 14 edges
5. `AppDbContext` - 13 edges
6. `EventsHub.Persistence` - 11 edges
7. `EventsHub Project (CLAUDE.md)` - 11 edges
8. `EventsHub Architecture` - 10 edges
9. `EventsHub.Domain` - 9 edges
10. `Clean Architecture` - 9 edges

## Surprising Connections (you probably didn't know these)
- `CORS policy (Vite dev server only)` --semantically_similar_to--> `CORS locked to localhost:3000`  [INFERRED] [semantically similar]
  src/EventsHub.Api/README.md → CLAUDE.md
- `OpenAPI typed RPC client regeneration pipeline` --semantically_similar_to--> `NSwag typed-client codegen (src/nswag/EventsHub.nswag, nswag.consolecore 14.7.1)`  [INFERRED] [semantically similar]
  CLAUDE.md → src/EventsHub.OpenApi/README.md
- `EventsHub.Api README` --semantically_similar_to--> `Shared seeded SQLite test DB convention (GlobalTestSetup)`  [INFERRED] [semantically similar]
  src/EventsHub.Api/README.md → CLAUDE.md
- `graphify skill trigger (/graphify)` --semantically_similar_to--> `graphify knowledge graph workflow`  [INFERRED] [semantically similar]
  .claude/CLAUDE.md → CLAUDE.md
- `Hardcoded API URL in App.tsx (no env base URL)` --semantically_similar_to--> `Bruno env local: baseUrl = https://localhost:5001/api/v1`  [INFERRED] [semantically similar]
  web/README.md → tests/EventsHub.IntegrationTests/environments/local.yml

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Clean Architecture layer stack (Domain, Application, Persistence, API)** — docs_fundamentals_cleanarchitecturefundamentals_domain_layer, docs_fundamentals_cleanarchitecturefundamentals_application_layer, docs_fundamentals_cleanarchitecturefundamentals_persistence_layer, docs_fundamentals_cleanarchitecturefundamentals_api_layer, docs_fundamentals_cleanarchitecturefundamentals_dependency_rule [EXTRACTED 1.00]
- **Create Event write flow (command, handler, repository, unit of work, aggregate)** — docs_fundamentals_cleanarchitecturefundamentals_createeventcommand, docs_fundamentals_cleanarchitecturefundamentals_createeventhandler, docs_fundamentals_cleanarchitecturefundamentals_ieventrepository, docs_fundamentals_cleanarchitecturefundamentals_iunitofwork, docs_fundamentals_cleanarchitecturefundamentals_event_aggregate_root [EXTRACTED 1.00]
- **Reliable event propagation (outbox, inbox, idempotency, projector)** — docs_fundamentals_cqrsfundamentals_outbox_pattern, docs_fundamentals_cqrsfundamentals_inbox_pattern, docs_fundamentals_cqrsfundamentals_idempotency, docs_fundamentals_cqrsfundamentals_projector, docs_fundamentals_cqrsfundamentals_dual_write_problem [INFERRED 0.85]
- **OpenAPI codegen pipeline: doc host -> JSON doc -> NSwag config -> typed client** — docs_guides_openapisetup_eventshub_openapi_host, docs_guides_openapisetup_openapi_document, docs_guides_openapisetup_nswag_config, docs_guides_openapisetup_typed_rpc_client, docs_guides_openapisetup_nswag_local_tool [EXTRACTED 1.00]
- **EF Core tracking and save flow** — docs_fundamentals_dbcontextfundamentals_dbcontext, docs_fundamentals_dbcontextfundamentals_change_tracker, docs_fundamentals_dbcontextfundamentals_entity_states, docs_fundamentals_dbcontextfundamentals_savechanges [EXTRACTED 1.00]
- **Testing pyramid layers** — docs_fundamentals_softwaretestingfundamentals_testing_pyramid, docs_fundamentals_softwaretestingfundamentals_unit_testing, docs_fundamentals_softwaretestingfundamentals_integration_testing, docs_fundamentals_softwaretestingfundamentals_e2e_testing [EXTRACTED 1.00]
- **OpenAPI document + NSwag typed client regeneration pipeline** — src_eventshub_openapi_readme_application_part_loading, src_eventshub_openapi_readme_openapi_document, src_eventshub_openapi_readme_nswag_codegen, src_eventshub_openapi_readme_eventshubrpcclient, claude_openapi_client_regeneration [EXTRACTED 1.00]
- **Bruno Events CRUD integration suite against EventsController** — tests_eventshub_integrationtests_events_events___list___200, tests_eventshub_integrationtests_events_events___get___200, tests_eventshub_integrationtests_events_events___get___404, tests_eventshub_integrationtests_events_events___create___200, tests_eventshub_integrationtests_events_events___edit___204, tests_eventshub_integrationtests_events_events___delete___200, tests_eventshub_integrationtests_environments_local_baseurl [INFERRED 0.95]
- **Event data shape shared across DB, API client, frontend types** — src_eventshub_domain_readme_event_entity, src_eventshub_persistence_readme_initialcreate_migration, src_eventshub_openapi_readme_eventshubrpcclient, web_readme_ambient_types [INFERRED 0.85]

## Communities (23 total, 2 thin omitted)

### Community 0 - "Solution Namespaces"
Cohesion: 0.05
Nodes (37): automapper, EventsHub.Domain, EventsHub.Persistence.Migrations, EventsHub.Application.Events.Queries, EventsHub.Api.Controllers, EventsHub.Application.Events.Commands, EventsHub.UnitTests, EventsHub.Persistence (+29 more)

### Community 1 - "CQRS Handlers & Mapping"
Cohesion: 0.05
Nodes (51): Command, DbContext, DbContextOptions, DbSet, ILogger, IMapper, IRequest, IRequestHandler (+43 more)

### Community 2 - "Project Conventions (CLAUDE.md)"
Cohesion: 0.06
Nodes (51): graphify skill trigger (/graphify), Phase 2 - Approval-gated outline, architecture-docs skill, Phase 1 - Discovery (detect stack from real signals), Phase 3 - Generation (per-component READMEs + architecture doc), Clean-Architecture-flavored layering, CORS locked to localhost:3000, EventsHub Project (CLAUDE.md) (+43 more)

### Community 3 - "Frontend Dependencies"
Cohesion: 0.05
Nodes (45): axios, @babel/core, babel-plugin-react-compiler, @emotion/react, @emotion/styled, eslint, @eslint/js, eslint-plugin-react-hooks (+37 more)

### Community 4 - "Clean Architecture Layers"
Cohesion: 0.06
Nodes (46): Aggregate / Aggregate Root, Anemic Domain Model (anti-pattern), Presentation / API Layer (thin controllers), Application Layer (Use Cases), Composition Root, Concentric-Circles Diagram (Entities, Use Cases, Interface Adapters, Frameworks & Drivers), CQRS (Clean Architecture topic 13), CreateEventCommand (+38 more)

### Community 5 - "EventsHub Architecture Doc"
Cohesion: 0.06
Nodes (42): EventsHub Architecture, EventsHubBaseController Routing Convention (api/v1/[controller]), Clean Architecture (shape only, not followed), CORS locked to Vite dev server (localhost:3000), EventsHub.Application (empty placeholder), Client-generated GUID string IDs, No DTO layer (Event entity as wire format), Request Walkthrough (SPA -> EventsController -> EF Core -> SQLite) (+34 more)

### Community 6 - "Architecture Theory & Authors"
Cohesion: 0.07
Nodes (33): Clean Architecture Fundamentals (guide), Alistair Cockburn, Architecture Tests (NetArchTest / ArchUnitNET), Clean Architecture, Guided Exercise: Create Event through every layer, Dependency Inversion Principle, Dependency Rule, DTO Boundaries and Mapping (Manual / Mapster / AutoMapper) (+25 more)

### Community 7 - "NuGet Packages"
Cohesion: 0.07
Nodes (26): AutoMapper (13.0.1), coverlet.collector (6.0.4), MediatR (14.2.0), Microsoft.AspNetCore.Mvc.NewtonsoftJson (10.0.11), Microsoft.AspNetCore.OpenApi (10.0.11), Microsoft.EntityFrameworkCore.Design (10.0.11), Microsoft.EntityFrameworkCore.Sqlite (10.0.11), Microsoft.NET.Test.Sdk (17.14.0) (+18 more)

### Community 8 - "Events API Controller"
Cohesion: 0.12
Nodes (19): ActionResult, ControllerBase, HttpDelete, HttpPost, HttpPut, IMediator, IReadOnlyList, NotFoundObjectResult (+11 more)

### Community 9 - "Git & Tooling Guides"
Cohesion: 0.13
Nodes (20): Generated vs Hand-maintained Files Rule, Git in Practice Guide, Branch Naming Convention (feature/fix/hotfix/chore), Git Cherry-pick, Git Merge (fast-forward vs three-way), Pull Requests (gh CLI), Git Squash (interactive rebase / squash merge), Git Stash (+12 more)

### Community 10 - "TS App Config"
Cohesion: 0.10
Nodes (19): compilerOptions, allowArbitraryExtensions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection (+11 more)

### Community 11 - "Frontend Dev Tooling"
Cohesion: 0.11
Nodes (18): devDependencies, @babel/core, babel-plugin-react-compiler, eslint, @eslint/js, eslint-plugin-react-hooks, eslint-plugin-react-refresh, globals (+10 more)

### Community 12 - "TS Node Config"
Cohesion: 0.12
Nodes (16): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+8 more)

### Community 13 - "EF Core DbContext Concepts"
Cohesion: 0.15
Nodes (13): AddDbContextPool, Change Tracker (snapshot-based), DbContext, DbContext Scoped Lifetime, DbSet<T>, Entity States (Added/Unchanged/Modified/Deleted/Detached), ExecuteUpdate / ExecuteDelete Bulk Ops, Fluent API vs Data Annotations (+5 more)

### Community 14 - "Api Launch Settings"
Cohesion: 0.20
Nodes (9): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, profiles, https (+1 more)

### Community 15 - "Unit Test Setup"
Cohesion: 0.25
Nodes (6): OneTimeSetUp, OneTimeTearDown, Task, Task, GlobalTestSetup, AppDbContext

### Community 16 - "OpenApi Launch Settings"
Cohesion: 0.22
Nodes (8): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, environmentVariables, launchBrowser, profiles, EventsHub.OpenApi, $schema

### Community 17 - "WeatherForecast Sample"
Cohesion: 0.25
Nodes (7): EventsHub.Api, DateOnly, WeatherForecast, Date, Summary, TemperatureC, TemperatureF

### Community 18 - "EF Model Snapshot"
Cohesion: 0.40
Nodes (4): ModelSnapshot, DateTime, ModelBuilder, AppDbContextModelSnapshot

### Community 19 - "Frontend Template Assets"
Cohesion: 0.40
Nodes (5): Favicon (purple Vite-style lightning bolt), Social Icons SVG Sprite (bluesky, discord, documentation, github, social, x), Hero Image (isometric stacked layers, purple base), React Logo SVG, Vite Logo SVG

### Community 20 - "EF Query Loading"
Cohesion: 0.67
Nodes (3): AsSplitQuery / Cartesian Explosion, Loading Strategies (Eager/Explicit/Lazy), N+1 Query Problem

## Ambiguous Edges - Review These
- `Bruno: Events - Create - 200 (POST /events)` → `Bruno: Events - Get - 404 (GET /events/non-existing-eventId)`  [AMBIGUOUS]
  tests/EventsHub.IntegrationTests/Events/Events - Create - 200.yml · relation: semantically_similar_to
- `Hero Image (isometric stacked layers, purple base)` → `Vite Logo SVG`  [AMBIGUOUS]
  web/src/assets/hero.png · relation: conceptually_related_to

## Knowledge Gaps
- **207 isolated node(s):** `Mediator`, `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.11)`, `Microsoft.EntityFrameworkCore.Design (10.0.11)`, `Microsoft.NET.Sdk.Web` (+202 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 260 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `Bruno: Events - Create - 200 (POST /events)` and `Bruno: Events - Get - 404 (GET /events/non-existing-eventId)`?**
  _Edge tagged AMBIGUOUS (relation: semantically_similar_to) - confidence is low._
- **What is the exact relationship between `Hero Image (isometric stacked layers, purple base)` and `Vite Logo SVG`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `Event` connect `CQRS Handlers & Mapping` to `Events API Controller`, `Solution Namespaces`?**
  _High betweenness centrality (0.031) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `CQRS Handlers & Mapping` to `Solution Namespaces`, `Unit Test Setup`?**
  _High betweenness centrality (0.025) - this node is a cross-community bridge._
- **What connects `Mediator`, `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.11)` to the rest of the system?**
  _207 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Solution Namespaces` be split into smaller, more focused modules?**
  _Cohesion score 0.05310734463276836 - nodes in this community are weakly interconnected._
- **Should `CQRS Handlers & Mapping` be split into smaller, more focused modules?**
  _Cohesion score 0.0512987012987013 - nodes in this community are weakly interconnected._