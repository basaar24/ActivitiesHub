# CQRS Fundamentals: A Tool-Agnostic Reference Guide

This guide explains Command Query Responsibility Segregation (CQRS) from first principles to advanced practice. It is independent of any language, framework, or database. Code samples use neutral pseudocode.

Each section carries a depth label, shown in the heading and repeated at the top of the section body:

- **General**: concepts every developer on the team should know.
- **Intermediate**: what you need to design and implement CQRS in a real system.
- **Advanced**: trade-offs and patterns for distributed, high-scale, or long-lived systems.

## Table of Contents

**Part 1: Foundations**
1. What is CQRS?
2. CQS vs. CQRS
3. The problem CQRS solves
4. Commands, Queries, and Events
5. When to use it and when not to

**Part 2: Core Building Blocks**
6. The write side
7. The read side
8. Handlers and dispatching
9. Validation and error handling

**Part 3: Architecture Variants**
10. Single database, separated models
11. Separate read and write stores
12. Eventual consistency
13. Domain events and integration events

**Part 4: Related Patterns**
14. Event Sourcing
15. Projections and read model rebuilding
16. Outbox pattern and reliable messaging
17. Sagas and process managers
18. Idempotency and concurrency

**Part 5: Practice**
19. Implementing the dispatching pipeline
20. Testing CQRS systems
21. CQRS in Web APIs
22. Common pitfalls and anti-patterns
23. Evolving toward CQRS

---

# Part 1: Foundations

## 1. What is CQRS? — General

> **Depth: General**

CQRS stands for Command Query Responsibility Segregation. The idea is simple: an application uses one model to change state and a different model to read state.

In a traditional layered application, the same entity classes, the same services, and the same database tables serve every operation. Creating an order, listing orders, and generating an order report all pass through the same model. That works well while the application is small, but the two kinds of operations pull the design in opposite directions.

- **Writes** care about rules, invariants, consistency, and transactions. They benefit from a rich model that protects business logic.
- **Reads** care about shape, speed, and convenience. They benefit from flat, precomputed structures that match what a screen or report needs.

CQRS accepts that these are different jobs and gives each its own model, its own code path, and, when justified, its own storage.

```
                     +------------------+
   Command  -------> |   Write model    | ------> State change
 ("do this")         +------------------+

                     +------------------+
   Query    -------> |   Read model     | ------> Data (no side effects)
 ("tell me")         +------------------+
```

The pattern was named by Greg Young, who built on an older principle called Command-Query Separation. It is a design approach, not a product, and it can be applied in a small way (two sets of classes over one database) or a large way (independent services and datastores).

## 2. CQS vs. CQRS — General

> **Depth: General**

The two terms are related but operate at different levels.

**Command-Query Separation (CQS)** is a principle from Bertrand Meyer, applied at the level of individual methods. Every method should be either:

- a **command**, which changes state and returns nothing meaningful, or
- a **query**, which returns data and changes nothing.

A method that both changes state and returns a computed answer is considered a design smell, because callers cannot safely call it twice or reason about it in isolation.

**CQRS** takes the same idea and lifts it to the architectural level. Instead of separating methods on one object, it separates whole models, and often whole execution paths, into a command side and a query side.

| Aspect | CQS | CQRS |
|---|---|---|
| Scope | Single method or object | Application or bounded context |
| Applies to | Method signatures | Models, handlers, and possibly storage |
| Effort to adopt | Discipline only | Structural change |
| Guarantees | Queries have no side effects | Reads and writes evolve independently |

You can follow CQS without doing CQRS. You cannot do CQRS meaningfully without respecting CQS, since a query that changes state would undermine the whole separation.

## 3. The problem CQRS solves — General

> **Depth: General**

A single shared model creates friction in three recurring ways.

**Model conflict.** To make writes safe, a domain model is loaded with validation, relationships, and behavior. To make reads fast, the same data should be flat and joined ahead of time. One model cannot be ideal for both, so it ends up mediocre at each. Typical symptoms are entities polluted with properties that exist only for a screen, and screens forced to load large object graphs just to show three fields.

**Workload asymmetry.** Most systems read far more often than they write, sometimes by ratios of 100 to 1 or more. Yet reads and writes share the same scaling constraints, indexes, and locks. Tuning an index for a heavy report can slow down inserts, and vice versa.

**Growing complexity.** As business rules multiply, a single service class accumulates methods for creating, updating, searching, filtering, exporting, and reporting. It becomes large, hard to test, and risky to change.

CQRS addresses these by letting each side be designed for its own purpose:

