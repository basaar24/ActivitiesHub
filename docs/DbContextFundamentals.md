# Entity Framework Core: DbContext Deep Dive

> This guide targets **EF Core** (not EF6/EF Classic). It assumes working knowledge of C#, .NET, and basic relational database concepts. Code samples use C#.

## 0. Introduction

> **Depth: General**

This guide explains what `DbContext` is and does, starting from the fundamentals and progressively moving into the internals: change tracking, transactions, performance, pooling, and testing. It's meant for developers who already use EF Core day-to-day and want a deeper, more precise mental model of what happens under the hood.

**Scope:** EF Core running against a relational provider (SQL Server, SQLite, PostgreSQL, etc.). Provider-specific syntax is avoided where possible; concepts apply broadly across providers.

---

## 1. What Is `DbContext`?

> **Depth: General**

In .NET, particularly with Entity Framework (EF) / EF Core, a `DbContext` is the primary class that manages the interaction between your application and the database.

It acts as:

- **A session with the database** — like a connection plus a change tracker, scoped to a unit of work.
- **A Unit of Work** — groups a set of changes into a single transaction when `SaveChanges()` is called.
- **A Repository** — provides access to entity sets via `DbSet<T>`, letting you query and manipulate data as objects rather than writing raw SQL.

### `DbContext` vs. `DbSet<T>`

- `DbContext` is the overall session — it owns the connection, the change tracker, the model, and the `SaveChanges()` pipeline.
- `DbSet<T>` is a typed collection *exposed by* the context, representing (roughly) a table or entity type. All `DbSet<T>` properties on a context share the same underlying connection, change tracker, and transaction.

### Where It Fits in an Application

In a typical layered application, `DbContext` sits in the data access layer, injected into repositories or directly into services via dependency injection (in ASP.NET Core). It is *not* meant to be a long-lived, application-wide singleton — its lifetime is a first-class design concern (see Section 6).

---

---

## 1.5. How `DbContext` Works — Visual Overview

> **Depth: General**

The diagram below traces a single request end-to-end: creating the context, running a query, tracking a change, and saving it back to the database file. Every numbered step maps to a section elsewhere in this guide.

