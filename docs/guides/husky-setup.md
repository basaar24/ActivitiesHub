# Installing Husky in a Project: .NET and npm

Husky runs scripts automatically on Git hooks (pre-commit, commit-msg, pre-push, etc.) so formatting, linting, and tests run before code reaches the repository.

## How to use this guide

Pick **one** part below and follow it from top to bottom. Every part is self-contained: it has its own prerequisites, numbered steps, verification, team/CI setup, and troubleshooting, so you never need to jump to another part.

| Your repo                                                                                     | Follow     |
| --------------------------------------------------------------------------------------------- | ---------- |
| .NET backend **and** an npm frontend in the same repo (e.g. EventsHub with `src/` and `web/`) | **Part 1** |
| Only .NET (a `.sln` / `.slnx`, no `package.json`)                                             | **Part 2** |
| Only npm, with `package.json` in the repo root                                                | **Part 3** |
| Only npm, with `package.json` in a subfolder (e.g. `frontend/`)                               | **Part 4** |

## Where to run the commands (read this once)

- **Repo root** = the folder that contains the `.git` folder and, in .NET repos, the `.sln` / `.slnx` file. In EventsHub this is the folder with `EventsHub.slnx`.
- Git hooks belong to the repository, not to one project. Git has **one** `core.hooksPath` setting per repo and runs hooks from the repo root.
- So, unless a step says otherwise, **run every command from the repo root**. Steps that must run somewhere else (for example inside `web/`) say so in a **Run from:** line.
- All commands are written for **PowerShell**. Hook files are plain text: edit them with a code editor and keep **LF** line endings (not CRLF).

> **Validation status.** The npm parts (3 and 4) were run end to end in a scratch repo with Husky 9.1.7 on Node 22: install, `husky init`, `lint-staged`, `commitlint`, teammate `npm install`, `npm ci --ignore-scripts`, `HUSKY=0`, and the subfolder setup. Those runs used a bash shell, so PowerShell-only lines (like `Add-Content` and `$env:HUSKY`) were not executed. The .NET parts (1 and 2) were checked against the official Husky.Net documentation but **not run**, because no .NET SDK was available. In particular the `dotnet format` task, the `commit-msg` hook contents, and the mixed .NET + npm setup in Part 1 are **untested**: try them on a branch first.

---

# Part 1: .NET + npm in the same repo (e.g. EventsHub)

Use this when one repository holds both a .NET solution and an npm project.

**Run from:** the **repo root** (the folder with `EventsHub.slnx` and `.git`), except where a step says `web/`.

Example layout:

```
EventsHub/                  <- repo root: run commands here
├── .git
├── .config/                <- dotnet-tools.json lives here
├── EventsHub.slnx
├── src/
│   ├── EventsHub.Api/
│   ├── EventsHub.Application/
│   └── ...
├── tests/
└── web/                    <- package.json (React/Vite) lives here
```

**How this works.** Husky.Net and npm Husky each set `core.hooksPath` to their own folder, and the last one to run wins, so installing both the normal way silently disables one of them. This part avoids that: **Husky.Net is the only hook manager, installed at the repo root**, and it calls the frontend checks (`lint-staged`) from its task runner. You do **not** install the npm `husky` package and you do **not** add a `prepare` script.

## Step 1: Check prerequisites

**Run from:** anywhere.

```powershell
git --version
dotnet --version
node --version
npm --version
```

The repo must already be a Git repo (`git status` works). If it isn't: `git init` in the repo root. Having at least one commit is recommended so you can test the hooks.

## Step 2: Install Husky.Net as a local tool

**Run from:** the repo root.

```powershell
dotnet new tool-manifest      # skip if .config/dotnet-tools.json already exists
dotnet tool install Husky
```

A local tool (not global) means every teammate and CI restores the same version from the manifest.

## Step 3: Install the Git hooks

**Run from:** the repo root.

```powershell
dotnet husky install
git config core.hooksPath
```

The last command should print `.husky`.

## Step 4: Attach Husky to a .NET project (auto-install for teammates)

**Run from:** the repo root.

```powershell
dotnet husky attach src/EventsHub.Api/EventsHub.Api.csproj
```

This adds an MSBuild `Husky` target to the `.csproj`. It runs after NuGet restore (on `dotnet restore` / `dotnet build`), runs `dotnet tool restore` and `dotnet husky install`, and is skipped when the `HUSKY` environment variable is `0`.

It also sets a `HuskyRoot` property: the relative path from the project folder to the repo root. The default `../../` is correct for a project directly under `src/`, such as `src/EventsHub.Api/`. If your `.csproj` sits at a different depth, open it and adjust `HuskyRoot` (keep the trailing slash).

