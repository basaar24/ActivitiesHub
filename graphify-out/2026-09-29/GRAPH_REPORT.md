# Graph Report - EventsHub  (2026-09-29)

## Corpus Check
- 99 files · ~81,801 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 10 file(s) not represented in the graph (top: (none) 7, .css 2, .nswag 1)

## Summary
- 907 nodes · 1226 edges · 69 communities (53 shown, 16 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 123 edges (avg confidence: 0.9)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `77da7c26`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- EventsHub.Application.Core.Mapping
- Event
- EventsHub Architecture
- package.json
- Part 3: Architecture Variants
- Software Testing Fundamentals
- Clean Architecture Fundamentals
- EventsHub.Application.UnitTests.csproj
- .GetEventsAsync
- Git in Practice Guide
- compilerOptions
- devDependencies
- compilerOptions
- Profile
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
- Part 2: Core Building Blocks
- Part 6 — Practice and Pitfalls
- graphify reference: extra exports and benchmark
- graphify reference: query, path, explain
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- extraction-spec.md
- MapperTests
- TypeMap
- ADDED Requirements
- openspec-explore/SKILL.md
- explore.md
- Command
- AppDbContext
- Part 2 — Layers in a .NET Solution
- Handler
- IRequest
- Handler
- Part 3 — Key Patterns
- 18. Logging, Caching, and Authentication / Authorization — Intermediate
- CqrsFundamentals.md
- GlobalTestSetup
- Part 1 — Foundations
- 26. Common Mistakes: Anemic Domain, Leaky Abstractions, Over-Engineering — Intermediate
- 13. Domain events and integration events — Intermediate
- 5. When to use it and when not to — Intermediate
- 9. Validation and error handling — Intermediate
- 12. Eventual consistency — Advanced
- 4. Commands, Queries, and Events — General
- 7. The read side — Intermediate
- automapper

## God Nodes (most connected - your core abstractions)
1. `Event` - 36 edges
2. `TypeMap` - 24 edges
3. `MapperTests` - 19 edges
4. `compilerOptions` - 18 edges
5. `Profile` - 17 edges
6. `compilerOptions` - 15 edges
7. `IMapper` - 14 edges
8. `Mapper` - 14 edges
9. `EventsHub Architecture` - 14 edges
10. `Software Testing Fundamentals` - 14 edges

## Surprising Connections (you probably didn't know these)
- `Steps` --references--> `Event`  [INFERRED]
  docs/fundamentals/CleanArchitectureFundamentals.md → src/EventsHub.Domain/Event.cs
- `5. New-instance creation` --references--> `Event`  [INFERRED]
  openspec/changes/replace-automapper-with-custom-mapper/design.md → src/EventsHub.Domain/Event.cs
- `Context` --references--> `Event`  [INFERRED]
  openspec/changes/replace-automapper-with-custom-mapper/design.md → src/EventsHub.Domain/Event.cs
- `Scenario: Edit copies fields onto a tracked entity` --references--> `Event`  [INFERRED]
  openspec/changes/replace-automapper-with-custom-mapper/specs/object-mapping/spec.md → src/EventsHub.Domain/Event.cs
- `Scenario: Startup with the existing profile` --references--> `Event`  [INFERRED]
  openspec/changes/replace-automapper-with-custom-mapper/specs/object-mapping/spec.md → src/EventsHub.Domain/Event.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **OpenAPI document + NSwag typed client regeneration pipeline** — src_eventshub_openapi_readme_application_part_loading, src_eventshub_openapi_readme_openapi_document, src_eventshub_openapi_readme_nswag_codegen, src_eventshub_openapi_readme_eventshubrpcclient, claude_openapi_client_regeneration [EXTRACTED 1.00]
- **OpenAPI codegen pipeline: doc host -> JSON doc -> NSwag config -> typed client** — docs_guides_openapisetup_eventshub_openapi_host, docs_guides_openapisetup_openapi_document, docs_guides_openapisetup_nswag_config, docs_guides_openapisetup_typed_rpc_client, docs_guides_openapisetup_nswag_local_tool [EXTRACTED 1.00]
- **EF Core tracking and save flow** — docs_fundamentals_dbcontextfundamentals_dbcontext, docs_fundamentals_dbcontextfundamentals_change_tracker, docs_fundamentals_dbcontextfundamentals_entity_states, docs_fundamentals_dbcontextfundamentals_savechanges [EXTRACTED 1.00]
- **Testing pyramid layers** — docs_fundamentals_softwaretestingfundamentals_testing_pyramid, docs_fundamentals_softwaretestingfundamentals_unit_testing, docs_fundamentals_softwaretestingfundamentals_integration_testing, docs_fundamentals_softwaretestingfundamentals_e2e_testing [EXTRACTED 1.00]
- **Event data shape shared across DB, API client, frontend types** — src_eventshub_domain_readme_event_entity, src_eventshub_persistence_readme_initialcreate_migration, src_eventshub_openapi_readme_eventshubrpcclient, web_readme_ambient_types [INFERRED 0.85]
- **Bruno Events CRUD integration suite against EventsController** — tests_eventshub_integrationtests_events_events___list___200, tests_eventshub_integrationtests_events_events___get___200, tests_eventshub_integrationtests_events_events___get___404, tests_eventshub_integrationtests_events_events___create___200, tests_eventshub_integrationtests_events_events___edit___204, tests_eventshub_integrationtests_events_events___delete___200, tests_eventshub_integrationtests_environments_local_baseurl [INFERRED 0.95]

## Communities (69 total, 16 thin omitted)

### Community 0 - "EventsHub.Application.Core.Mapping"
Cohesion: 0.07
Nodes (25): EventsHub.Domain, EventsHub.Application.Core.Mapping, EventsHub.Application.Events.Queries, EventsHub.Api.Controllers, EventsHub.Application.Events.Commands, EventsHub.UnitTests, EventsHub.Persistence, EventsHub.Application.UnitTests.Mapping (+17 more)

### Community 1 - "Event"
Cohesion: 0.15
Nodes (12): DateTime, Event, Category, City, Date, Description, Id, IsCancelled (+4 more)

### Community 2 - "EventsHub Architecture"
Cohesion: 0.05
Nodes (62): graphify skill trigger (/graphify), Phase 2 - Approval-gated outline, architecture-docs skill, Phase 1 - Discovery (detect stack from real signals), Phase 3 - Generation (per-component READMEs + architecture doc), Clean-Architecture-flavored layering, CORS locked to localhost:3000, EventsHub Project (CLAUDE.md) (+54 more)

### Community 3 - "package.json"
Cohesion: 0.05
Nodes (45): axios, @babel/core, babel-plugin-react-compiler, @emotion/react, @emotion/styled, eslint, @eslint/js, eslint-plugin-react-hooks (+37 more)

### Community 4 - "Part 3: Architecture Variants"
Cohesion: 0.25
Nodes (8): 10. Single database, separated models — Intermediate, 11. Separate read and write stores — Advanced, Part 3: Architecture Variants, Synchronization strategies, The real cost, Variations, Why do it, Why start here

### Community 5 - "Software Testing Fundamentals"
Cohesion: 0.06
Nodes (34): Startup MigrateAsync + SeedDataAsync (swallowed errors), Testing: in-process unit tests on shared SQLite + Bruno HTTP integration tests, DbContext Fundamentals (EF Core Deep Dive), AddDbContextPool, Change Tracker (snapshot-based), Compiled Queries (EF.CompileQuery), DbContext, DbContext Scoped Lifetime (+26 more)

### Community 6 - "Clean Architecture Fundamentals"
Cohesion: 0.25
Nodes (7): 21. Unit Testing the Domain and Application Layers — General, 22. Mocking Boundaries — Intermediate, 23. Integration Testing with WebApplicationFactory and a Test Database — Advanced, 24. Architecture Tests — Advanced, Clean Architecture Fundamentals, Glossary, Part 5 — Testing

### Community 7 - "EventsHub.Application.UnitTests.csproj"
Cohesion: 0.06
Nodes (32): MediatR (14.2.0), Microsoft.AspNetCore.Mvc.NewtonsoftJson (10.0.11), Microsoft.AspNetCore.OpenApi (10.0.11), Microsoft.EntityFrameworkCore.Design (10.0.11), Microsoft.EntityFrameworkCore.Sqlite (10.0.11), Moq (4.20.72), Newtonsoft.Json (13.0.4), NSwag.AspNetCore (14.7.1) (+24 more)

### Community 8 - ".GetEventsAsync"
Cohesion: 0.12
Nodes (20): ActionResult, ControllerBase, HttpDelete, HttpPost, HttpPut, IMediator, NotFoundObjectResult, 8. Tests (+12 more)

### Community 9 - "Git in Practice Guide"
Cohesion: 0.07
Nodes (30): End-to-End Testing, Ice Cream Cone Anti-Pattern, Integration Testing, Test Doubles (Dummy/Stub/Fake/Mock/Spy), Testing Pyramid (Mike Cohn), Unit Testing, Git in Practice Guide, Branch Naming Convention (feature/fix/hotfix/chore) (+22 more)

### Community 10 - "compilerOptions"
Cohesion: 0.10
Nodes (19): compilerOptions, allowArbitraryExtensions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection (+11 more)

### Community 11 - "devDependencies"
Cohesion: 0.11
Nodes (18): devDependencies, @babel/core, babel-plugin-react-compiler, eslint, @eslint/js, eslint-plugin-react-hooks, eslint-plugin-react-refresh, globals (+10 more)

### Community 12 - "compilerOptions"
Cohesion: 0.12
Nodes (16): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+8 more)