```
┌───────────────────────────────────────────────────────────────────────────┐
│                              APPLICATION (API)                             │
│                                                                             │
│   HTTP Request comes in                                                    │
│         │                                                                  │
│         ▼                                                                  │
│   ┌─────────────────────┐                                                  │
│   │   DI Container       │  1. New scope created for this request          │
│   │  (Scoped lifetime)   │     -> builds a NEW AppDbContext instance       │
│   └──────────┬───────────┘                                                 │
│              │  new AppDbContext(options)                                  │
│              ▼                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐ │
│   │                         DbContext INSTANCE                          │ │
│   │                                                                     │ │
│   │   ┌───────────────┐   ┌────────────────────┐   ┌──────────────┐    │ │
│   │   │   DbSet<T>     │   │   Change Tracker    │   │  Connection  │    │ │
│   │   │  Users, Orders │   │  (entity snapshots) │   │   (to file)  │    │ │
│   │   └───────┬────────┘   └──────────┬──────────┘   └──────┬───────┘    │ │
│   │           │                       │                     │           │ │
│   └───────────┼───────────────────────┼─────────────────────┼───────────┘ │
│               │                       │                     │             │
└───────────────┼───────────────────────┼─────────────────────┼─────────────┘
                │                       │                     │
   2. Controller/Service code           │                     │
      issues a LINQ query:              │                     │
      context.Users.Where(...)          │                     │
                │                       │                     │
                ▼                       │                     │
   ┌─────────────────────────┐          │                     │
   │   LINQ Expression Tree   │         │                     │
   └────────────┬─────────────┘         │                     │
                │  translate            │                     │
                ▼                       │                     │
   ┌─────────────────────────┐          │                     │
   │     SQL Generator        │         │                     │
   │  "SELECT * FROM Users     │        │                     │
   │   WHERE IsActive = 1"     │        │                     │
   └────────────┬─────────────┘         │                     │
                │  3. send SQL          │                     │
                └───────────────────────┼─────────────────────┤
                                        │                     ▼
                                        │           ┌───────────────────┐
                                        │           │   app.db (file)    │
                                        │           │   ┌─────────────┐  │
                                        │           │   │ Users table │  │
                                        │           │   │ Orders table│  │
                                        │           │   └─────────────┘  │
                                        │           └─────────┬──────────┘
                                        │   4. rows returned   │
                                        │◄──────────────────────┘
                                        │
                                        ▼
                          ┌──────────────────────────┐
                          │   Materialization          │
                          │  rows -> User objects      │
                          └────────────┬────────────────┘
                                       │
                5. Each returned entity is registered
                   with the Change Tracker as "Unchanged"
                   (skipped entirely if AsNoTracking() was used)
                                       │
                                       ▼
                          ┌──────────────────────────┐
                          │   Change Tracker snapshot  │
                          │  User#1: Name="Alice"      │
                          │  State = Unchanged          │
                          └────────────┬────────────────┘
                                       │
           6. App code mutates the entity:
              user.Name = "Alice Updated";
                                       │
                                       ▼
                          ┌──────────────────────────┐
                          │   Change Tracker detects   │
                          │   the difference            │
                          │  User#1: State = Modified   │
                          └────────────┬────────────────┘
                                       │
           7. App code calls:
              context.SaveChanges();
                                       │
                                       ▼
                     ┌───────────────────────────────┐
                     │        SaveChanges() pipeline    │
                     │  a) DetectChanges()               │
                     │  b) Find Added/Modified/Deleted   │
                     │  c) Generate SQL per entity        │
                     │  d) BEGIN TRANSACTION              │
                     └────────────────┬──────────────────┘
                                      │
                                      ▼
                     "UPDATE Users SET Name='Alice Updated'
                      WHERE Id=1"
                                      │
                                      ▼
                          ┌───────────────────┐
                          │   app.db (file)     │
                          │   Users table        │
                          │   row updated ✅     │
                          └────────────┬──────────┘
                                      │
                                      ▼
                     ┌───────────────────────────────┐
                     │   COMMIT TRANSACTION              │
                     │   Tracker resets User#1 state      │
                     │   back to Unchanged                 │
                     └───────────────────────────────┘
                                      │
                                      ▼
                     8. DbContext disposed at end of
                        the HTTP request (scope ends)
                        -> connection released
```

**Step-by-step narration:**

1. **Instance creation.** The DI container starts a new scope for the incoming request and constructs a fresh `AppDbContext` — this is the "session" role described in Section 1. See Section 6 for lifetime/scope rules.
2. **Query issued.** Application code writes a LINQ query against a `DbSet<T>`.
3. **Translation.** EF Core turns the LINQ expression tree into SQL and sends it to the database. See Section 3.
4. **Materialization & tracking.** Rows come back from `app.db` and are turned into entity objects; each one is registered with the Change Tracker in the `Unchanged` state (skipped entirely under `AsNoTracking()`). See Section 4.
5. **Mutation.** Application code changes a property on a tracked entity; the Change Tracker flags it `Modified` on the next `DetectChanges()` pass.
6. **`SaveChanges()`.** EF Core detects the pending change, generates the `UPDATE` statement, and wraps it in an implicit transaction. See Section 5.
7. **Commit & dispose.** The transaction commits, the tracker resets the entity back to `Unchanged`, and the context is disposed at the end of the request scope, releasing the connection.

---

## 2. Configuration & Setup

> **Depth: General**

When you create a `DbContext` class, you configure:

- The **database connection**, via a connection string or `DbContextOptions`.
- **Entity mappings**, via `OnModelCreating` (Fluent API) or data annotations on entity classes.
- **`DbSet<T>` properties**, one for each entity type you want to query/persist directly.

```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fluent API configuration
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}
```

Registering the context for DI in an ASP.NET Core app:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### Fluent API vs. Data Annotations

| Approach | Where it lives | Strengths | Limitations |
|---|---|---|---|
| **Data Annotations** | Attributes on entity properties (`[Required]`, `[MaxLength]`) | Quick, colocated with the model | Can't express everything (e.g., composite keys, some index configs); couples domain classes to EF |
| **Fluent API** | `OnModelCreating`, via `ModelBuilder` | Full configuration surface; keeps entities EF-agnostic | Configuration lives separately from the entity, which can reduce discoverability |