## Step 5: Install lint-staged (and optionally Prettier) in the web project

**Run from:** `web/` (this is the one step that does not run at the repo root).

```powershell
cd web
npm install --save-dev lint-staged
```

Add a `lint-staged` section to `web/package.json`. Only list tools that are **installed in that project**: a command that isn't installed makes the hook fail with "'prettier' is not recognized". The version below uses only `eslint`; the optional Prettier section right after it shows how to add Prettier.

Copy the next json to the bottom of the `web/package.json` file.

```json
"lint-staged": {
  "*.{js,jsx,ts,tsx}": "eslint --fix"
}
```

### Optional: add Prettier (code formatting)

**Run from:** `web/` (still the same folder as the install above).

1. Install it:
   ```powershell
   npm install --save-dev prettier
   ```
2. Create `web/.prettierrc`. Pick options that match your **current** code style, otherwise Prettier rewrites every file it touches. The Vite React-TS template uses no semicolons and single quotes, so this is a likely match, but check your files. Copy the next json to the file created:
   ```json
   {
     "semi": false,
     "singleQuote": true
   }
   ```
3. Create `web/.prettierignore` so generated files are skipped. Copy the next text to the file created:
   ```
   dist
   node_modules
   package-lock.json
   ```
4. Replace the `lint-staged` section in `web/package.json` with this version. ESLint runs first, then Prettier:
   ```json
   "lint-staged": {
     "*.{ts,tsx}": ["eslint --fix --max-warnings=0", "prettier --write"],
     "*.{json,md,css}": "prettier --write"
   }
   ```
5. (Optional) Format the existing code once, in its **own commit**, so it doesn't mix with the Husky setup. Hooks are skipped for this one commit because it only reformats files. (Run from the folder you are in; `git add .` stages only that folder.)
   ```powershell
   npx prettier --write .
   git add .
   git commit --no-verify -m "style: format code with prettier"
   ```

**If ESLint and Prettier ever fight over a rule** (for example, ESLint reports a semicolon or quote style that Prettier just changed), turn off ESLint's formatting rules with `eslint-config-prettier`.

**Run from:** `web/`.

1. Install it:
   ```powershell
   npm install --save-dev eslint-config-prettier
   ```
2. Open `web/eslint.config.js`. Add this import at the top, with the other imports:
   ```js
   import prettier from "eslint-config-prettier/flat";
   ```
3. Add `prettier` as the **last** entry of the exported config array, so it can switch off any earlier rule that conflicts with Prettier:
   ```js
   export default defineConfig([
     globalIgnores(["dist"]),
     {
       // ...your existing config object stays as it is
     },
     prettier, // keep this last
   ]);
   ```
   If your file exports a plain array instead of using `defineConfig`, add `prettier` at the end of that array in the same way.
4. Check that ESLint still runs without errors:
   ```powershell
   npx eslint .
   ```

`eslint-config-prettier` only turns rules off. Prettier still does the formatting itself, through the `lint-staged` rule from step 4.

Then go back to the repo root:

```powershell
cd ..
```

Do **not** run `npx husky init` here and do **not** add a `"prepare": "husky"` script: that would overwrite the `core.hooksPath` that Husky.Net set in Step 3.

## Step 6: Add the pre-commit hook

**Run from:** the repo root.

```powershell
dotnet husky add pre-commit -c "dotnet husky run --group pre-commit"
git add .husky/pre-commit
```

Pass the hook **name** (`pre-commit`), not a path.

## Step 7: Configure the tasks

**Run from:** the repo root (the file is `.husky/task-runner.json`).

Create or edit `.husky/task-runner.json` with one task for the .NET code and one for the frontend. Each task's `group` ties it to a hook, and `dotnet husky run --group pre-commit` runs every task in that group:

```json
{
  "tasks": [
    {
      "name": "Format staged C# files",
      "group": "pre-commit",
      "command": "dotnet",
      "args": ["format", "--include", "${staged}"],
      "include": ["**/*.cs"]
    },
    {
      "name": "Lint web",
      "group": "pre-commit",
      "command": "npx",
      "args": ["lint-staged"],
      "cwd": "web"
    }
  ]
}
```

- `${staged}` expands to the files currently staged in Git.
- `cwd` makes the task run inside `web/`, so you don't need a `cd` in the hook.
- The official Husky.Net example calls the standalone `dotnet-format` tool. On current .NET SDKs `dotnet format` is built in, so this example uses that. If it complains about file paths, add `"pathMode": "relative"` (or `"absolute"`) to the task.