- The write side stays small and focused on rules.
- The read side is shaped exactly like the consumers that use it.
- Each side can be optimized, scaled, and changed independently.

It is worth stating clearly that CQRS does not make a system faster or simpler by itself. It moves complexity into a more manageable place, at a cost (see section 5).

## 4. Commands, Queries, and Events — General

> **Depth: General**

Three kinds of messages describe everything that flows through a CQRS system.

### Commands

A command is a request to change state. It expresses intent and is named as an imperative verb phrase: `PlaceOrder`, `CancelSubscription`, `ChangeEmailAddress`.

- It targets exactly one handler.
- It can be rejected (validation failure, broken business rule).
- It should carry only the data needed to perform the action.
- It usually returns nothing, or only an identifier or acknowledgment.

Commands should capture business intent, not raw data edits. `UpdateOrder` is vague; `ShipOrder` and `ChangeShippingAddress` say what the business actually means.

### Queries

A query is a request for information. It is named for what it returns: `GetOrderSummary`, `ListOverdueInvoices`.

- It never changes state.
- It can be called any number of times with no side effects.
- It returns a shape designed for its caller.

### Events

An event is a fact that already happened, named in the past tense: `OrderPlaced`, `EmailAddressChanged`.

- It cannot be rejected, because it is already true.
- It can have zero, one, or many subscribers.
- It is immutable.

| | Command | Query | Event |
|---|---|---|---|
| Meaning | "Do this" | "Tell me this" | "This happened" |
| Tense | Imperative | Interrogative | Past |
| Can fail? | Yes | Yes (technical only) | No |
| Handlers | Exactly one | Exactly one | Zero or more |
| Changes state? | Yes | No | Records a change |

## 5. When to use it and when not to — Intermediate

> **Depth: Intermediate**

CQRS is a tool with a real price. Choosing it should be a deliberate decision, made for one part of a system at a time, not for the entire application by default.

### Signals that CQRS may pay off

- The domain has complex business rules, and reads and writes clearly need different shapes.
- Read and write loads differ greatly and need to scale independently.
- Many different views of the same data exist (dashboards, search, reports, mobile screens).
- Multiple teams work on the same area and collide in shared code.
- The system needs an audit trail or history of changes (a natural fit with event-based approaches).

### Signals that it is probably unnecessary

- The application is mostly create-read-update-delete over simple data.
- The team is small and the domain is well understood and stable.
- Reads and writes look nearly identical.
- The extra code and concepts would outweigh the benefits.

### The costs

- **More code.** Separate models, handlers, and mappings replace one shared class.
- **More concepts** for new team members to learn.
- **Eventual consistency**, if the read and write stores are separated (section 12).
- **Operational overhead**, such as synchronization, monitoring, and rebuilds.

### A useful rule of thumb

Apply CQRS within a **bounded context**, and only where its benefits are visible. A system can use CQRS in its order-processing area while keeping plain CRUD for user preferences. Mixing styles is normal and healthy.

---

# Part 2: Core Building Blocks

## 6. The write side — Intermediate

> **Depth: Intermediate**

The write side is responsible for accepting commands, enforcing business rules, and persisting state changes. It is optimized for correctness, not for display.

### Typical flow

```
Command
   |
   v
Validation (shape and basic rules)
   |
   v
Command handler
   |
   +--> load aggregate from write store
   +--> execute business behavior on the aggregate
   +--> persist the changes (single transaction)
   +--> publish resulting events
```

### The command handler

A handler is a small coordinator. It should not contain business logic itself; it delegates to the domain model.

```
handle(command: ShipOrder):
    order = orders.load(command.orderId)
    order.ship(command.carrier, command.trackingNumber)   # rules live here
    orders.save(order)
```

### The domain model and aggregates

The write model is often built from **aggregates**: clusters of objects treated as a single consistency unit, with one root that guards its invariants. An `Order` aggregate, for instance, ensures that a shipped order cannot be modified and that its total matches its line items.

Design guidelines for the write side:

- Keep aggregates small. Large aggregates cause contention and slow transactions.
- Make one command modify one aggregate in one transaction.
- Expose behavior, not setters. `order.ship(...)` protects rules; `order.status = "shipped"` does not.
- The write model does not need to be queryable. It only needs to load by identity and save.

## 7. The read side — Intermediate

> **Depth: Intermediate**

The read side answers questions as directly as possible. It contains no business rules and no behavior, only data shaped for its consumers.

### Read models