A common convention: use Fluent API as the primary source of truth (often split into `IEntityTypeConfiguration<T>` classes per entity), reserving annotations for simple, obvious cases.

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Name).HasMaxLength(200).IsRequired();
    }
}

// In OnModelCreating:
modelBuilder.ApplyConfiguration(new UserConfiguration());
```

---

## 3. Querying Data

> **Depth: Intermediate**

When you query using LINQ, EF Core:

1. Builds an **expression tree** representing your LINQ query.
2. **Translates** that expression tree into SQL via the query pipeline.
3. Sends the SQL to the database.
4. **Materializes** the result set back into entity objects (or DTOs/projections).

```csharp
using var context = new AppDbContext(options);

var activeUsers = context.Users
    .Where(u => u.IsActive)
    .ToList(); // Translated to SQL and executed here
```

### Deferred vs. Immediate Execution

An `IQueryable<T>` expression is **not executed** until it's enumerated — by `.ToList()`, `.First()`, `foreach`, etc. Building up a query (chaining `.Where()`, `.OrderBy()`, etc.) only composes the expression tree; nothing hits the database until a terminal operation runs.

```csharp
var query = context.Users.Where(u => u.IsActive); // No SQL executed yet
var count = query.Count();                         // SQL executes here
```

This matters for two common mistakes:
- Accidentally executing the same query multiple times (once per enumeration) because it was never materialized into a list.
- Composing a query across method boundaries without realizing additional `.Where()` calls are still being folded into the same SQL statement (usually beneficial, but surprising if unexpected).

### Client-Side vs. Server-Side Evaluation

EF Core translates as much of the query as possible into SQL (server-side evaluation). Anything it *cannot* translate (arbitrary C# methods, complex custom logic) will either:

- Throw an exception (in modern EF Core versions, for most cases), or
- In some cases, silently pull more data than expected before filtering in memory (client-side evaluation) — this was more permissive in EF Core 2.x and is far more restricted in EF Core 3.0+.

**Rule of thumb:** keep predicates translatable (stick to LINQ operators, simple property access, and EF-supported functions). If you need genuinely custom logic, materialize the data first (`.ToList()`) and then apply it in memory — but be conscious this means you've pulled the full result set into memory.

### `AsNoTracking()`

For read-only queries, disable change tracking to reduce overhead:

```csharp
var readOnlyUsers = context.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .ToList();
```

No tracked snapshots are created, so there's less memory overhead and faster query execution — but you can't call `SaveChanges()` to persist edits to these entities without re-attaching them.

---

## 4. Change Tracking

> **Depth: Intermediate**

When you load entities through a tracking query, EF Core's **Change Tracker** keeps a snapshot of each entity's original values and monitors it for modifications.

```csharp
var user = context.Users.First();
user.Name = "Updated Name"; // EF marks this entity as Modified
```

### Entity States

Every tracked entity has one of five states, exposed via `EntityEntry.State`:

| State | Meaning |
|---|---|
| `Added` | New entity; will be `INSERT`ed on `SaveChanges()`. |
| `Unchanged` | Tracked, matches its original snapshot; no-op on `SaveChanges()`. |
| `Modified` | Tracked, one or more properties differ from the original snapshot; will be `UPDATE`d. |
| `Deleted` | Marked for removal; will be `DELETE`d on `SaveChanges()`. |
| `Detached` | Not tracked by this context's change tracker at all. |

You can inspect or manually set state:

```csharp
var entry = context.Entry(user);
Console.WriteLine(entry.State); // Modified