Test the tasks without committing:

```powershell
dotnet husky run --group pre-commit
```

## Step 8 (optional): Enforce a commit message format

**Run from:** the repo root.

```powershell
dotnet husky add commit-msg
```

Open the generated `.husky/commit-msg` and add these lines **below** whatever Husky generated (keep its first lines as they are):

```bash
msg=$(cat "$1")
pattern='^(feat|fix|chore|docs|refactor|test|hotfix)(\(.+\))?: .{1,}'

if ! echo "$msg" | grep -qE "$pattern"; then
  echo "Invalid commit message. Use: type: description (e.g. feat: add login form)"
  exit 1
fi
```

## Step 9: Verify it works

**Run from:** the repo root.

```powershell
git add .
git commit -m "chore: add husky"
```

The tasks should run before the commit is created. If you did Step 8, this should be blocked:

```powershell
git commit --allow-empty -m "bad message"
```

Finally, confirm the hooks folder is still Husky.Net's:

```powershell
git config core.hooksPath
```

It should print `.husky`. If it prints something ending in `/_`, npm Husky overwrote it: remove any `"prepare": "husky"` script from `web/package.json` and run `dotnet husky install` again.

## Step 10: Commit the Husky files

**Run from:** the repo root.

```powershell
git add .husky .config src/EventsHub.Api/EventsHub.Api.csproj web/package.json web/package-lock.json web/.prettierrc web/.prettierignore web/eslint.config.js
git commit -m "chore: add husky git hooks"
```

Leave out `web/.prettierrc` and `web/.prettierignore` if you skipped Prettier, and `web/eslint.config.js` if you didn't change it.

## Step 11: Setup for a new teammate or CI

**Run from:** the repo root after cloning.

```powershell
git clone <repo-url>
cd <repo>
dotnet tool restore
dotnet build
cd web
npm install
cd ..
```

The restore step of `dotnet build` triggers `dotnet husky install` automatically (thanks to Step 4). In CI you usually don't need hooks: set the environment variable `HUSKY=0` to skip the install.

## Skipping hooks (use sparingly)

```powershell
git commit -m "wip" --no-verify      # skip hooks for one commit
$env:HUSKY = "0"                     # skip Husky for this PowerShell session
Remove-Item Env:HUSKY                # turn it back on
```

## Troubleshooting (Part 1)

| Problem                                                                  | Fix                                                                                                                                                     |
| ------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Hooks don't run                                                          | From the repo root run `dotnet husky install`, then check `git config core.hooksPath` (should be `.husky`)                                              |
| `core.hooksPath` ends in `/_`                                            | npm Husky overwrote it. Remove `"prepare": "husky"` from `web/package.json` and re-run `dotnet husky install`                                           |
| `dotnet husky` not found                                                 | From the repo root run `dotnet tool restore`                                                                                                            |
| The web task fails with `'prettier' is not recognized` (or another tool) | `lint-staged` runs the commands listed in `web/package.json`: remove the rule, or install the tool with `npm install --save-dev prettier` inside `web/` |
| The web task fails with "command not found"                              | Run `npm install` inside `web/` so `lint-staged` is installed                                                                                           |
| `dotnet format` task errors on file paths                                | Add `"pathMode": "relative"` (or `"absolute"`) to the task                                                                                              |
| Hook fails with `\r` errors on Windows                                   | Save the hook file with LF line endings, not CRLF                                                                                                       |
| Hook doesn't run from a GUI client (Visual Studio, VS Code, Git GUI)     | Make sure `dotnet` and `node` are on the PATH available to that app, then restart it                                                                    |
| Hook fails on CI                                                         | Set `HUSKY=0` in the CI environment                                                                                                                     |

---

# Part 2: .NET only (Husky.Net)

Use this when the repo has a .NET solution and no npm project.

**Run from:** the **repo root** (the folder with the `.sln` / `.slnx` file and `.git`) for every step.

## Step 1: Check prerequisites

**Run from:** anywhere.

```powershell
git --version
dotnet --version
```

The repo must already be a Git repo (`git status` works). If it isn't: `git init` in the repo root. Having at least one commit is recommended so you can test the hooks.

## Step 2: Create a local tool manifest

**Run from:** the repo root.

Check whether `.config/dotnet-tools.json` already exists. If it does, skip this step. Otherwise:

```powershell
dotnet new tool-manifest
```

## Step 3: Install Husky as a local tool

**Run from:** the repo root.

```powershell
dotnet tool install Husky
```

A local tool (not global) means every teammate and CI restores the same version from the manifest.

## Step 4: Install the Git hooks