A read model (also called a view model or projection) is a structure tailored to one use case. It is typically flat, denormalized, and contains exactly the fields a screen or report needs.

```
OrderSummary
    orderId
    customerName
    itemCount
    totalAmount
    status
    lastUpdated
```

Instead of loading an order, its customer, and its line items, and then assembling them, the query reads one prepared record.

### Query handlers

```
handle(query: GetOrderSummary):
    return orderSummaries.findById(query.orderId)
```

A query handler should do little more than fetch and return. It should not modify data, publish events, or apply business decisions.

### Design guidelines

- **One read model per use case** is acceptable, and often desirable. Duplication of data across read models is the price of speed and simplicity.
- **Denormalize freely.** Joins are moved from query time to write time.
- **Bypass the domain model.** Queries can go straight to the data using whatever access method is fastest and simplest.
- **Return DTOs** (plain data objects), never domain entities, so callers cannot accidentally depend on write-side behavior.

Because the read side has no invariants to protect, it is the easiest place to optimize with caching, indexes, search engines, or specialized stores.

## 8. Handlers and dispatching — Intermediate

> **Depth: Intermediate**

A CQRS application needs a way to route each message to the right handler. This is the job of the **dispatcher**, often implemented as a command bus and a query bus (or one mediator that does both).

### Responsibilities

- Find the single handler registered for a given command or query type.
- Invoke it and return its result.
- Provide a place for cross-cutting concerns to run before and after the handler.

```
sender  -->  dispatcher  -->  [ behavior 1 ] -> [ behavior 2 ] -> handler
```

### Pipeline behaviors

A **pipeline** wraps every handler in reusable steps. Common behaviors:

| Behavior | Purpose |
|---|---|
| Logging | Record what was requested and how long it took |
| Validation | Reject malformed commands before they reach the handler |
| Authorization | Verify that the caller may perform the action |
| Transaction | Begin, commit, or roll back around the handler |
| Caching (queries) | Return stored results for repeated queries |
| Retry | Repeat on transient failures |

This keeps handlers focused on one job and avoids repeating infrastructure code in every one of them.

### Design guidelines

- A dispatcher decouples callers from handlers: the caller knows the message, not the handler.
- Do not let handlers call other handlers directly. If two operations must be combined, use a domain service, an event, or a higher-level orchestrator.
- A dispatcher is optional. Direct calls to handler objects are still CQRS; the bus is a convenience, not a requirement.

## 9. Validation and error handling — Intermediate

> **Depth: Intermediate**

Failures come in different kinds, and they deserve different treatment.

### Where validation belongs

| Layer | What it checks | Example |
|---|---|---|
| Input / boundary | Shape and format | Email has valid format; quantity is a number |
| Command validation | Rules that need only the command | Quantity greater than zero |
| Domain model | Business invariants that need state | Cannot ship a cancelled order |

Validate cheap things early and enforce business invariants inside the domain model, since that is the only place that always has the current state. Never rely on client-side or boundary checks alone for rules that protect data integrity.

### Types of failure

- **Validation failures**: the command is malformed. Caller should fix and resend.
- **Business rule violations**: the command is well-formed but not allowed in the current state.
- **Concurrency conflicts**: someone else changed the data first (section 18).
- **Technical failures**: infrastructure is unavailable or broken.

### The Result pattern

Rather than using exceptions for expected failures, handlers can return an explicit outcome:

```
Result
    isSuccess
    value          (when successful)
    error          (code + message, when not)
```

Advantages: failures are visible in the method signature, control flow stays predictable, and callers must decide how to respond. Exceptions remain appropriate for truly unexpected technical problems.

### Guidelines

- Use stable error codes so clients can react without parsing messages.
- Return all validation errors at once, not one at a time.
- Do not leak internal details (stack traces, table names) in errors.

---

# Part 3: Architecture Variants

## 10. Single database, separated models — Intermediate

> **Depth: Intermediate**

The simplest form of CQRS keeps **one database** but uses **two models** in code. Commands go through the domain model; queries bypass it and read directly.

```
   Commands                       Queries
      |                              |
      v                              v
 Command handlers             Query handlers
      |                              |
      v                              v
 Domain model                 Direct data access
      |                              |
      +---------- Same database -----+
```

### Why start here

- **No synchronization problem.** Both sides see the same data immediately, so reads are strongly consistent.
- **Low operational cost.** No extra infrastructure to run or monitor.
- **Most of the benefit.** Clean separation of code, focused models, and simpler handlers, without distributed-system complexity.
- **An upgrade path.** If scaling or shape needs grow, the read side can later move to its own store with little change to callers.