entry.State = EntityState.Unchanged; // Discard the pending update
```

### Snapshot-Based Change Detection

By default, EF Core uses **snapshot-based** change tracking: it stores a copy of each tracked entity's original property values at the time it was loaded (or added), and compares against that snapshot when `DetectChanges()` runs.

`DetectChanges()` is triggered automatically before operations like `SaveChanges()`, `Find()`, and most query executions — it walks all tracked entities and compares current vs. original values. On large object graphs with many tracked entities, this comparison has a real, measurable cost — one reason `AsNoTracking()` matters for read-heavy workloads.

An alternative is **changed-tracking proxies** (via `Microsoft.EntityFrameworkCore.Proxies`), where entities implement `INotifyPropertyChanged` and notify the tracker immediately on mutation, avoiding the need for a full snapshot comparison — at the cost of requiring virtual properties and a proxy-generated runtime type.

### Tracking vs. No-Tracking, Revisited

| | Tracking (default) | No-Tracking |
|---|---|---|
| Change detection | Yes — snapshot maintained | No |
| Memory overhead | Higher | Lower |
| Use case | Entities you intend to modify and save | Read-only display/reporting queries |
| Identity resolution | Same entity instance returned for repeated queries within a context | Each query returns fresh instances |

---

## 5. Saving Changes

> **Depth: Intermediate**

When you call `SaveChanges()` (or `SaveChangesAsync()`):

1. EF Core runs `DetectChanges()` to find pending changes across all tracked entities.
2. It determines which entities are `Added`, `Modified`, or `Deleted`.
3. It generates the corresponding SQL (`INSERT`/`UPDATE`/`DELETE`) statements, ordered to respect foreign key dependencies.
4. It executes them, by default **wrapped in an implicit transaction** — if any statement fails, the whole batch is rolled back.

```csharp
context.SaveChanges(); // Commits all pending changes in one transaction
```

After a successful save, EF Core updates tracked entities' state back to `Unchanged` (or removes `Deleted` entities from tracking entirely), and refreshes any database-generated values (e.g., identity columns) back onto the in-memory objects.

### Implicit Transactions

If `SaveChanges()` issues multiple SQL statements, they run inside a single implicit transaction automatically — you don't need to wrap a single `SaveChanges()` call in your own transaction for atomicity across those statements. You *do* need an explicit transaction when you need atomicity **across multiple `SaveChanges()` calls**, or across multiple contexts (see Section 9).

### Concurrency Tokens (Preview)

EF Core can detect whether a row changed since it was loaded, using a **concurrency token** (e.g., a `RowVersion`/`Timestamp` column). If a conflicting update is detected during `SaveChanges()`, EF Core throws `DbUpdateConcurrencyException` instead of silently overwriting someone else's change. This is covered in depth in Section 9.

---

## 6. `DbContext` Lifetime & Scope

> **Depth: Intermediate**

- In **web apps** (ASP.NET Core), `DbContext` is typically registered with a **scoped** lifetime — one instance per HTTP request, created by the DI container and disposed at the end of the request via `AddDbContext<T>()`.
- In **desktop/console apps**, there's no request scope to piggyback on — you create and dispose the context explicitly, usually per logical operation or per `using` block:

```csharp
using (var context = new AppDbContext(options))
{
    var users = context.Users.ToList();
} // Disposed here
```

- `DbContext` **is not thread-safe**. A single instance must not be used concurrently from multiple threads — this includes issuing two queries "in parallel" via `Task.WhenAll` against the same context instance, which will throw `InvalidOperationException` (a second operation was started before a previous one completed).

### Concrete Failure Scenarios

- **Registering a `DbContext` as a singleton** and injecting it into request-handling code — leads to threading violations under concurrent requests, and the context never gets a fresh change tracker per unit of work (stale/accumulating tracked entities over the app's lifetime).
- **Capturing a scoped context inside a `Task.Run` or background thread** without its own scope — the context may be disposed by the time the background work runs, or race with the original request thread.
- **Long-lived contexts in long-running processes** (e.g., a Windows Service that keeps one context alive for hours) — the change tracker accumulates entities indefinitely, degrading `DetectChanges()` performance and risking stale data (see Section 13).

---

## 7. Key Features Overview

> **Depth: General**

A quick survey of major `DbContext` capabilities, each expanded further later in this guide:

- **Lazy Loading** *(optional)* — related data is loaded automatically from the database the first time a navigation property is accessed. See Section 8.
- **Eager Loading** — related data is loaded upfront via `.Include()` as part of the initial query. See Section 8.
- **Migrations** — a versioned, code-first way to evolve the database schema alongside your model changes (`dotnet ef migrations add`, `dotnet ef database update`).
- **Transactions** — automatic within a single `SaveChanges()` call; manual, cross-call transactions available via `BeginTransaction()`. See Section 9.

---

## 8. Loading Strategies in Depth

> **Depth: Advanced**

EF Core offers three distinct strategies for loading related (navigation property) data, each with different performance characteristics.

### Eager Loading — `.Include()` / `.ThenInclude()`

Related data is fetched as part of the original query, typically via a SQL `JOIN` (or, with split queries, via multiple separate queries):

```csharp
var orders = context.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items)
        .ThenInclude(i => i.Product)
    .ToList();