### Community 13 - "Profile"
Cohesion: 0.05
Nodes (49): Assembly, IServiceCollection, 1. Namespace and layout, 3. Members from the source are matched case-sensitively, 4. Nested and collection members, 5. New-instance creation, 6. Errors and lifetime, 7. Startup wiring (+41 more)

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

### Community 25 - "Part 2: Core Building Blocks"
Cohesion: 0.22
Nodes (9): 6. The write side — Intermediate, 8. Handlers and dispatching — Intermediate, Design guidelines, Part 2: Core Building Blocks, Pipeline behaviors, Responsibilities, The command handler, The domain model and aggregates (+1 more)

### Community 26 - "Part 6 — Practice and Pitfalls"
Cohesion: 0.20
Nodes (10): 25. Folder and Project Structure: Layer-Based vs. Feature-Based — Intermediate, 27. When *Not* to Use Clean Architecture — General, 28. Migrating an Existing Codebase Incrementally — Advanced, 29. Guided Exercise: Build "Create Event" Through Every Layer — General, Feature-based (vertical slices), Layer-based (by technical role), Part 6 — Practice and Pitfalls, Review questions (+2 more)

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

### Community 35 - "MapperTests"
Cohesion: 0.08
Nodes (38): ArgumentException, ArgumentNullException, InvalidOperationException, TestCase, IReadOnlyList, List, ChildDest, Name (+30 more)