**Run from:** the repo root.

```powershell
dotnet husky install
git config core.hooksPath
```

This sets Husky up for the repo and points Git's `core.hooksPath` to the `.husky` folder. The second command should print `.husky`.

## Step 5: Attach Husky to a project (auto-install for teammates)

**Run from:** the repo root.

```powershell
dotnet husky attach src/YourProject/YourProject.csproj
```

Attach it to the main/API project. This adds an MSBuild `Husky` target to the `.csproj` that runs after NuGet restore (on `dotnet restore` / `dotnet build`). It runs `dotnet tool restore` and `dotnet husky install`, and is skipped when the `HUSKY` environment variable is `0`.

It also sets a `HuskyRoot` property: the relative path from the project folder to the repo root. The default `../../` is correct for a project at `src/YourProject/`. If your `.csproj` sits at a different depth, open it and adjust `HuskyRoot` (keep the trailing slash).

Multi-project solution? Instead of attaching every project, put the target once in a root `Directory.Build.targets` file (not `Directory.Build.props`) with `HuskyRoot` set to `$(MSBuildThisFileDirectory)`.

## Step 6: Add the pre-commit hook

**Run from:** the repo root.

```powershell
dotnet husky add pre-commit -c "dotnet husky run --group pre-commit"
git add .husky/pre-commit
```

Pass the hook **name** (`pre-commit`), not a path. Other examples:

```powershell
dotnet husky add pre-push -c "dotnet test"
dotnet husky add commit-msg
```

## Step 7: Configure the tasks

**Run from:** the repo root (the file is `.husky/task-runner.json`).

Create or edit `.husky/task-runner.json`. Each task has a `group` that ties it to a hook; `dotnet husky run --group pre-commit` runs every task in that group. Example that formats only the staged C# files:

```json
{
  "tasks": [
    {
      "name": "Format staged C# files",
      "group": "pre-commit",
      "command": "dotnet",
      "args": ["format", "--include", "${staged}"],
      "include": ["**/*.cs"]
    }
  ]
}
```

`${staged}` expands to the files currently staged in Git, so the task only touches what you are about to commit. Optional second task, added to the same `tasks` array:

```json
{
  "name": "Build solution",
  "group": "pre-commit",
  "command": "dotnet",
  "args": ["build", "--no-restore"]
}
```

The official Husky.Net example calls the standalone `dotnet-format` tool. On current .NET SDKs `dotnet format` is built in, so this example uses that. If it complains about file paths, add `"pathMode": "relative"` (or `"absolute"`) to the task.

Test the tasks without committing:

```powershell
dotnet husky run --group pre-commit
```

## Step 8 (optional): Enforce a commit message format

**Run from:** the repo root.

```powershell
dotnet husky add commit-msg
```

Open the generated `.husky/commit-msg` and add these lines **below** whatever Husky generated (keep its first lines as they are):

```bash
msg=$(cat "$1")
pattern='^(feat|fix|chore|docs|refactor|test|hotfix)(\(.+\))?: .{1,}'

if ! echo "$msg" | grep -qE "$pattern"; then
  echo "Invalid commit message. Use: type: description (e.g. feat: add login form)"
  exit 1
fi
```

## Step 9: Verify it works

**Run from:** the repo root.

```powershell
git add .
git commit -m "chore: add husky"
```

The `pre-commit` task should run before the commit is created. If you did Step 8, this should be blocked:

```powershell
git commit --allow-empty -m "bad message"
```

## Step 10: Commit the Husky files

**Run from:** the repo root.

```powershell
git add .husky .config src/YourProject/YourProject.csproj
git commit -m "chore: add husky git hooks"
```

## Step 11: Setup for a new teammate or CI

**Run from:** the repo root after cloning.

```powershell
git clone <repo-url>
cd <repo>
dotnet tool restore
dotnet build
```

The restore step of `dotnet build` triggers `dotnet husky install` automatically (thanks to Step 5). In CI you usually don't need hooks: set the environment variable `HUSKY=0` to skip the install.

## Skipping hooks (use sparingly)

```powershell
git commit -m "wip" --no-verify      # skip hooks for one commit
$env:HUSKY = "0"                     # skip Husky for this PowerShell session
Remove-Item Env:HUSKY                # turn it back on
```

## Troubleshooting (Part 2)