### Variations

- Queries read from the same tables the write model uses, with hand-written queries or projections.
- Queries read from **database views** or materialized views that reshape data for reading.
- Queries read from tables that are maintained by the write side within the same transaction.

For many applications, this variant is the right final destination, not merely a stepping stone.

## 11. Separate read and write stores — Advanced

> **Depth: Advanced**

The fuller form of CQRS gives each side **its own datastore**, chosen for its workload.

```
 Command --> Write model --> [ Write store ]
                                  |
                          events / change feed
                                  |
                                  v
                             Projector
                                  |
                                  v
 Query   --> Read model  <-- [ Read store ]
```

### Why do it

- **Independent scaling.** Read replicas or entirely different technology can handle read load without affecting writes.
- **Specialized storage.** The write store may prioritize transactional integrity; the read store may be a document store, a search index, a cache, or a columnar store for analytics.
- **Multiple read models.** The same write events can feed several stores, each tuned for one purpose.
- **Isolation.** Heavy reporting queries cannot lock or slow the write path.

### Synchronization strategies

| Strategy | How it works | Trade-off |
|---|---|---|
| Event-driven | Write side publishes events; projectors update read store | Flexible, decoupled; needs reliable messaging |
| Change data capture | A tool streams committed database changes | Little application code; couples to the write schema |
| Scheduled refresh | Read store rebuilt periodically | Simple; data can be noticeably stale |
| Synchronous dual write | Update both stores in one request | Simple to see, fragile: partial failure causes divergence |

Synchronous dual write is generally discouraged, because two independent stores cannot be updated atomically without heavy coordination. Prefer a design where one store is the source of truth and the other is derived from it.

### The real cost

This variant introduces eventual consistency, projection code, failure handling, and rebuild procedures. Choose it only when a clear need justifies those costs.

## 12. Eventual consistency — Advanced

> **Depth: Advanced**

When reads and writes use separate stores, the read store lags slightly behind the write store. This delay is called **replication lag** or **projection lag**, and the system is said to be **eventually consistent**: given no new writes, all views converge to the same state.

### What it looks like to a user

```
t0   User submits "Change address"   --> command accepted
t1   User is redirected to profile   --> query runs
t2   Read store not yet updated      --> old address displayed
t3   Projector applies the event     --> read store updated
```

Lag is usually milliseconds, but it can grow under load or during failures. The design must tolerate it.

### Handling it in the user experience

- **Acknowledge, don't assume.** Show "Your request was received" instead of pretending the change is complete.
- **Optimistic UI.** Update the screen locally with the expected result, then reconcile with the server.
- **Return the result from the command** (or a version marker) so the client can display it without re-querying.
- **Read-your-own-writes.** Route a user's next read to the write store, or wait until the read store reaches a known version.
- **Polling or push notification.** Refresh the view when the projection catches up.

### Design principles

- Decide per use case how much staleness is acceptable. A dashboard can lag by seconds; an account balance shown before a withdrawal may not.
- Never make business decisions on read-model data when correctness matters. Use the write side, which holds the authoritative state.
- Monitor projection lag as a first-class metric and alert when it exceeds the tolerated limit.

Eventual consistency is not a defect to be eliminated; it is a trade-off to be made consciously and communicated clearly.

## 13. Domain events and integration events — Intermediate

> **Depth: Intermediate**

Events are how the write side tells the rest of the world that something happened. Two kinds are worth distinguishing.

### Domain events

A domain event is raised inside a bounded context when an aggregate changes: `OrderShipped`, `PaymentReceived`.

- Used within the same service or module.
- May carry rich domain detail.
- Can be handled in the same process, often in the same transaction.
- Consumers: projectors, other aggregates, internal side effects.

### Integration events

An integration event is published across boundaries, to other services or systems.

- Deliberately **smaller and more stable** than domain events. It is a public contract.
- Versioned carefully, since consumers you do not control depend on it.
- Delivered through a message broker, asynchronously.
- Should not expose internal model structure.

### Translating between them

```
Aggregate raises domain event
        |
        v
Internal handler
        |
        +--> update read models (in-process)
        +--> map to integration event --> publish to broker
```

### Guidelines

- Name events in the past tense using business language.
- Include enough data for consumers to act, without forcing them to call back.
- Keep events immutable and never repurpose an existing event for a new meaning; introduce a new one.
- Plan for versioning from the beginning (section 18 and section 14 cover related concerns).

---

# Part 4: Related Patterns

## 14. Event Sourcing — Advanced

