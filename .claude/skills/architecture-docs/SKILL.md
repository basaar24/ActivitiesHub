---
name: architecture-docs
description: Document a codebase's architecture and generate README files for each project/component in a repo. Use this whenever the user asks to "document the architecture," "write a README," "explain how this project is structured," "create developer docs," or similar — for any tech stack, not just .NET. Also trigger when the user is onboarding to an unfamiliar repo and wants an architecture overview, or when adding a new service/app to a repo that needs its own README. Works across mixed-stack monorepos (backend + frontend + services in different languages) without assuming a single framework.
---

# Architecture & README Documentation

A workflow for producing an accurate architecture document plus per-component
README files for a repository, regardless of language or framework. The
process is discovery-first and approval-gated: never assume the stack, and
never generate full content before the outline is approved.

## When to use this

Trigger on requests like:
- "Document the architecture of this app"
- "Write me a README for the API / frontend / this project"
- "I need docs for onboarding new devs"
- "Explain how the pieces of this repo fit together"

Do NOT skip straight to writing docs from memory or assumptions about the
stack — always run Phase 1 first, even if the user seems to expect a fast
answer. Inaccurate architecture docs are worse than none.

## Phase 1 — Discovery (no writing yet)

1. Walk the repository structure to identify every distinct project/component:
   backend API(s), web frontend(s), mobile app, worker/background services,
   shared libraries, infra/config. Don't assume which of these exist —
   detect them from what's actually there.
2. Detect the stack per component from real signals: solution/workspace
   files, manifest/dependency files (`.csproj`, `package.json`,
   `pyproject.toml`, `go.mod`, `pom.xml`, `Cargo.toml`, `Gemfile`, etc.),
   Dockerfiles, lockfiles, or monorepo config (`nx.json`, `turbo.json`,
   `lerna.json`, workspace fields). Different components in the same repo
   may use different stacks — expect and handle that.
3. For each component, determine: purpose, language/framework, key
   dependencies, how to run it locally, how it's tested, and how it relates
   to the other components (project references, HTTP/RPC calls, message
   queues, shared packages).
4. Identify the architectural pattern(s) actually in use (layered, CQRS,
   hexagonal/clean architecture, MVC, microservices, monorepo with shared
   packages, etc.) from the real code and folder layout — never claim a
   pattern the code doesn't actually follow.
5. Note inconsistencies across components (e.g. one service diverges from
   the rest in pattern or stack). Flag these rather than smoothing them
   into a uniform-looking story.

## Phase 2 — Proposed outline (stop and wait for approval here)

Present, but do not yet write in full:

1. **Architecture document outline** (single doc covering the whole system):
   component/system diagram, end-to-end flow of a typical request or
   process, key architectural decisions, and cross-cutting conventions found
   (naming, folder structure, error handling, auth).
2. **Per-component README outline**: one outline per component, tailored to
   that component's own stack and maturity — do not force every README into
   an identical template if the components genuinely differ (a Python
   worker's README doesn't need the same sections as a React frontend's).

Explicitly ask the user to review and approve before moving to Phase 3.

## Phase 3 — Generation (only after approval)

1. Write the architecture document section by section; if it's long, check
   in with the user at natural breakpoints rather than dumping it all at
   once.
2. Write each README from its approved outline, in clear original prose —
   not copied from code comments or boilerplate. Keep framing tool-agnostic
   except where the specific stack is genuinely relevant to that section.
3. Verify every install/run/test command actually works against this repo's
   real manifest and package manager — read the actual scripts/config,
   never assume generic commands for "a Python project" or "a .NET project."

## Output format

- Architecture document: Markdown, at the repo root or in `/docs`.
- README: one per component, placed at that component's own root folder.
- Diagrams: match whatever convention the repo already uses (ASCII vs.
  Mermaid); default to Mermaid for component/flow diagrams if there's no
  existing precedent.
- If components differ significantly in structure, maturity, or stack, say
  so explicitly in the outline and in the final docs rather than presenting
  a falsely uniform picture.