| Problem                                                              | Fix                                                                                                        |
| -------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Hooks don't run                                                      | From the repo root run `dotnet husky install`, then check `git config core.hooksPath` (should be `.husky`) |
| `dotnet husky` not found                                             | From the repo root run `dotnet tool restore`                                                               |
| `dotnet format` task errors on file paths                            | Add `"pathMode": "relative"` (or `"absolute"`) to the task                                                 |
| Hook fails with `\r` errors on Windows                               | Save the hook file with LF line endings, not CRLF                                                          |
| Hook doesn't run from a GUI client (Visual Studio, VS Code, Git GUI) | Make sure `dotnet` is on the PATH available to that app, then restart it                                   |
| Hook fails on CI                                                     | Set `HUSKY=0` in the CI environment                                                                        |

---

# Part 3: npm only, `package.json` in the repo root (Husky v9)

Use this when the repo is a Node/JavaScript/TypeScript project and `package.json` sits next to the `.git` folder.

**Run from:** the **repo root** (the folder with `package.json` and `.git`) for every step.

## Step 1: Check prerequisites

**Run from:** anywhere.

```powershell
git --version
node --version
npm --version
```

The repo must already be a Git repo (`git status` works). If it isn't: `git init` in the repo root. Having at least one commit is recommended so you can test the hooks.

## Step 2: Make sure `package.json` and `.gitignore` exist

**Run from:** the repo root.

If the project doesn't have a `package.json` yet:

```powershell
npm init -y
```

Make sure `.gitignore` contains `node_modules/` so dependencies are not committed:

```powershell
Add-Content .gitignore "node_modules/"
```

## Step 3: Install Husky

**Run from:** the repo root.

```powershell
npm install --save-dev husky
```

## Step 4: Initialize Husky

**Run from:** the repo root.

```powershell
npx husky init
```

This does two things:

1. Creates `.husky/pre-commit` with a sample command (`npm test`).
2. Adds a `prepare` script to `package.json`:
   ```json
   "scripts": {
     "prepare": "husky"
   }
   ```
   The `prepare` script runs after every `npm install`, so teammates get the hooks installed automatically.

Husky v9 sets `core.hooksPath` to `.husky/_`. Don't pass extra arguments or flags to `npx husky`: it treats the argument as the hooks folder.

## Step 5: Edit the pre-commit hook

**Run from:** the repo root (the file is `.husky/pre-commit`).

Open `.husky/pre-commit` and put the command(s) you want to run before each commit:

```
npm test
```

> **Heads up:** a project created with `npm init -y` has a placeholder `test` script that always fails (`echo "Error: no test specified" && exit 1`), so with the default hook **every commit will be blocked**. Either define a real `test` script first, or replace the hook with something that passes for now (Step 6 uses `lint-staged`).

The command must exit with a non-zero code to block the commit (most tools do this by default). Hook files do not need to be executable; Husky runs them through `sh`.

## Step 6 (recommended): Run checks only on staged files with lint-staged (Prettier is optional)

**Run from:** the repo root.

Running the whole test suite or linter on every commit is slow. `lint-staged` runs commands only on the files you staged.

1. Install it:
   ```powershell
   npm install --save-dev lint-staged
   ```
2. Add a `lint-staged` section to `package.json`. Only list tools that are **installed in that project**: a command that isn't installed makes the hook fail with "'prettier' is not recognized". The version below uses only `eslint`; the optional Prettier section right after this step shows how to add Prettier:
   ```json
   "lint-staged": {
     "*.{js,jsx,ts,tsx}": "eslint --fix"
   }
   ```
3. Replace the contents of `.husky/pre-commit` with:
   ```
   npx lint-staged
   ```

### Optional: add Prettier (code formatting)

**Run from:** the repo root.

1. Install it:
   ```powershell
   npm install --save-dev prettier
   ```
2. Create `.prettierrc`. Pick options that match your **current** code style, otherwise Prettier rewrites every file it touches. The Vite React-TS template uses no semicolons and single quotes, so this is a likely match, but check your files:
   ```json
   {
     "semi": false,
     "singleQuote": true
   }
   ```
3. Create `.prettierignore` so generated files are skipped:
   ```
   dist
   node_modules
   package-lock.json
   ```
4. Replace the `lint-staged` section in `package.json` with this version. ESLint runs first, then Prettier:
   ```json
   "lint-staged": {
     "*.{ts,tsx}": ["eslint --fix --max-warnings=0", "prettier --write"],
     "*.{json,md,css}": "prettier --write"
   }
   ```
5. (Optional) Format the existing code once, in its **own commit**, so it doesn't mix with the Husky setup. Hooks are skipped for this one commit because it only reformats files. (Run from the folder you are in; `git add .` stages only that folder.)
   ```powershell
   npx prettier --write .
   git add .
   git commit --no-verify -m "style: format code with prettier"
   ```