> **Depth: Advanced**

Event Sourcing is often mentioned alongside CQRS, but they are independent patterns. CQRS separates reads from writes. Event Sourcing changes **how state is stored**.

### The core idea

Instead of storing the current state of an aggregate, store the **sequence of events** that produced it. The current state is derived by replaying those events.

```
Traditional storage:        Event-sourced storage:

Order #42                   Order #42 stream
  status: shipped             1. OrderPlaced
  total: 120.00               2. ItemAdded
                              3. PaymentReceived
                              4. OrderShipped
```

### Why it pairs well with CQRS

- The event stream is a natural source for building read models: projectors subscribe to events and build views.
- The write side stores only events, and the read side provides the queryability that an event store lacks.
- Full history becomes available for auditing, debugging, and analysis.

### Benefits

- Complete, tamper-evident audit trail.
- Ability to rebuild any read model from history.
- Ability to answer questions nobody asked when the system was designed.
- Easy temporal queries ("what did this look like last March?").

### Costs and challenges

- **Event schema evolution.** Events are stored forever; changing their shape requires upcasting or versioning strategies.
- **Learning curve.** The mental model differs from CRUD.
- **Performance of replay.** Long streams need **snapshots** (periodic saved states) to avoid replaying everything.
- **Correcting mistakes.** Events are not edited; errors are fixed by appending compensating events.
- **Privacy.** Deleting personal data from an immutable log needs deliberate design (for example, encryption with discardable keys).

### Key point

You can adopt CQRS without Event Sourcing, and this is by far the more common choice. Adopt Event Sourcing only when its specific benefits, such as auditability or temporal analysis, are genuinely required.

## 15. Projections and read model rebuilding — Advanced

> **Depth: Advanced**

A **projection** is a process that consumes events (or changes) and maintains a read model. It is the bridge between the write side and the read side.

### How a projector works

```
on OrderPlaced(e):
    orderSummaries.insert(id = e.orderId, customer = e.customerName, status = "placed", ...)

on OrderShipped(e):
    orderSummaries.update(id = e.orderId, status = "shipped")
```

Each event type maps to a small, well-defined change in the read model.

### Rebuilding

Because read models are derived data, they are disposable. If a projection has a bug, or a new view is needed, it can be rebuilt from the source of truth:

1. Create a new, empty version of the read model.
2. Replay history (from the event store, or by reading the write store) through the projector.
3. Switch queries to the new version when it catches up.
4. Drop the old one.

This is one of CQRS's strongest operational advantages: the read side can be replaced without risk to the authoritative data.

### Practical concerns

- **Idempotent projectors.** The same event may be delivered twice; applying it again must not corrupt the view (section 18).
- **Ordering.** Events for one aggregate must be applied in order. Partition processing by aggregate identity.
- **Checkpointing.** Store the position of the last processed event so the projector can resume after a restart.
- **Blue/green rebuilds.** Build the new model alongside the old one, then switch, so users see no downtime.
- **Lag monitoring.** Track the gap between the latest event and the last projected one.

## 16. Outbox pattern and reliable messaging — Advanced

> **Depth: Advanced**

A common failure in event-driven systems is the **dual-write problem**: the application must save state to the database *and* publish an event to a broker, but these are two separate systems that cannot share a transaction.

```
save order         --> succeeds
publish event      --> fails (broker down)
Result: state changed, no one is told.

or the reverse:
publish event      --> succeeds
save order         --> fails
Result: event announces something that never happened.
```

### The outbox solution

Write the event into an **outbox table** in the *same database transaction* as the state change. A separate process then reads the outbox and publishes to the broker.

```
+------------- one transaction -------------+
|  update order state                       |
|  insert event into Outbox table           |
+-------------------------------------------+
                    |
                    v
        Relay process reads Outbox
                    |
                    v
             Publish to broker
                    |
                    v
         Mark outbox row as sent
```

Because both writes commit or roll back together, the event is recorded if and only if the state change happened.

### Delivery guarantees

The relay may publish an event and crash before marking it sent, so the same event can be published twice. This gives **at-least-once delivery**. It is achievable and reliable; exactly-once delivery across systems is not realistically attainable. The consequence is that **consumers must be idempotent** (section 18).

### Related concepts

- **Inbox pattern:** the receiving side records processed message IDs to detect duplicates.
- **Change data capture:** a tool reads the database log and publishes changes, removing the need for a polling relay.
- **Dead-letter queues:** messages that repeatedly fail are set aside for inspection instead of blocking the flow.

## 17. Sagas and process managers — Advanced

