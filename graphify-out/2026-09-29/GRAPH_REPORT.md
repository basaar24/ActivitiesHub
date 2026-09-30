# Graph Report - EventsHub  (2026-09-29)

## Corpus Check
- 72 files · ~48,633 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 5, .css 2, .nswag 1)

## Summary
- 675 nodes · 819 edges · 35 communities (30 shown, 5 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 51 edges (avg confidence: 0.88)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b55122c3`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- EventsHub.Persistence
- Event
- EventsHub Architecture
- package.json
- CqrsFundamentals.md
- Software Testing Fundamentals
- Clean Architecture Fundamentals
- EventsHub.UnitTests.csproj
- .GetEventsAsync
- Git in Practice Guide
- compilerOptions
- devDependencies
- compilerOptions
- DbContext
- https
- What You Must Do When Invoked
- EventsHub.OpenApi
- WeatherForecast
- 20260828131904_InitialCreate.Designer.cs
- Vite Logo SVG
- Loading Strategies (Eager/Explicit/Lazy)
- tsconfig.json
- index.d.ts
- Part 5: Practice
- 14. Event Sourcing — Advanced
- 9. Validation and error handling — Intermediate
- Part 6 — Practice and Pitfalls
- graphify reference: extra exports and benchmark
- graphify reference: query, path, explain
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- extraction-spec.md

## God Nodes (most connected - your core abstractions)
1. `Event` - 26 edges
2. `compilerOptions` - 18 edges
3. `compilerOptions` - 15 edges
4. `EventsHub Architecture` - 14 edges
5. `Software Testing Fundamentals` - 14 edges
6. `AppDbContext` - 13 edges
7. `What You Must Do When Invoked` - 12 edges
8. `EventsHub.Persistence` - 11 edges
9. `/graphify` - 11 edges
10. `EventsHub Project (CLAUDE.md)` - 11 edges

## Surprising Connections (you probably didn't know these)
- `Steps` --references--> `Event`  [INFERRED]
  docs/fundamentals/CleanArchitectureFundamentals.md → src/EventsHub.Domain/Event.cs
- `13. CQRS: Commands vs. Queries — Intermediate` --references--> `CreateEvent`  [INFERRED]
  docs/fundamentals/CleanArchitectureFundamentals.md → src/EventsHub.Application/Events/Commands/CreateEvent.cs
- `7. The Application Layer — Intermediate` --references--> `CreateEvent`  [INFERRED]
  docs/fundamentals/CleanArchitectureFundamentals.md → src/EventsHub.Application/Events/Commands/CreateEvent.cs
- `graphify skill trigger (/graphify)` --semantically_similar_to--> `graphify knowledge graph workflow`  [INFERRED] [semantically similar]
  .claude/CLAUDE.md → CLAUDE.md
- `CORS policy (Vite dev server only)` --semantically_similar_to--> `CORS locked to localhost:3000`  [INFERRED] [semantically similar]
  src/EventsHub.Api/README.md → CLAUDE.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **OpenAPI document + NSwag typed client regeneration pipeline** — src_eventshub_openapi_readme_application_part_loading, src_eventshub_openapi_readme_openapi_document, src_eventshub_openapi_readme_nswag_codegen, src_eventshub_openapi_readme_eventshubrpcclient, claude_openapi_client_regeneration [EXTRACTED 1.00]
- **OpenAPI codegen pipeline: doc host -> JSON doc -> NSwag config -> typed client** — docs_guides_openapisetup_eventshub_openapi_host, docs_guides_openapisetup_openapi_document, docs_guides_openapisetup_nswag_config, docs_guides_openapisetup_typed_rpc_client, docs_guides_openapisetup_nswag_local_tool [EXTRACTED 1.00]
- **EF Core tracking and save flow** — docs_fundamentals_dbcontextfundamentals_dbcontext, docs_fundamentals_dbcontextfundamentals_change_tracker, docs_fundamentals_dbcontextfundamentals_entity_states, docs_fundamentals_dbcontextfundamentals_savechanges [EXTRACTED 1.00]
- **Testing pyramid layers** — docs_fundamentals_softwaretestingfundamentals_testing_pyramid, docs_fundamentals_softwaretestingfundamentals_unit_testing, docs_fundamentals_softwaretestingfundamentals_integration_testing, docs_fundamentals_softwaretestingfundamentals_e2e_testing [EXTRACTED 1.00]
- **Event data shape shared across DB, API client, frontend types** — src_eventshub_domain_readme_event_entity, src_eventshub_persistence_readme_initialcreate_migration, src_eventshub_openapi_readme_eventshubrpcclient, web_readme_ambient_types [INFERRED 0.85]
- **Bruno Events CRUD integration suite against EventsController** — tests_eventshub_integrationtests_events_events___list___200, tests_eventshub_integrationtests_events_events___get___200, tests_eventshub_integrationtests_events_events___get___404, tests_eventshub_integrationtests_events_events___create___200, tests_eventshub_integrationtests_events_events___edit___204, tests_eventshub_integrationtests_events_events___delete___200, tests_eventshub_integrationtests_environments_local_baseurl [INFERRED 0.95]

## Communities (35 total, 5 thin omitted)

### Community 0 - "EventsHub.Persistence"
Cohesion: 0.06
Nodes (31): automapper, EventsHub.Domain, EventsHub.Application.Events.Queries, EventsHub.Api.Controllers, EventsHub.Application.Events.Commands, EventsHub.UnitTests, EventsHub.Persistence, EventsHub.UnitTests.Controllers (+23 more)

### Community 1 - "Event"
Cohesion: 0.05
Nodes (51): Command, DbContext, DbContextOptions, DbSet, ILogger, IMapper, IRequest, IRequestHandler (+43 more)

### Community 2 - "EventsHub Architecture"
Cohesion: 0.05
Nodes (62): graphify skill trigger (/graphify), Phase 2 - Approval-gated outline, architecture-docs skill, Phase 1 - Discovery (detect stack from real signals), Phase 3 - Generation (per-component READMEs + architecture doc), Clean-Architecture-flavored layering, CORS locked to localhost:3000, EventsHub Project (CLAUDE.md) (+54 more)

### Community 3 - "package.json"
Cohesion: 0.05
Nodes (45): axios, @babel/core, babel-plugin-react-compiler, @emotion/react, @emotion/styled, eslint, @eslint/js, eslint-plugin-react-hooks (+37 more)

### Community 4 - "CqrsFundamentals.md"
Cohesion: 0.06
Nodes (32): 10. Single database, separated models — Intermediate, 11. Separate read and write stores — Advanced, 12. Eventual consistency — Advanced, 13. Domain events and integration events — Intermediate, 1. What is CQRS? — General, 2. CQS vs. CQRS — General, 3. The problem CQRS solves — General, 4. Commands, Queries, and Events — General (+24 more)

### Community 5 - "Software Testing Fundamentals"
Cohesion: 0.08
Nodes (28): Startup MigrateAsync + SeedDataAsync (swallowed errors), Testing: in-process unit tests on shared SQLite + Bruno HTTP integration tests, DbContext Fundamentals (EF Core Deep Dive), Compiled Queries (EF.CompileQuery), Deferred vs Immediate Execution (IQueryable), EF Core Migrations, DbContext Test Provider Options (In-Memory vs SQLite vs Real DB), Software Testing Fundamentals (+20 more)

### Community 6 - "Clean Architecture Fundamentals"
Cohesion: 0.06
Nodes (35): 10. Project References and Enforcing the Dependency Direction — Intermediate, 11. Dependency Injection and the Composition Root — Intermediate, 12. Repository and Unit of Work — Advanced, 13. CQRS: Commands vs. Queries — Intermediate, 14. MediatR and Pipeline Behaviors — Advanced, 15. The Result Pattern vs. Exceptions — Intermediate, 16. Mapping and DTO Boundaries — Intermediate, 17. Validation with FluentValidation — Intermediate (+27 more)

### Community 7 - "EventsHub.UnitTests.csproj"
Cohesion: 0.07
Nodes (26): AutoMapper (13.0.1), coverlet.collector (6.0.4), MediatR (14.2.0), Microsoft.AspNetCore.Mvc.NewtonsoftJson (10.0.11), Microsoft.AspNetCore.OpenApi (10.0.11), Microsoft.EntityFrameworkCore.Design (10.0.11), Microsoft.EntityFrameworkCore.Sqlite (10.0.11), Microsoft.NET.Test.Sdk (17.14.0) (+18 more)

### Community 8 - ".GetEventsAsync"
Cohesion: 0.12
Nodes (19): ActionResult, ControllerBase, HttpDelete, HttpPost, HttpPut, IMediator, IReadOnlyList, NotFoundObjectResult (+11 more)

### Community 9 - "Git in Practice Guide"
Cohesion: 0.09
Nodes (23): Git in Practice Guide, Branch Naming Convention (feature/fix/hotfix/chore), Git Cherry-pick, Git Merge (fast-forward vs three-way), Pull Requests (gh CLI), Git Squash (interactive rebase / squash merge), Git Stash, Install Graphify + OpenSpec Guide (+15 more)

### Community 10 - "compilerOptions"
Cohesion: 0.10
Nodes (19): compilerOptions, allowArbitraryExtensions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection (+11 more)

### Community 11 - "devDependencies"
Cohesion: 0.11
Nodes (18): devDependencies, @babel/core, babel-plugin-react-compiler, eslint, @eslint/js, eslint-plugin-react-hooks, eslint-plugin-react-refresh, globals (+10 more)

### Community 12 - "compilerOptions"
Cohesion: 0.12
Nodes (16): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+8 more)

### Community 13 - "DbContext"
Cohesion: 0.15
Nodes (13): AddDbContextPool, Change Tracker (snapshot-based), DbContext, DbContext Scoped Lifetime, DbSet<T>, Entity States (Added/Unchanged/Modified/Deleted/Detached), ExecuteUpdate / ExecuteDelete Bulk Ops, Fluent API vs Data Annotations (+5 more)

### Community 14 - "https"
Cohesion: 0.20
Nodes (9): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, profiles, https (+1 more)

### Community 15 - "What You Must Do When Invoked"
Cohesion: 0.07
Nodes (26): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+18 more)

### Community 16 - "EventsHub.OpenApi"
Cohesion: 0.22
Nodes (8): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, environmentVariables, launchBrowser, profiles, EventsHub.OpenApi, $schema

### Community 17 - "WeatherForecast"
Cohesion: 0.25
Nodes (7): EventsHub.Api, DateOnly, WeatherForecast, Date, Summary, TemperatureC, TemperatureF

### Community 18 - "20260828131904_InitialCreate.Designer.cs"
Cohesion: 0.12
Nodes (15): EventsHub.Persistence.Migrations, microsoft_entityframeworkcore_infrastructure, microsoft_entityframeworkcore_migrations, microsoft_entityframeworkcore_storage_valueconversion, Migration, MigrationBuilder, ModelSnapshot, DateTime (+7 more)

### Community 19 - "Vite Logo SVG"
Cohesion: 0.40
Nodes (5): Favicon (purple Vite-style lightning bolt), Social Icons SVG Sprite (bluesky, discord, documentation, github, social, x), Hero Image (isometric stacked layers, purple base), React Logo SVG, Vite Logo SVG

### Community 20 - "Loading Strategies (Eager/Explicit/Lazy)"
Cohesion: 0.67
Nodes (3): AsSplitQuery / Cartesian Explosion, Loading Strategies (Eager/Explicit/Lazy), N+1 Query Problem

### Community 23 - "Part 5: Practice"
Cohesion: 0.08
Nodes (25): 19. Implementing the dispatching pipeline — Intermediate, 20. Testing CQRS systems — Intermediate, 21. CQRS in Web APIs — Intermediate, 22. Common pitfalls and anti-patterns — Intermediate, 23. Evolving toward CQRS — Advanced, A minimal dispatcher, A quick self-check, A staged approach (+17 more)

### Community 24 - "14. Event Sourcing — Advanced"
Cohesion: 0.08
Nodes (24): 14. Event Sourcing — Advanced, 15. Projections and read model rebuilding — Advanced, 16. Outbox pattern and reliable messaging — Advanced, 17. Sagas and process managers — Advanced, 18. Idempotency and concurrency — Advanced, Benefits, Compensation, Concurrency (+16 more)

### Community 25 - "9. Validation and error handling — Intermediate"
Cohesion: 0.11
Nodes (18): 6. The write side — Intermediate, 7. The read side — Intermediate, 8. Handlers and dispatching — Intermediate, 9. Validation and error handling — Intermediate, Design guidelines, Design guidelines, Guidelines, Part 2: Core Building Blocks (+10 more)

### Community 26 - "Part 6 — Practice and Pitfalls"
Cohesion: 0.13
Nodes (15): 25. Folder and Project Structure: Layer-Based vs. Feature-Based — Intermediate, 26. Common Mistakes: Anemic Domain, Leaky Abstractions, Over-Engineering — Intermediate, 27. When *Not* to Use Clean Architecture — General, 28. Migrating an Existing Codebase Incrementally — Advanced, 29. Guided Exercise: Build "Create Event" Through Every Layer — General, Feature-based (vertical slices), Layer-based (by technical role), Leaky abstractions (+7 more)

### Community 27 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 28 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 29 - "graphify reference: add a URL and watch a folder"
Cohesion: 0.50
Nodes (3): For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 30 - "graphify reference: commit hook and native CLAUDE.md integration"
Cohesion: 0.50
Nodes (3): For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration

### Community 31 - "graphify reference: incremental update and cluster-only"
Cohesion: 0.50
Nodes (3): For --cluster-only, For --update (incremental re-extraction), graphify reference: incremental update and cluster-only

## Ambiguous Edges - Review These
- `Hero Image (isometric stacked layers, purple base)` → `Vite Logo SVG`  [AMBIGUOUS]
  web/src/assets/hero.png · relation: conceptually_related_to
- `Bruno: Events - Create - 200 (POST /events)` → `Bruno: Events - Get - 404 (GET /events/non-existing-eventId)`  [AMBIGUOUS]
  tests/EventsHub.IntegrationTests/Events/Events - Create - 200.yml · relation: semantically_similar_to

## Knowledge Gaps
- **335 isolated node(s):** `Mediator`, `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.11)`, `Microsoft.EntityFrameworkCore.Design (10.0.11)`, `Microsoft.NET.Sdk.Web` (+330 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 397 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **5 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `Hero Image (isometric stacked layers, purple base)` and `Vite Logo SVG`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **What is the exact relationship between `Bruno: Events - Create - 200 (POST /events)` and `Bruno: Events - Get - 404 (GET /events/non-existing-eventId)`?**
  _Edge tagged AMBIGUOUS (relation: semantically_similar_to) - confidence is low._
- **Why does `Event` connect `Event` to `.GetEventsAsync`, `EventsHub.Persistence`, `Part 6 — Practice and Pitfalls`?**
  _High betweenness centrality (0.038) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `Event` to `EventsHub.Persistence`?**
  _High betweenness centrality (0.022) - this node is a cross-community bridge._
- **Why does `EventsHub Architecture` connect `EventsHub Architecture` to `Software Testing Fundamentals`?**
  _High betweenness centrality (0.022) - this node is a cross-community bridge._
- **What connects `Mediator`, `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.11)` to the rest of the system?**
  _335 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `EventsHub.Persistence` be split into smaller, more focused modules?**
  _Cohesion score 0.0603921568627451 - nodes in this community are weakly interconnected._