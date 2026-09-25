# Prompt: Install Graphify + OpenSpec in EventsHub

Copy everything below the line into a fresh AI agent session (or follow it
yourself step by step). It is self-contained — the agent doesn't need any
other context from this repo's history.

---

You are working in the `EventsHub` repository (repo root =
`c:\sources\uaa\2026\EventsHub`, a Windows machine, PowerShell as the primary
shell). It's a two-app repo: a .NET 10 backend under `src/`/`tests/`, and a
React + TypeScript + Vite frontend under `web/`. Your task is to install two
developer-tooling packages into this repo and verify both work:

1. **Graphify** ([Graphify-Labs/graphify](https://github.com/safishamsi/graphify),
   PyPI package `graphifyy`) — turns the codebase into a queryable knowledge
   graph, exposed as a `/graphify` skill for Claude Code (and other AI
   coding assistants).
2. **OpenSpec** ([Fission-AI/OpenSpec](https://github.com/Fission-AI/OpenSpec),
   npm package `@fission-ai/openspec`) — a spec-driven development workflow:
   change proposals → specs → implementation, as plain Markdown under
   `openspec/`.

Neither tool is a project dependency (nothing goes in `.csproj` or
`package.json`) — both are installed as global CLIs on the developer machine
and then initialized once per repo, writing their own config/output
directories into the repo.

## Step 1 — Install Graphify

Prerequisites: Python 3.10+ and `uv` (or `pipx`).

```powershell
# Prereqs, if not already present
winget install astral-sh.uv
python --version   # confirm 3.10+

# Install (isolated environment, recommended)
uv tool install graphifyy
# Alternative if uv isn't available: pipx install graphifyy
```

Confirm the CLI is on PATH:

```powershell
graphify --version
```

### Register the Claude Code skill

This repo is primarily used with Claude Code (see `.claude/skills/` already
in the repo), so register it project-scoped so the registration is committed
to git for other contributors:

```powershell
graphify install --project
```

If you also use another assistant in this repo, register it too, e.g.:

```powershell
graphify install --platform cursor
```

### Exclude noisy paths before the first build

Create `.graphifyignore` at the repo root (same syntax as `.gitignore`) so
the graph isn't polluted by build output, dependencies, and generated code:

```
web/node_modules/
web/dist/
**/bin/
**/obj/
src/EventsHub.OpenApi/Generated/
src/openapi/
```

(`src/EventsHub.OpenApi/Generated/` and `src/openapi/` are NSwag-generated —
see `docs/Architecture.md` — not worth graphing as "real" code.)

### Build the graph

From an AI assistant session in this repo, run:

```
/graphify .
```

(Or from a plain shell: `graphify .` — no leading slash outside an
assistant.)

This produces `graphify-out/` containing `graph.html` (interactive viewer),
`GRAPH_REPORT.md` (summary + suggested questions), and `graph.json`
(queryable structure). Per Graphify's own guidance, commit `graphify-out/`
to git, but exclude `graphify-out/cost.json` (and any cache subfolder it
creates) via `.gitignore`.

### Verify

- `graphify-out/graph.html` opens in a browser and shows a non-empty graph
  covering both `src/` (C#) and `web/src` (TypeScript/React) — Graphify
  supports both languages via tree-sitter, so a correct install should graph
  the whole repo, not just one side.
- `GRAPH_REPORT.md` mentions real entities from this repo (e.g.
  `EventsController`, `AppDbContext`, `Event`), not a placeholder/empty
  report.

### Keep the graph fresh — auto-rebuild on every commit

By default the graph only updates when someone runs `/graphify .` (or
`graphify .`) by hand. To rebuild it automatically, install Graphify's own
git hooks from the repo root:

```powershell
graphify hook install
```

This writes `post-commit` and `post-checkout` hooks (platform-agnostic
shell scripts) into `.git/hooks/`. From then on, every commit diffs against
`HEAD~1`, and if any graphed file changed, `graph.json` and
`GRAPH_REPORT.md` are rebuilt as part of the commit — deterministic AST
parsing, no LLM call, effectively free for code-only commits. Switching
branches (`post-checkout`) triggers the same rebuild so the graph matches
whatever's checked out. The hook is designed to never fail your commit: if
the rebuild errors, it exits `0` and the commit still goes through.

Useful companions:

```powershell
graphify hook status      # confirm the hooks are installed and active
graphify hook uninstall   # remove them again
```

Since `.git/hooks/` isn't tracked by git, **every contributor who wants the
auto-rebuild needs to run `graphify hook install` once themselves** after
cloning — it's not something that comes along automatically when they pull
`graphify-out/` or `.graphifyignore` from the repo.

## Step 2 — Install OpenSpec

Prerequisite: Node.js 20.19.0+.

```powershell
node --version   # confirm >= 20.19.0
npm install -g @fission-ai/openspec@latest
openspec --version
```

### Initialize in this repo

From the repo root:

```powershell
openspec init --tools claude
```

This creates:
- `openspec/specs/` — the current, agreed specs
- `openspec/changes/archive/` — completed change proposals, archived
- `.claude/skills/openspec-*/SKILL.md` — one skill file per OpenSpec
  workflow you selected, using this repo's existing skill-discovery
  convention (same mechanism as `.claude/skills/architecture-docs/`) — no
  separate import step needed, a fresh Claude Code session picks these up
  automatically
- `.claude/commands/opsx/<id>.md` — command files, invoked as `/opsx:propose`,
  `/opsx:apply`, etc.

If you use another assistant alongside Claude Code, pass a comma-separated
list instead, e.g. `--tools claude,cursor`. Run `openspec init --help` to
see the full list of supported `--tools` IDs.

### Verify

```powershell
openspec list       # should run without error, showing an empty specs/changes list on a fresh install
openspec validate    # should report no structural issues
```

Also confirm `/opsx:propose` (or the equivalent listed by `openspec init`'s
"Getting started" hint) shows up as an available slash command in a new
Claude Code session.

## Step 3 — Report back

Once both are installed, summarize:
- Graphify and OpenSpec CLI versions (`graphify --version`, `openspec --version`)
- Whether `graphify-out/graph.html` renders a real graph of this repo
- Whether `graphify hook status` shows the post-commit/post-checkout hooks installed
- Whether `openspec list` / `openspec validate` run cleanly
- Any files git now shows as new/untracked (`git status`) so the user can
  review before committing — do not commit anything yourself.

---

## Notes for whoever runs this (not part of the agent prompt)

- Both tools are installed **globally** on the machine running them — every
  contributor who wants to use `/graphify` or the OpenSpec workflow needs to
  run Step 1/Step 2's install commands themselves once. Only the
  *initialized, repo-scoped* output (`graphify-out/`, `.graphifyignore`,
  `openspec/`, `.claude/skills/...`) is meant to be committed.
- `graphify hook install` writes to `.git/hooks/`, which git never tracks —
  so the auto-rebuild-on-commit behavior is also per-machine. Each
  contributor who wants commits/checkouts to auto-refresh the graph needs to
  run `graphify hook install` themselves; there's no way to ship that setup
  via a committed file the way `.graphifyignore` can be.
- Neither tool touches `EventsHub.slnx`, any `.csproj`, or `web/package.json`
  — if a future agent run proposes editing those to "add" Graphify/OpenSpec,
  that's a sign it misunderstood the install model above.
- `openspec update` regenerates the tool-specific config files after an
  OpenSpec version bump; re-run it after upgrading the CLI.