**If ESLint and Prettier ever fight over a rule** (for example, ESLint reports a semicolon or quote style that Prettier just changed), turn off ESLint's formatting rules with `eslint-config-prettier`.

**Run from:** the repo root.

1. Install it:
   ```powershell
   npm install --save-dev eslint-config-prettier
   ```
2. Open `eslint.config.js`. Add this import at the top, with the other imports:
   ```js
   import prettier from "eslint-config-prettier/flat";
   ```
3. Add `prettier` as the **last** entry of the exported config array, so it can switch off any earlier rule that conflicts with Prettier:
   ```js
   export default defineConfig([
     globalIgnores(["dist"]),
     {
       // ...your existing config object stays as it is
     },
     prettier, // keep this last
   ]);
   ```
   If your file exports a plain array instead of using `defineConfig`, add `prettier` at the end of that array in the same way.
4. Check that ESLint still runs without errors:
   ```powershell
   npx eslint .
   ```

`eslint-config-prettier` only turns rules off. Prettier still does the formatting itself, through the `lint-staged` rule from step 4.

## Step 7 (optional): Enforce a commit message format with commitlint

**Run from:** the repo root.

1. Install it:
   ```powershell
   npm install --save-dev @commitlint/cli @commitlint/config-conventional
   ```
2. Create `commitlint.config.js` in the repo root. If your `package.json` has `"type": "module"`, use:
   ```js
   export default { extends: ["@commitlint/config-conventional"] };
   ```
   Otherwise use the CommonJS form:
   ```js
   module.exports = { extends: ["@commitlint/config-conventional"] };
   ```
   (The ESM form also works without `"type": "module"` on recent Node versions, but Node prints a `MODULE_TYPELESS_PACKAGE_JSON` warning on every commit.)
3. Create the file `.husky/commit-msg` with this single line:
   ```
   npx --no -- commitlint --edit $1
   ```

## Step 8 (optional): Add a pre-push hook

**Run from:** the repo root.

Create `.husky/pre-push` with the checks you want before pushing, for example:

```
npm test
```

## Step 9: Verify it works

**Run from:** the repo root.

```powershell
git add .
git commit -m "chore: add husky"
```

The pre-commit hook should run. If you did Step 7, confirm commitlint blocks a bad message and accepts a valid one:

```powershell
git commit --allow-empty -m "bad message"        # should fail
git commit --allow-empty -m "chore: test hook"   # should pass
```

## Step 10: Commit the Husky files

**Run from:** the repo root.

```powershell
git add .husky package.json package-lock.json commitlint.config.js .prettierrc .prettierignore eslint.config.js
git commit -m "chore: add husky git hooks"
```

Leave out `commitlint.config.js` if you skipped commitlint, `.prettierrc` / `.prettierignore` if you skipped Prettier, and `eslint.config.js` if you didn't change it.

## Step 11: Setup for a new teammate or CI

**Run from:** the repo root after cloning.

```powershell
git clone <repo-url>
cd <repo>
npm install
```

The `prepare` script installs the hooks automatically. In CI you usually don't need hooks: set `HUSKY=0`, or install with `npm ci --ignore-scripts`.

## Skipping hooks (use sparingly)

```powershell
git commit -m "wip" --no-verify      # skip hooks for one commit
$env:HUSKY = "0"                     # skip Husky for this PowerShell session
Remove-Item Env:HUSKY                # turn it back on
```

## Troubleshooting (Part 3)

| Problem                                                                                          | Fix                                                                                                                                                        |
| ------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Hooks don't run                                                                                  | From the repo root run `npm run prepare`, then check `git config core.hooksPath` (should be `.husky/_`)                                                    |
| `husky: command not found`                                                                       | Run `npm install` so `node_modules/.bin` is populated                                                                                                      |
| The hook fails with `'prettier' is not recognized` (or another tool)                             | `lint-staged` runs the commands listed in `package.json`: remove that rule, or install the tool with `npm install --save-dev prettier`                     |
| Every git command fails with `sh: 0: Illegal option --`, or `core.hooksPath` has a strange value | You passed a flag such as `--version` to `npx husky`, and Husky used it as the hooks folder. Delete the stray folder and run `npm run prepare` to reset it |
| The first commit after `npx husky init` is blocked by `npm test`                                 | The placeholder `test` script fails by design: add a real test script or change `.husky/pre-commit`                                                        |
| Hook fails with `\r` errors on Windows                                                           | Save the hook file with LF line endings, not CRLF                                                                                                          |
| Hook doesn't run from a GUI client                                                               | Make sure `node` is on the PATH available to that app, then restart it                                                                                     |
| `.husky` ignored by Git                                                                          | Check that `.gitignore` doesn't exclude `.husky/`                                                                                                          |
| Hook fails on CI                                                                                 | Set `HUSKY=0` in the CI environment                                                                                                                        |