> **Depth: Advanced**

Some business processes span several aggregates or services, so a single command and a single transaction cannot cover them. Placing an order might involve reserving stock, charging a payment, and arranging shipping, each owned by a different component.

A **saga** (or **process manager**) coordinates such a workflow through a sequence of commands and events, with a plan for failure.

### Two coordination styles

| Style | How it works | Trade-off |
|---|---|---|
| **Choreography** | Each service reacts to events from others; no central coordinator | Loosely coupled; the overall flow is hard to see |
| **Orchestration** | A central saga sends commands and reacts to results | Flow is explicit and easy to follow; the coordinator is a dependency |

### Example (orchestrated)

```
OrderPlaced
   |
   v
Saga --> ReserveStock ------> StockReserved
   |
   +---> ChargePayment ------> PaymentFailed
   |
   +---> ReleaseStock  (compensation)
   |
   +---> CancelOrder
```

### Compensation

There is no distributed rollback. When a later step fails, the saga runs **compensating actions** that logically undo earlier steps: release the reserved stock, refund the payment, mark the order cancelled. Compensations should be designed as carefully as the happy path.

### Guidelines

- Persist saga state so it survives restarts.
- Make every step idempotent, since messages can be redelivered.
- Define timeouts for steps that never respond.
- Give each saga instance a correlation identifier that ties all its messages together.
- Keep sagas for real cross-boundary workflows. Use a simple handler when a single aggregate suffices.

## 18. Idempotency and concurrency — Advanced

> **Depth: Advanced**

Distributed systems retry, duplicate, and reorder. CQRS systems must be built to survive that.

### Idempotency

An operation is **idempotent** if performing it several times has the same effect as performing it once. It is essential because networks fail after work is done, clients resend, and brokers redeliver.

Techniques:

- **Idempotency keys.** The client attaches a unique identifier to each command. The server stores processed keys and returns the previous result for a repeated one.
- **Natural idempotence.** Prefer operations like "set status to shipped" over "increment counter".
- **Deduplication on the consumer side.** Track processed message IDs (inbox pattern).
- **Upserts in projectors.** "Set this row to these values" is safe to repeat; "add one" is not.

### Concurrency

Two users, or two retries, may attempt to change the same aggregate simultaneously.

**Optimistic concurrency** is the usual choice: each aggregate carries a **version number**, and a save succeeds only if the version is unchanged since it was loaded.

```
load order          (version = 7)
apply change
save with expected version = 7
   |
   +-- current version is 7  --> saved, version becomes 8
   +-- current version is 8  --> conflict, reject
```

On conflict, the system can:

1. Reject the command and tell the caller to retry.
2. Reload the aggregate and automatically retry the command.
3. Merge changes when the business rules allow it.

**Pessimistic locking** (holding a lock while working) is simpler to reason about but reduces throughput and risks deadlocks. It is generally avoided for user-facing operations.

### Ordering

Events for a single aggregate must be processed in order, while events across aggregates usually need not be. Partitioning by aggregate identity provides parallelism without breaking order.

---

# Part 5: Practice

## 19. Implementing the dispatching pipeline — Intermediate

> **Depth: Intermediate**

This section describes how to structure the handler and pipeline machinery in practical terms, regardless of language or library. Whether the pieces come from a library or are written by hand, the shape is the same.

### The essential contracts

```
interface Command
interface Query<TResult>

interface CommandHandler<TCommand>
    handle(command) -> Result

interface QueryHandler<TQuery, TResult>
    handle(query) -> TResult

interface Dispatcher
    send(command) -> Result
    ask(query)    -> TResult
```

### A minimal dispatcher

```
class Dispatcher:
    handlers = registry of  messageType -> handler

    send(command):
        handler = handlers.find(typeof(command))
        return pipeline.run(command, handler)
```

### Building the pipeline

Each behavior receives the message and a reference to "the next step":

```
behavior.handle(message, next):
    before(message)
    result = next(message)
    after(result)
    return result
```

Behaviors are chained so that the outermost runs first. A typical order:

1. Logging
2. Authorization
3. Validation
4. Transaction
5. Handler

### Practical guidance

- **Register handlers by convention** (scan the codebase for handler types) so that adding a feature means adding one file, not editing a central list.
- **Organize by feature, not by technical layer.** Keep a command, its validator, and its handler together in one folder. This makes each use case easy to find, read, and delete.
- **Keep handlers thin.** If a handler grows large, its logic probably belongs in the domain model or a domain service.
- **Separate transaction handling.** Commands run in a transaction; queries usually do not need one.
- **Do not overuse the bus.** Simple, in-process code with direct calls is fine; a bus adds indirection that pays off when many cross-cutting behaviors exist.