### Community 36 - "TypeMap"
Cohesion: 0.09
Nodes (24): IReadOnlyDictionary, LambdaExpression, MethodInfo, 2. Compiled delegates per map, PropertyInfo, Action, Expression, Func (+16 more)

### Community 37 - "ADDED Requirements"
Cohesion: 0.07
Nodes (26): ADDED Requirements, Purpose, Requirement: Convention-based member matching, Requirement: Edit behavior is preserved, Requirement: Map into a new destination, Requirement: Map onto an existing destination, Requirement: Nested and collection members, Requirement: Per-member overrides (+18 more)

### Community 38 - "openspec-explore/SKILL.md"
Cohesion: 0.17
Nodes (11): Check for context, Ending Discovery, Guardrails, Handling Different Entry Points, OpenSpec Awareness, Planning a Change, The Stance, What You Don't Have To Do (+3 more)

### Community 39 - "explore.md"
Cohesion: 0.18
Nodes (10): Check for context, Ending Discovery, Guardrails, OpenSpec Awareness, Planning a Change, The Stance, What You Don't Have To Do, What You Might Do (+2 more)

### Community 40 - "Command"
Cohesion: 0.27
Nodes (8): Command, IRequestHandler, CancellationToken, Task, Handler, CancellationToken, Task, Handler

### Community 41 - "AppDbContext"
Cohesion: 0.25
Nodes (7): DbContext, DbContextOptions, DbSet, AppDbContext, Events, Task, DbInitializer

### Community 42 - "Part 2 — Layers in a .NET Solution"
Cohesion: 0.25
Nodes (8): 10. Project References and Enforcing the Dependency Direction — Intermediate, 13. CQRS: Commands vs. Queries — Intermediate, 6. The Domain Layer — Intermediate, 7. The Application Layer — Intermediate, 8. The Infrastructure / Persistence Layer — Intermediate, 9. The Presentation / API Layer — General, Part 2 — Layers in a .NET Solution, CreateEvent

### Community 43 - "Handler"
Cohesion: 0.32
Nodes (7): ILogger, CancellationToken, List, Task, GetEventList, Handler, Query

### Community 44 - "IRequest"
Cohesion: 0.25
Nodes (8): IRequest, Command, Event, Command, Id, DeleteEvent, Command, Event

### Community 45 - "Handler"
Cohesion: 0.29
Nodes (7): Query, CancellationToken, Task, GetEventDetails, Handler, Query, Id

### Community 46 - "Part 3 — Key Patterns"
Cohesion: 0.29
Nodes (7): 11. Dependency Injection and the Composition Root — Intermediate, 12. Repository and Unit of Work — Advanced, 14. MediatR and Pipeline Behaviors — Advanced, 15. The Result Pattern vs. Exceptions — Intermediate, 16. Mapping and DTO Boundaries — Intermediate, 17. Validation with FluentValidation — Intermediate, Part 3 — Key Patterns

### Community 47 - "18. Logging, Caching, and Authentication / Authorization — Intermediate"
Cohesion: 0.29
Nodes (7): 18. Logging, Caching, and Authentication / Authorization — Intermediate, 19. API Contracts and OpenAPI — Intermediate, 20. Configuration and the Options Pattern — General, Authentication and authorization, Caching, Logging, Part 4 — Cross-Cutting Concerns