---

# Part 4: npm only, `package.json` in a subfolder (Husky v9)

Use this when the repo has a Node project inside a subfolder (for example `frontend/`) and `package.json` is **not** next to the `.git` folder.

**Run from:** the **subfolder** (the one with `package.json`) for npm commands, and the **repo root** (the folder with `.git`) for Git commands. Each step says which. Examples below use `frontend/`: replace it with your folder name.

Why this needs its own steps: Git runs hooks from the **repo root**, so Husky must be pointed at a hooks folder inside the subfolder, and the commands inside the hooks must move into the subfolder first.

## Step 1: Check prerequisites

**Run from:** anywhere.

```powershell
git --version
node --version
npm --version
```

The repo must already be a Git repo (`git status` works from the repo root). Having at least one commit is recommended so you can test the hooks.

## Step 2: Install Husky

**Run from:** `frontend/`.

```powershell
cd frontend
npm install --save-dev husky
```

Make sure `node_modules/` is ignored by Git (in the repo root `.gitignore` or in `frontend/.gitignore`).

## Step 3: Point the `prepare` script at the subfolder

**Run from:** `frontend/`.

Edit `frontend/package.json` and add (or change) the `prepare` script:

```json
"scripts": {
  "prepare": "cd .. && husky frontend/.husky"
}
```

`cd ..` moves to the repo root, and `husky frontend/.husky` tells Husky to keep its hooks in `frontend/.husky`. Do **not** use `npx husky init` in this setup; you create the hook files yourself in the next step.

## Step 4: Install the hooks

**Run from:** `frontend/`.

```powershell
npm run prepare
```

Then check it from the repo root:

```powershell
cd ..
git config core.hooksPath
```

It should print `frontend/.husky/_`.

## Step 5: Create the pre-commit hook

**Run from:** the repo root (or any editor), creating the file `frontend/.husky/pre-commit`.

Because Git runs hooks from the repo root, move into the subfolder first:

```
cd frontend
npm test
```

> **Heads up:** a project created with `npm init -y` has a placeholder `test` script that always fails, so every commit will be blocked. Define a real `test` script first, or use Step 6 (`lint-staged`) instead.

## Step 6 (recommended): Run checks only on staged files with lint-staged (Prettier is optional)

**Run from:** `frontend/` for the install and `package.json` edit, then edit the hook file.

1. Install it:
   ```powershell
   cd frontend
   npm install --save-dev lint-staged
   ```
2. Add a `lint-staged` section to `frontend/package.json`. Only list tools that are **installed in that project**: a command that isn't installed makes the hook fail with "'prettier' is not recognized". The version below uses only `eslint`; the optional Prettier section right after this step shows how to add Prettier:
   ```json
   "lint-staged": {
     "*.{js,jsx,ts,tsx}": "eslint --fix"
   }
   ```
3. Replace the contents of `frontend/.husky/pre-commit` with:
   ```
   cd frontend
   npx lint-staged
   ```

### Optional: add Prettier (code formatting)

**Run from:** `frontend/`.

1. Install it:
   ```powershell
   npm install --save-dev prettier
   ```
2. Create `frontend/.prettierrc`. Pick options that match your **current** code style, otherwise Prettier rewrites every file it touches. The Vite React-TS template uses no semicolons and single quotes, so this is a likely match, but check your files:
   ```json
   {
     "semi": false,
     "singleQuote": true
   }
   ```
3. Create `frontend/.prettierignore` so generated files are skipped:
   ```
   dist
   node_modules
   package-lock.json
   ```
4. Replace the `lint-staged` section in `frontend/package.json` with this version. ESLint runs first, then Prettier:
   ```json
   "lint-staged": {
     "*.{ts,tsx}": ["eslint --fix --max-warnings=0", "prettier --write"],
     "*.{json,md,css}": "prettier --write"
   }
   ```
5. (Optional) Format the existing code once, in its **own commit**, so it doesn't mix with the Husky setup. Hooks are skipped for this one commit because it only reformats files. (Run from the folder you are in; `git add .` stages only that folder.)
   ```powershell
   npx prettier --write .
   git add .
   git commit --no-verify -m "style: format code with prettier"
   ```

**If ESLint and Prettier ever fight over a rule** (for example, ESLint reports a semicolon or quote style that Prettier just changed), turn off ESLint's formatting rules with `eslint-config-prettier`.