```

By default, EF Core generates a single SQL query with joins for all included navigations. For collection navigations, this can cause a **cartesian explosion** — if an order has 10 items and you also include another one-to-many navigation, the row count multiplies, duplicating parent data across rows.

`AsSplitQuery()` mitigates this by issuing separate SQL queries per included collection instead of one large joined query:

```csharp
var orders = context.Orders
    .Include(o => o.Items)
    .AsSplitQuery()
    .ToList();
```

**Trade-off:** split queries avoid duplicated data and cartesian growth, but consistency across the split queries is not guaranteed if data changes between them (no longer a single atomic snapshot), and multiple round-trips replace one.

### Explicit Loading

Load related data on demand, after the parent entity is already loaded, using an explicit call:

```csharp
var user = context.Users.First();

context.Entry(user)
    .Collection(u => u.Orders)
    .Load();
```

Useful when you conditionally need related data and don't want to always eager-load it, but don't want the implicit, easy-to-miss behavior of lazy loading either.

### Lazy Loading

With lazy loading enabled (via proxies or injected lazy-loading delegates), accessing a navigation property triggers a query automatically the first time it's touched:

```csharp
var user = context.Users.First();
var orders = user.Orders; // Triggers a query here, implicitly
```

**The N+1 problem:** lazy loading is the most common source of this anti-pattern — iterating over a collection of parents and accessing a lazy navigation property on each one issues one query per parent, in addition to the original query (`N+1` total queries), instead of one or two well-shaped queries.

```csharp
// N+1 problem: one query for users, then one more per user for orders
foreach (var user in context.Users.ToList())
{
    Console.WriteLine(user.Orders.Count); // Separate query, every iteration
}
```

**When to avoid lazy loading:** in APIs and serialization-heavy contexts especially — accidentally serializing an entity graph can trigger cascading lazy loads across an entire object graph, and can also cause reference cycles if navigation properties point back to their parents (see Section 13).

### Choosing a Strategy

| Strategy | Round trips | Risk | Best for |
|---|---|---|---|
| Eager (`.Include()`) | 1 (or N with split query) | Cartesian explosion on multiple collections | Known, fixed data needs per query |
| Explicit | 1 + 1 per load call | Easy to forget a call, but visible in code | Conditional/on-demand related data |
| Lazy | 1 + 1 per access | N+1 problem, hidden queries, serialization issues | Rarely recommended for APIs; sometimes fine for UI-bound desktop apps with tight object graphs |

---

## 9. Transactions & Concurrency Control

> **Depth: Advanced**

### Manual Transactions

A single `SaveChanges()` call is already atomic. When you need atomicity **across multiple `SaveChanges()` calls** (or across operations against multiple contexts), wrap them in an explicit transaction:

```csharp
using var transaction = context.Database.BeginTransaction();
try
{
    context.Orders.Add(order);
    context.SaveChanges();

    context.Inventory.First(i => i.ProductId == order.ProductId).Quantity -= order.Quantity;
    context.SaveChanges();

    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

For coordinating a transaction across **multiple `DbContext` instances** (or other resources), `TransactionScope` can wrap them under one ambient transaction — but be aware this may escalate to a distributed transaction (requiring a transaction coordinator) depending on the providers involved, which carries additional infrastructure and performance cost. Prefer a single context/transaction where possible; reach for `TransactionScope` only when genuinely coordinating multiple independent resources.

### Optimistic Concurrency Control

Optimistic concurrency assumes conflicts are rare: rather than locking a row while it's read, EF Core checks — at `SaveChanges()` time — whether the row still matches what was originally read.

**Setup**, typically via a `RowVersion`/`Timestamp` column:

```csharp
public class Order
{
    public int Id { get; set; }
    public int Quantity { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```

Or, for any property, via the Fluent API:

```csharp
modelBuilder.Entity<Order>()
    .Property(o => o.Quantity)
    .IsConcurrencyToken();
```

When `SaveChanges()` runs, EF Core includes the original concurrency token value in the `WHERE` clause of the generated `UPDATE`/`DELETE`. If zero rows are affected (because the token no longer matches — someone else updated the row first), EF Core throws `DbUpdateConcurrencyException`.

**Handling the conflict:**

```csharp
try
{
    context.SaveChanges();
}
catch (DbUpdateConcurrencyException ex)
{
    foreach (var entry in ex.Entries)
    {
        var databaseValues = entry.GetDatabaseValues();
        if (databaseValues == null)
        {
            // Row was deleted by another process
        }
        else
        {
            // Decide a resolution strategy, e.g.:
            // - Client wins: entry.OriginalValues.SetValues(databaseValues); then retry SaveChanges()
            // - Database wins: entry.CurrentValues.SetValues(databaseValues); discard local change
            // - Merge: inspect both value sets and combine manually
        }
    }
}
```

### Pessimistic Concurrency (Brief)

Pessimistic concurrency locks a row at read time (e.g., `SELECT ... FOR UPDATE` in PostgreSQL, or explicit locking hints in SQL Server) so no other transaction can modify it until the lock is released. EF Core doesn't provide first-class pessimistic locking APIs — it typically requires raw SQL or provider-specific extensions. It trades throughput for guaranteed conflict avoidance, and is generally reserved for high-contention scenarios where retries under optimistic concurrency would be too costly.

---

## 10. Performance & Query Optimization

> **Depth: Advanced**

### Compiled Queries

For queries executed very frequently with the same shape (differing only in parameter values), `EF.CompileQuery` caches the translation-to-SQL step, avoiding repeated expression-tree compilation overhead:

```csharp
private static readonly Func<AppDbContext, int, User?> GetUserById =
    EF.CompileQuery((AppDbContext ctx, int id) =>
        ctx.Users.FirstOrDefault(u => u.Id == id));

var user = GetUserById(context, 42);
```

In most applications, EF Core's built-in query caching already avoids most of this cost automatically — compiled queries are a targeted optimization for hot paths, not a default practice.

### Avoiding N+1 Systematically

Beyond avoiding lazy loading (Section 8), watch for N+1 patterns introduced through seemingly innocent code — e.g., calling a method that queries the database inside a loop over already-materialized results. Tools like EF Core's logging (`.LogTo(Console.WriteLine)`) or a SQL profiler are the most reliable way to catch these in practice, since they're easy to miss by reading code alone.

### Bulk Operations: `ExecuteUpdate` / `ExecuteDelete`

Loading entities into memory, mutating them, and calling `SaveChanges()` is wasteful for bulk operations affecting many rows. EF Core provides direct, set-based bulk operations that translate to a single `UPDATE`/`DELETE` statement without materializing entities or tracking them:

```csharp
context.Orders
    .Where(o => o.Status == OrderStatus.Cancelled)
    .ExecuteDelete();

context.Orders
    .Where(o => o.CreatedAt < cutoff)
    .ExecuteUpdate(setters => setters
        .SetProperty(o => o.Archived, true));
```

These bypass the change tracker entirely — faster for bulk scenarios, but they also bypass any client-side logic normally triggered by `SaveChanges()` (e.g., interceptors reacting to tracked state changes).

### Query Splitting Trade-offs

Revisit Section 8's `AsSplitQuery()` guidance: use it when eager-loading multiple collection navigations causes row duplication/cartesian growth that measurably hurts performance, but be mindful of the consistency trade-off (multiple round trips instead of one atomic read).

### Indexing from the EF Side

While the database ultimately owns index performance, EF Core migrations are often how indexes get created and maintained. Configure them explicitly through the Fluent API rather than relying on defaults:

```csharp
modelBuilder.Entity<Order>()
    .HasIndex(o => new { o.CustomerId, o.CreatedAt });
```

Reviewing generated SQL and query plans periodically (especially for frequently-run queries) remains necessary — EF Core cannot infer which composite or covering indexes your actual query patterns need.

---

## 11. `DbContext` Pooling & Advanced Lifetime Management

> **Depth: Advanced**

### `AddDbContextPool`

Creating a `DbContext` instance has overhead (service resolution, internal service initialization). `AddDbContextPool<T>` maintains a pool of pre-built context instances that get reset and reused across requests, instead of being newly constructed and disposed each time:

```csharp
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
```

**Constraint:** because instances are reused, a pooled context **must not hold any per-request state** in its own fields/constructor beyond what EF Core manages internally — any custom fields you add to the context class will leak across requests if not carefully reset, since the same instance is handed to a different request next time.

### `IDbContextFactory<T>`

For scenarios without a per-request DI scope to rely on — Blazor Server/WASM components, background services, parallel processing — inject a factory instead of the context directly, and create short-lived contexts on demand:

```csharp
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

public class MyBackgroundService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public MyBackgroundService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task DoWorkAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        // Use context, then it's disposed at the end of this method
    }
}
```

This is the recommended pattern anywhere a component or service has a lifetime that doesn't cleanly map to a single request scope.

---

## 12. Testing with `DbContext`

> **Depth: Advanced**

### Provider Options for Tests

| Approach | Accuracy | Speed | Notes |
|---|---|---|---|
| **EF Core In-Memory provider** | Low — doesn't enforce real relational constraints, doesn't validate real SQL translation | Fast | Convenient, but can pass tests that would fail against a real database (e.g., missed unique constraint violations, different LINQ translation behavior) |
| **SQLite in-memory** | Moderate-high — a real relational engine, real SQL execution | Fast | Much closer to real behavior than the In-Memory provider; still not 100% identical to your production provider (e.g., SQL Server-specific functions/behaviors won't be tested) |
| **Real test database** (containerized or dedicated instance) | Highest | Slower | Closest to production fidelity; typically reserved for integration tests rather than fast unit tests |

**General guidance:** avoid the EF Core In-Memory provider for anything beyond the most trivial checks — its behavioral differences from real relational databases (no real constraint enforcement, different LINQ translation paths) can hide real bugs. SQLite in-memory is a stronger default for integration-style tests that need to run fast; a real (often containerized) database instance is worth the extra time for tests that must validate provider-specific behavior.

### Abstraction for Unit Testing

For true unit tests (isolating logic *around* the context, not the context's own query translation), wrap data access behind a repository interface and substitute a test double — keeping `DbContext` itself out of the unit test entirely, and reserving real-provider tests for the data access layer's own integration tests.

### Seeding Data for Integration Tests

Use `OnModelCreating`'s `HasData()` for static reference data, or explicitly seed within test setup/fixtures for per-test data:

```csharp
using var context = new AppDbContext(options);
context.Database.EnsureCreated();

context.Users.AddRange(
    new User { Name = "Test User 1" },
    new User { Name = "Test User 2" });
context.SaveChanges();

// ... run the test against this seeded context
```

Prefer fresh, isolated data per test (a fresh in-memory/SQLite database per test run) over a shared seeded database, to avoid order-dependent or flaky tests (see the Software Testing Fundamentals guide, Section 10).

---

## 13. Common Pitfalls & Anti-Patterns

> **Depth: Advanced**

- **Long-lived contexts ("context leaks").** Holding a single context alive across many operations (e.g., as a singleton, or across an entire app session) causes the change tracker to accumulate entities indefinitely, slowing `DetectChanges()` and risking stale data — the context never sees changes made by other processes after its entities were first loaded.
- **Over-fetching via unnecessary `.Include()` chains.** Eagerly loading navigation properties "just in case" pulls more data than needed and increases risk of cartesian explosion (Section 8) — include only what the specific operation actually uses.
- **Disabling tracking incorrectly.** Using `AsNoTracking()` on entities you intend to modify and save means those changes won't be detected — you'd need to explicitly re-attach and mark them as modified, which is easy to forget and leads to silently-lost updates.
- **Unintentional lazy loading in APIs.** Serializing an entity graph that has lazy loading enabled can trigger cascading queries across the whole graph, and can cause `ReferenceLoop`/serialization cycle exceptions when navigation properties point back to their parent. Prefer explicit DTOs/projections for API responses instead of serializing tracked entities directly.
- **Not disposing contexts properly outside DI-managed scopes.** In console apps, background threads, or manually constructed contexts, forgetting a `using`/`await using` leaks the underlying connection and any unmanaged resources it holds.
- **Mixing pooled contexts with custom mutable state.** Adding custom fields to a pooled `DbContext` subclass without resetting them (e.g., via overriding `OnConfiguring`/reset hooks) leaks state between logically unrelated requests (Section 11).
- **Ignoring `DbUpdateConcurrencyException`** or catching and swallowing it without a real resolution strategy — silently discards a legitimate conflict signal and risks data loss for one of the two conflicting writers.

---

## 14. Summary Table

> **Depth: General**

| Feature | Purpose | When to Use | Performance Cost |
|---|---|---|---|
| `AsNoTracking()` | Skip change tracking | Read-only queries | Lower overhead than tracked queries |
| `.Include()` / `.ThenInclude()` | Eager load related data | Known, fixed related-data needs | Can cause cartesian growth with multiple collections |
| `AsSplitQuery()` | Avoid cartesian growth | Multiple collection includes | Extra round trips; less atomic consistency |
| Explicit loading | On-demand related data | Conditional related-data needs | One extra query per load call |
| Lazy loading | Automatic on-access loading | Rarely recommended for APIs | High risk of N+1 problem |
| `SaveChanges()` | Persist tracked changes | Standard unit-of-work commit | Implicit transaction; DetectChanges cost scales with tracked entity count |
| Manual transactions | Atomicity across multiple `SaveChanges()`/contexts | Multi-step operations needing all-or-nothing | Extra coordination overhead |
| Optimistic concurrency tokens | Detect conflicting concurrent updates | Multi-user editable data | Cheap in the common (no-conflict) case |
| `ExecuteUpdate`/`ExecuteDelete` | Bulk set-based operations | Large-scale updates/deletes | Much faster than load-then-save for bulk changes; bypasses tracking/interceptors |
| `AddDbContextPool` | Reuse context instances | High-throughput web APIs | Faster context acquisition; requires stateless context design |
| `IDbContextFactory<T>` | Create contexts outside DI scope | Blazor, background services, parallel work | Same per-instance cost as normal, but correctly scoped |

---

## 15. Suggested Exercise

> **Depth: Intermediate**

A ~30–40 minute hands-on activity to practice the concepts, using a lightweight provider (e.g., SQLite):

1. Define a small model — e.g., `Order` and `OrderItem` in a one-to-many relationship — and a `DbContext` with Fluent API configuration for at least one index and one required field.
2. Run a **tracked** query and a **no-tracking** query for the same data; inspect `context.ChangeTracker.Entries()` after each to confirm the difference.
3. Modify a tracked entity's property and call `SaveChanges()`; confirm the generated SQL (via `.LogTo(Console.WriteLine)`) is an `UPDATE`, not an `INSERT`.
4. Add a `RowVersion` concurrency token to one entity. Simulate a conflict: load the same row into two separate context instances, modify and save the first, then modify and save the second — observe the `DbUpdateConcurrencyException` and implement a simple "database wins" resolution.
5. Add a second `Order` with several `OrderItem`s, then load orders with `.Include(o => o.Items)` both with and without `AsSplitQuery()`. Compare the generated SQL and discuss when you'd choose each.
6. Wrap-up discussion: for a hypothetical high-traffic ASP.NET Core API, decide where you'd use `AsNoTracking()`, whether pooling (`AddDbContextPool`) makes sense, and how you'd guard against accidental N+1 queries in code review.

**Goal:** build direct, observable intuition for change tracking, transaction boundaries, and loading strategy trade-offs — not just the API surface, but *why* each choice matters at runtime.