### Libraries versus hand-written

A small hand-written dispatcher is a few dozen lines and is a good way to learn the pattern. Existing libraries provide the same structure with more features. The important thing is the design, not the tool.

## 20. Testing CQRS systems — Intermediate

> **Depth: Intermediate**

The separation of responsibilities makes CQRS code unusually testable, because each piece has one job and clear inputs and outputs.

### What to test at each level

| Level | Target | Style |
|---|---|---|
| Domain | Aggregates and their business rules | Pure unit tests, no infrastructure |
| Command handlers | Orchestration around the domain | Unit tests with fakes for storage |
| Query handlers | Correct data returned for a read model | Tests against a real or realistic store |
| Projectors | Events produce the expected read model | Given events, expect state |
| Pipeline behaviors | Validation, authorization, and so on | Isolated unit tests |
| End to end | A full flow through the system | A few, targeted integration tests |

### Given / When / Then for aggregates

A clear style for write-side tests:

```
Given:  OrderPlaced, PaymentReceived
When:   ShipOrder
Then:   OrderShipped is raised

Given:  OrderCancelled
When:   ShipOrder
Then:   the command is rejected with "order is cancelled"
```

This format reads like a business specification and works whether the aggregate is event-sourced or state-based.

### Testing projections

Feed a known list of events into the projector and assert on the resulting read model:

```
Given events:  OrderPlaced(id=1), OrderShipped(id=1)
Expect read model:  OrderSummary(id=1, status="shipped")
```

Also test duplicates and out-of-order arrival to verify idempotency.

### Guidelines

- Test **behavior and outcomes**, not internal method calls.
- Avoid mocking the domain model; it is meant to be exercised directly.
- Use realistic data stores for query and projection tests when queries depend on database behavior.
- Test failure paths (validation, concurrency conflicts, duplicate messages) as carefully as success paths.
- Keep a small number of end-to-end tests to confirm wiring; rely on fast unit tests for the rest.

## 21. CQRS in Web APIs — Intermediate

> **Depth: Intermediate**

HTTP maps naturally onto CQRS, but the mapping needs some care.

### Verbs and messages

| HTTP method | Typical role | Notes |
|---|---|---|
| GET | Query | Safe and cacheable; never changes state |
| POST | Command | Create or trigger an action |
| PUT / PATCH | Command | Replace or partially change a resource |
| DELETE | Command | Remove a resource |

A controller or endpoint should do little more than translate an HTTP request into a command or query, dispatch it, and translate the outcome back into an HTTP response.

```
POST /orders/42/ship
   -> build ShipOrder command from route and body
   -> dispatcher.send(command)
   -> map result to HTTP response
```

### Choosing resource styles

- **Resource-oriented:** `PUT /orders/42` with a body. Familiar but can hide intent.
- **Action-oriented:** `POST /orders/42/ship`, `POST /orders/42/cancel`. Expresses business intent clearly and matches commands well.

Both are valid. Action-oriented routes fit CQRS particularly well because each route corresponds to one command.

### Status codes

| Outcome | Status |
|---|---|
| Command completed | 200 OK, 201 Created, or 204 No Content |
| Command accepted for asynchronous processing | 202 Accepted |
| Validation failure | 400 Bad Request or 422 Unprocessable Content |
| Business rule violation or state conflict | 409 Conflict |
| Concurrency conflict | 409 Conflict or 412 Precondition Failed |
| Resource not found (query) | 404 Not Found |

### Asynchronous commands

When a command is processed later (queued, or with a lagging read model), respond with **202 Accepted** plus a way to follow up: a status URL, a correlation identifier, or a notification channel. Do not claim completion prematurely.

### Guidelines

- Keep controllers thin; they contain no business logic.
- Use separate request/response models from commands and read models when the public contract must stay stable.
- Version the API independently of internal message types.
- Provide idempotency-key support on POST commands that clients may retry.

## 22. Common pitfalls and anti-patterns — Intermediate

> **Depth: Intermediate**

Most CQRS difficulties come from a handful of repeated mistakes.

**1. Applying it everywhere.** Using CQRS on simple CRUD areas multiplies code for no benefit. Apply it where the domain justifies it.

**2. Sharing the model across both sides.** If commands and queries still use the same entity classes, the separation exists only in name. The read side should have its own DTOs and read models.

