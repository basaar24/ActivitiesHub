# Git in Practice: A Step-by-Step Guide

> All commands are written for **PowerShell**. The Git binary is the same across shells, so `git` syntax doesn't change — what does use PowerShell syntax are the helper commands (creating files, viewing content, etc.).

## Prerequisites

1. Check that Git is installed:
   ```powershell
   git --version
   ```
2. Have a GitHub account ready (you'll need it for the Pull Requests section).
3. (Optional) Install GitHub CLI if you want to create Pull Requests from the terminal. If you'd rather not install it, the alternative flow is doing `git push` and creating the PR manually from GitHub's web interface:
   ```powershell
   winget install --id GitHub.cli
   gh auth login
   ```
4. Create a folder and a new repository to practice in:
   ```powershell
   New-Item -ItemType Directory -Path .\git-lab
   Set-Location .\git-lab
   git init
   git config user.name "Your Name"
   git config user.email "your.email@example.com"
   ```
5. Create a first commit so you have a base to work from:
   ```powershell
   "# Practice project" | Out-File README.md
   git add README.md
   git commit -m "chore: initial commit"
   ```
6. Make sure your default branch is named `main` (Git's default branch name can vary depending on your version and configuration):
   ```powershell
   git branch -M main
   ```
7. Create the repository on GitHub and connect it as your remote. With GitHub CLI:
   ```powershell
   gh repo create git-lab --public --source=. --remote=origin
   ```
   Without `gh`, create the repository manually on github.com (leave it empty, no README/license), then connect it:
   ```powershell
   git remote add origin https://github.com/<your-username>/git-lab.git
   ```
8. Check what's staged and ready to go before publishing:
   ```powershell
   git status
   ```
9. Review the commit history:
   ```powershell
   git log --oneline --graph --all
   ```
10. Make sure there are no stale remote-tracking references before pushing (a good habit once teammates start deleting branches too):
    ```powershell
    git remote prune origin
    ```
11. Push your branch and set it to track the remote:
    ```powershell
    git push -u origin main
    ```

---

## 1. Creating branches

A branch is a movable pointer to a commit. It lets you develop features, fixes, or experiments in isolation, without affecting `main`.

1. Check which branch you're on and which branches exist:
   ```powershell
   git branch
   ```
2. Create a new branch without switching to it:
   ```powershell
   git branch feature/login
   ```
3. Create a branch and switch to it in one step:
   ```powershell
   git switch -c feature/login
   # classic equivalent:
   git checkout -b feature/login
   ```
4. Adopt a naming convention for your branches:

   | Prefix | Use |
   |---|---|
   | `feature/` | New functionality |
   | `fix/` | Bug fix |
   | `hotfix/` | Urgent production fix |
   | `chore/` | Maintenance tasks (configs, dependencies) |

---

## 2. Checking out branches

"Checking out" moves `HEAD` (the pointer to "where you're standing") to another branch or commit, updating the working directory's files to match that point in history.

1. Switch to an existing branch:
   ```powershell
   git switch main
   # classic equivalent:
   git checkout main
   ```
2. If you have uncommitted local changes and want to discard them when switching branches, use the force flag (it's destructive — use it with care!):
   ```powershell
   git switch -f main
   ```
3. Go back to the branch you were on before:
   ```powershell
   git switch -
   ```
4. If you get lost, check the history of which branches or commits you've moved to:
   ```powershell
   git reflog
   ```

---

## 3. Commits

A commit is a snapshot of the repository's state at a given moment, with a descriptive message, author, and date. It's the atomic unit you'll later move, combine, or rewrite with `merge`, `cherry-pick`, and `rebase`.

1. Check which files changed:
   ```powershell
   git status
   ```
2. Stage the changes:
   ```powershell
   git add .
   # or a specific file:
   git add specific-file.txt
   ```
3. Create the commit with a descriptive message:
   ```powershell
   git commit -m "feat: add login form"
   ```
4. Review the resulting history:
   ```powershell
   git log --oneline --graph --all
   ```

---

## 4. Pull Requests

A Pull Request (PR) isn't a Git concept itself, but one from platforms like GitHub, GitLab, or Bitbucket. It's a formal request to merge the changes from one branch into another (typically into `main`), accompanied by code review, comments, and automated checks before the merge is approved.

1. Push the branch to the remote:
   ```powershell
   git push origin feature/login
   ```
2. Create the PR from the terminal with GitHub CLI:
   ```powershell
   gh pr create --base main --head feature/login `
     --title "feat: add login form" `
     --body "Implements the login form with basic validation."
   ```
   If you didn't use `gh`, create the PR manually from GitHub's web interface — this is a good point to show both approaches in class.
3. Check the PR's status:
   ```powershell
   gh pr status
   ```
4. Approve and merge from the terminal (if you have permissions):
   ```powershell
   gh pr merge feature/login --squash
   ```

---

## 5. Merge

`merge` integrates the history of one branch into another, creating — in most cases — a *merge commit* with two parents. There are two main scenarios: **fast-forward** (the target branch has no new commits, so Git just moves the pointer) and **three-way merge** (both branches advanced separately and Git combines the changes; if the same files or lines changed in both, a conflict occurs that has to be resolved by hand).

1. Switch to the target branch:
   ```powershell
   git switch main
   ```
2. Merge the source branch:
   ```powershell
   git merge feature/login
   ```
3. If you want to keep the branch's context in the history even when the merge would be fast-forward, force a merge commit:
   ```powershell
   git merge --no-ff feature/login
   ```
4. If conflicts appear, check which files are in conflict:
   ```powershell
   git status
   ```
5. Edit the conflicting files, look for the `<<<<<<<`, `=======`, `>>>>>>>` markers, and leave the correct content.
6. Mark the files as resolved and complete the merge:
   ```powershell
   git add resolved-file.txt
   git commit
   ```
7. If you need to cancel a merge in progress:
   ```powershell
   git merge --abort
   ```

---

## 6. Cherry-pick

`cherry-pick` applies a specific commit (identified by its hash) onto the current branch, without bringing in the rest of the source branch's history. It's useful for taking a specific fix to another branch without doing a full merge.

1. Find the hash of the commit you want to bring over:
   ```powershell
   git log --oneline feature/login
   ```
2. Apply that commit onto the current branch:
   ```powershell
   git cherry-pick a1b2c3d
   ```
3. If you need to bring over several commits, list them in order:
   ```powershell
   git cherry-pick a1b2c3d e4f5g6h
   ```
4. If there's a conflict during the cherry-pick, resolve it the same way as in a merge:
   ```powershell
   git status
   git add resolved-file.txt
   git cherry-pick --continue
   ```
5. If you need to cancel the cherry-pick:
   ```powershell
   git cherry-pick --abort
   ```

---

## 7. Squash

"Squashing" combines several commits into one. It's commonly used before a merge to clean up the history: instead of leaving several commits like "wip", "fix typo", "again", you end up with a single descriptive commit.

1. Option A — interactive rebase (rewrites the current branch's history):
   ```powershell
   git rebase -i HEAD~4
   ```
   An editor opens: change `pick` to `squash` (or `s`) on the commits you want to combine with the previous one, save, and close.
2. Option B — squash merge (combines the whole branch into a single commit when merging):
   ```powershell
   git switch main
   git merge --squash feature/login
   git commit -m "feat: add login form"
   ```
3. Keep in mind that `git rebase -i` rewrites history: avoid using it on shared branches where others have already pulled, unless the team agrees on it (it requires `git push --force-with-lease` afterward).

---

## Extra: Stash (bonus)

`stash` stores uncommitted changes "in a drawer" so you can switch branches with a clean working directory, and bring them back later. Useful when an interruption comes up mid-task.

1. Stash the current changes:
   ```powershell
   git stash
   ```
2. Check the stored stashes:
   ```powershell
   git stash list
   ```
3. Switch branches and do what you need:
   ```powershell
   git switch main
   ```
4. Go back to your branch and restore the latest stash:
   ```powershell
   git switch feature/login
   git stash pop
   ```

---

## Suggested guided activity

A ~30-40 minute exercise to practice the whole flow, in teams of 2-3 people:

1. Each team creates a repo and makes an initial commit with a README.
2. They create a `feature/homepage` branch and add an `index.html` file with two separate commits.
3. They push the branch and open a Pull Request into `main` (with `gh` or from GitHub's web interface).
4. Before merging, they `squash` the two commits into one.
5. They merge the PR (fast-forward if applicable, or `--no-ff` if they want to keep the context).
6. A commit from another branch (`fix/styles`, prepared beforehand) is brought in with `cherry-pick` onto `main`.
7. Wrap-up: they review the final history with `git log --oneline --graph --all` and discuss what happened at each step.

**Goal:** the final history should tell a clear story, and each team should understand which command to use depending on the situation (new branch vs. bringing in a specific commit vs. cleaning up history before sharing it).
