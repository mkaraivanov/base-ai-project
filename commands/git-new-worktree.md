---
description: Create a new git worktree with branch for isolated feature development
---

# Worktree Command

Create a new git worktree for isolated feature development without affecting your main working tree.

## Usage

```bash
/git-new-worktree <feature-name>
```

## What This Command Does

1. **Creates a new git worktree** in the `.claude/worktrees/` directory
2. **Creates a new branch** based on `main` with the provided feature name
3. **Switches your session** to the new worktree (isolated from main working tree)
4. **Enables parallel work** - you can work on multiple features simultaneously

## When to Use

Use `/git-new-worktree` when:
- Starting a new feature (keeps main working tree clean)
- Working on multiple features in parallel
- Wanting isolation from uncommitted changes in main tree
- Running long-running builds without blocking main development
- Experimentation without affecting current state

## Example Usage

```bash
# Create and switch to a new worktree for user authentication
/git-new-worktree user-authentication

# Creates:
# - Branch: user-authentication (from main)
# - Worktree: .claude/worktrees/user-authentication
# - Switches session to the new worktree

# Your working directory is now isolated:
# /Users/martin.karaivanov/Projects/base-ai-project/.claude/worktrees/user-authentication

# Start development
git status          # Shows branch: user-authentication
dotnet build        # Builds in isolation
npm run dev         # Frontend dev server isolated

# When done with the feature:
# - Commit your changes
# - Create a PR
# - Session exit will prompt: keep or remove worktree?
```

## How to Work with Worktrees

### Multiple Worktrees

You can have multiple worktrees active simultaneously:

```bash
# Terminal 1:
/git-new-worktree feature-one
# Work on feature one...

# Terminal 2 (or separate session):
/git-new-worktree feature-two
# Work on feature two...

# Both are independent and don't interfere
```

### Returning to Main Tree

To exit a worktree and return to main:
- Session will prompt on exit: "Keep worktree (y/n)?"
- `y` = Keep worktree for later (can re-open)
- `n` = Delete worktree (work is preserved in branches/commits)

### Cleaning Up

List and manage worktrees:

```bash
# List all worktrees
git worktree list

# Remove a worktree (after deleting branch)
git worktree remove .claude/worktrees/feature-name
git branch -D feature-name
```

## Integration with Development Workflow

Typical workflow with worktrees:

```
Main tree (pristine)
    |
    +---> /git-new-worktree feature-one
    |         |
    |         +---> git add, git commit
    |         +---> /code-review
    |         +---> Create PR
    |
    +---> /git-new-worktree feature-two
              |
              +---> git add, git commit
              +---> /code-review
              +---> Create PR
```

## Arguments

`$ARGUMENTS`:
- `<feature-name>` (required) - Name for branch and worktree (e.g., "user-auth", "audit-logging")
  - Used for both git branch name and worktree directory name
  - Only alphanumeric, hyphens, and underscores allowed
  - Will be created as-is without modification