**Run from:** `frontend/`.

1. Install it:
   ```powershell
   npm install --save-dev eslint-config-prettier
   ```
2. Open `frontend/eslint.config.js`. Add this import at the top, with the other imports:
   ```js
   import prettier from "eslint-config-prettier/flat";
   ```
3. Add `prettier` as the **last** entry of the exported config array, so it can switch off any earlier rule that conflicts with Prettier:
   ```js
   export default defineConfig([
     globalIgnores(["dist"]),
     {
       // ...your existing config object stays as it is
     },
     prettier, // keep this last
   ]);
   ```
   If your file exports a plain array instead of using `defineConfig`, add `prettier` at the end of that array in the same way.
4. Check that ESLint still runs without errors:
   ```powershell
   npx eslint .
   ```

`eslint-config-prettier` only turns rules off. Prettier still does the formatting itself, through the `lint-staged` rule from step 4.

## Step 7 (optional): Enforce a commit message format with commitlint

**Run from:** `frontend/`.

1. Install it:
   ```powershell
   npm install --save-dev @commitlint/cli @commitlint/config-conventional
   ```
2. Create `frontend/commitlint.config.js`. If `frontend/package.json` has `"type": "module"`, use:
   ```js
   export default { extends: ["@commitlint/config-conventional"] };
   ```
   Otherwise use the CommonJS form:
   ```js
   module.exports = { extends: ["@commitlint/config-conventional"] };
   ```
3. Create `frontend/.husky/commit-msg` with these three lines:
   ```
   msg="$(pwd)/$1"
   cd frontend
   npx --no -- commitlint --edit "$msg"
   ```
   Git passes the commit message file as a path relative to the repo root (`$1`), so the script builds the full path **before** it moves into the subfolder. This subfolder variant of the commit-msg hook was not run, so test it with the bad/valid messages in Step 8.

## Step 8: Verify it works

**Run from:** the repo root.

```powershell
git add .
git commit -m "chore: add husky"
```

The pre-commit hook should run. If you did Step 7, confirm commitlint blocks a bad message and accepts a valid one:

```powershell
git commit --allow-empty -m "bad message"        # should fail
git commit --allow-empty -m "chore: test hook"   # should pass
```

## Step 9: Commit the Husky files

**Run from:** the repo root.

```powershell
git add frontend/.husky frontend/package.json frontend/package-lock.json frontend/commitlint.config.js frontend/.prettierrc frontend/.prettierignore frontend/eslint.config.js
git commit -m "chore: add husky git hooks"
```

Leave out `commitlint.config.js` if you skipped commitlint, `.prettierrc` / `.prettierignore` if you skipped Prettier, and `eslint.config.js` if you didn't change it.

## Step 10: Setup for a new teammate or CI

**Run from:** `frontend/` after cloning.

```powershell
git clone <repo-url>
cd <repo>/frontend
npm install
```

The `prepare` script installs the hooks automatically. In CI you usually don't need hooks: set `HUSKY=0`, or install with `npm ci --ignore-scripts`.

## Skipping hooks (use sparingly)

```powershell
git commit -m "wip" --no-verify      # skip hooks for one commit
$env:HUSKY = "0"                     # skip Husky for this PowerShell session
Remove-Item Env:HUSKY                # turn it back on
```

## Troubleshooting (Part 4)

| Problem                                                                                          | Fix                                                                                                                                                                |
| ------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Hooks don't run                                                                                  | From `frontend/` run `npm run prepare`, then check `git config core.hooksPath` from the repo root (should be `frontend/.husky/_`)                                  |
| The hook says a command or file isn't found                                                      | Git runs hooks from the repo root: make sure the hook starts with `cd frontend`                                                                                    |
| `husky: command not found`                                                                       | Run `npm install` inside `frontend/`                                                                                                                               |
| The hook fails with `'prettier' is not recognized` (or another tool)                             | `lint-staged` runs the commands listed in `frontend/package.json`: remove that rule, or install the tool with `npm install --save-dev prettier` inside `frontend/` |
| Every git command fails with `sh: 0: Illegal option --`, or `core.hooksPath` has a strange value | You passed a flag such as `--version` to `husky`, and it was used as the hooks folder. Delete the stray folder and run `npm run prepare` again                     |
| Hook fails with `\r` errors on Windows                                                           | Save the hook file with LF line endings, not CRLF                                                                                                                  |
| Hook doesn't run from a GUI client                                                               | Make sure `node` is on the PATH available to that app, then restart it                                                                                             |
| Hook fails on CI                                                                                 | Set `HUSKY=0` in the CI environment                                                                                                                                |