**3. Queries that change state.** A query that updates a "last viewed" timestamp, for instance, breaks the guarantee that queries are safe to repeat. Make such tracking an explicit command or an event.

**4. Fat handlers.** Handlers packed with business logic become miniature transaction scripts. Move rules into aggregates and domain services.

**5. CRUD-style commands.** Commands such as `UpdateCustomer` with a bag of optional fields hide intent and push decisions to the caller. Prefer commands that name business actions.

**6. Returning too much from commands.** Commands that return full data structures blur the line with queries. Return an identifier, a version, or a status, and let the client query for the rest.

**7. Reading from the write side "just this once".** Ad hoc queries against the write model bring back the coupling CQRS was meant to remove. Build a read model instead.

**8. Ignoring eventual consistency.** Assuming the read store is instantly current leads to confusing user experiences and subtle bugs. Design for lag explicitly.

**9. Making business decisions from read models.** Read data may be stale. Decisions that require correctness must use the write side.

**10. Unreliable event publishing.** Saving state and publishing events as two independent steps loses or duplicates events. Use the outbox pattern.

**11. Non-idempotent consumers.** Assuming each message arrives exactly once leads to duplicate records and double side effects.

**12. Over-engineering the infrastructure.** Adding brokers, separate stores, and event sourcing before they are needed. Start with the simplest variant (section 10) and evolve.

### A quick self-check

- Can a new developer find everything about one use case in one place?
- Is each query side-effect free?
- Do handlers contain orchestration but not rules?
- Is every consumer safe against duplicates?
- Is there a clear, documented answer to "how stale can this view be?"

## 23. Evolving toward CQRS — Advanced

> **Depth: Advanced**

Few teams start with a perfect CQRS design. Most arrive at it by gradually restructuring an existing layered or CRUD application. An incremental path reduces risk and lets each step prove its value.

### A staged approach

**Stage 0: Understand the pain.** Identify where a single model is hurting: slow reports, bloated services, blocked teams. Choose one bounded context as the first candidate.

**Stage 1: Separate the code paths.** Split large service classes into individual command and query handlers, still using the same model and database. This alone improves structure and testability.

**Stage 2: Separate the models.** Introduce dedicated read DTOs and query code that bypasses the domain model. Keep the write model focused on behavior. Still one database (section 10).

**Stage 3: Optimize the read side.** Add denormalized tables, views, or caches where queries are slow. Keep them updated within the same transaction to preserve consistency.

**Stage 4: Introduce events.** Raise domain events from aggregates and use them to maintain read models. Adopt the outbox pattern once events cross process boundaries.

**Stage 5: Separate the stores (only if needed).** Move the read model to its own store and accept eventual consistency, with monitoring and rebuild procedures in place (sections 11, 12, 15).

**Stage 6: Consider Event Sourcing (only if needed).** Adopt it in areas where auditability or temporal queries justify the complexity (section 14).

### Migration techniques

- **Strangler approach.** Route new features, or one feature at a time, through the CQRS structure while the old code continues to serve the rest.
- **Parallel run.** Run old and new read paths side by side and compare results before switching.
- **Feature flags.** Switch traffic gradually and roll back quickly.
- **Backfill.** Populate new read models from existing data before cutting over.

### Decision checkpoints

At the end of each stage, ask:

- Did this solve a real problem we could measure?
- Is the added complexity justified by what we gained?
- Is it acceptable to stop here?

Stopping early is a legitimate outcome. Many systems settle happily at Stage 2 or 3.

---

## Appendix: Glossary

| Term | Meaning |
|---|---|
| **Aggregate** | A cluster of domain objects treated as a single consistency boundary, with one root |
| **Bounded context** | A boundary within which a particular model and vocabulary apply |
| **Command** | A request to change state, expressing intent |
| **Query** | A request for data that causes no side effects |
| **Domain event** | A fact that happened inside a bounded context |
| **Integration event** | A stable, published event meant for other systems |
| **Read model / projection** | A structure shaped for reading, derived from write-side changes |
| **Projector** | A process that keeps a read model up to date |
| **Eventual consistency** | A state in which views converge after some delay |
| **Idempotency** | The property that repeating an operation gives the same result as doing it once |
| **Outbox** | A table that records events atomically with state changes for later publishing |
| **Saga** | A coordinated, compensable workflow across several aggregates or services |
| **Event Sourcing** | Storing state as a sequence of events instead of current values |
| **Snapshot** | A saved aggregate state used to shorten event replay |
| **Optimistic concurrency** | Detecting conflicting changes with a version check at save time |