### Community 48 - "CqrsFundamentals.md"
Cohesion: 0.29
Nodes (6): 1. What is CQRS? — General, 2. CQS vs. CQRS — General, 3. The problem CQRS solves — General, CQRS Fundamentals: A Tool-Agnostic Reference Guide, Part 1: Foundations, Table of Contents

### Community 49 - "GlobalTestSetup"
Cohesion: 0.33
Nodes (5): OneTimeSetUp, OneTimeTearDown, Task, GlobalTestSetup, AppDbContext

### Community 50 - "Part 1 — Foundations"
Cohesion: 0.33
Nodes (6): 1. What Clean Architecture Is and the Problem It Solves — General, 2. Origins and Related Styles: Hexagonal, Onion, and Clean — General, 3. The Dependency Rule — General, 4. The Concentric-Circles Diagram — General, 5. SOLID Principles as the Basis — Intermediate, Part 1 — Foundations

### Community 51 - "26. Common Mistakes: Anemic Domain, Leaky Abstractions, Over-Engineering — Intermediate"
Cohesion: 0.40
Nodes (5): 26. Common Mistakes: Anemic Domain, Leaky Abstractions, Over-Engineering — Intermediate, Leaky abstractions, Other frequent problems, Over-engineering, The anemic domain model

### Community 52 - "13. Domain events and integration events — Intermediate"
Cohesion: 0.40
Nodes (5): 13. Domain events and integration events — Intermediate, Domain events, Guidelines, Integration events, Translating between them

### Community 53 - "5. When to use it and when not to — Intermediate"
Cohesion: 0.40
Nodes (5): 5. When to use it and when not to — Intermediate, A useful rule of thumb, Signals that CQRS may pay off, Signals that it is probably unnecessary, The costs

### Community 54 - "9. Validation and error handling — Intermediate"
Cohesion: 0.40
Nodes (5): 9. Validation and error handling — Intermediate, Guidelines, The Result pattern, Types of failure, Where validation belongs

### Community 55 - "12. Eventual consistency — Advanced"
Cohesion: 0.50
Nodes (4): 12. Eventual consistency — Advanced, Design principles, Handling it in the user experience, What it looks like to a user

### Community 56 - "4. Commands, Queries, and Events — General"
Cohesion: 0.50
Nodes (4): 4. Commands, Queries, and Events — General, Commands, Events, Queries

### Community 57 - "7. The read side — Intermediate"
Cohesion: 0.50
Nodes (4): 7. The read side — Intermediate, Design guidelines, Query handlers, Read models

## Ambiguous Edges - Review These
- `Hero Image (isometric stacked layers, purple base)` → `Vite Logo SVG`  [AMBIGUOUS]
  web/src/assets/hero.png · relation: conceptually_related_to
- `Bruno: Events - Create - 200 (POST /events)` → `Bruno: Events - Get - 404 (GET /events/non-existing-eventId)`  [AMBIGUOUS]
  tests/EventsHub.IntegrationTests/Events/Events - Create - 200.yml · relation: semantically_similar_to

## Knowledge Gaps
- **409 isolated node(s):** `Mediator`, `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.11)`, `Microsoft.EntityFrameworkCore.Design (10.0.11)`, `Microsoft.NET.Sdk.Web` (+404 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 500 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **16 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `Hero Image (isometric stacked layers, purple base)` and `Vite Logo SVG`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **What is the exact relationship between `Bruno: Events - Create - 200 (POST /events)` and `Bruno: Events - Get - 404 (GET /events/non-existing-eventId)`?**
  _Edge tagged AMBIGUOUS (relation: semantically_similar_to) - confidence is low._
- **Why does `Event` connect `Event` to `ADDED Requirements`, `.GetEventsAsync`, `AppDbContext`, `Handler`, `IRequest`, `Profile`, `Handler`, `Part 6 — Practice and Pitfalls`?**
  _High betweenness centrality (0.087) - this node is a cross-community bridge._
- **Why does `Profile` connect `Profile` to `EventsHub.Application.Core.Mapping`, `MapperTests`, `TypeMap`?**
  _High betweenness centrality (0.032) - this node is a cross-community bridge._
- **Why does `TypeMap` connect `TypeMap` to `EventsHub.Application.Core.Mapping`, `Profile`?**
  _High betweenness centrality (0.027) - this node is a cross-community bridge._
- **Are the 10 inferred relationships involving `Event` (e.g. with `Steps` and `5. New-instance creation`) actually correct?**
  _`Event` has 10 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Mediator`, `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.11)` to the rest of the system?**
  _409 weakly-connected nodes found - possible documentation gaps or missing edges._