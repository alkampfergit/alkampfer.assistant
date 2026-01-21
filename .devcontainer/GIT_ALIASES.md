# Git Aliases

This document describes the git aliases configured in the development container.

## Logging Aliases

### `git logf`
**Command:** `log --graph --oneline --all --decorate`

Displays a graphical log of all branches with decorations (branch names, tags).

**Usage:**
```bash
git logf
```

### `git logr`
**Command:** `log --graph --oneline --decorate`

Displays a graphical log of the current branch with decorations.

**Usage:**
```bash
git logr
```

## Branch Status Alias

### `git bstatus`
**Command:** `!f() { git log HEAD...origin/develop --oneline --left-right | awk '{ print substr($0, 1, 1)}' | sort -n | uniq -c; }; f`

Shows a summary of commits that are ahead or behind compared to `origin/develop`.
- `<` indicates commits on remote that you don't have locally
- `>` indicates commits on local that aren't on remote

**Usage:**
```bash
git bstatus
```

**Example output:**
```
  3 <
  5 >
```
This means you have 5 commits locally that aren't on origin/develop, and 3 commits on origin/develop that you don't have locally.

## Rebase and Fetch Aliases

### `git rod`
**Command:** `!git fetch --prune && git rebase origin/develop`

Fetches the latest changes (pruning deleted branches) and rebases the current branch on top of `origin/develop`.

**Usage:**
```bash
git rod
```

### `git fpull`
**Command:** `!git fetch --prune && git pull --rebase`

Fetches the latest changes (pruning deleted branches) and pulls with rebase instead of merge.

**Usage:**
```bash
git fpull
```

### `git rfi`
**Command:** `!git rebase -i $(git merge-base HEAD origin/develop)`

Starts an interactive rebase from the point where your branch diverged from `origin/develop`.

**Usage:**
```bash
git rfi
```

This is useful for:
- Squashing commits
- Reordering commits
- Editing commit messages
- Dropping commits

### `git rc`
**Command:** `rebase --continue`

Continues a rebase after resolving conflicts.

**Usage:**
```bash
# After resolving conflicts and staging files
git add .
git rc
```

## Commit Aliases

### `git amen`
**Command:** `!git commit -a --amend -C HEAD`

Amends the last commit with all current changes, keeping the same commit message.
- `-a`: Stages all modified files
- `--amend`: Modifies the last commit
- `-C HEAD`: Reuses the commit message from HEAD

**Usage:**
```bash
# Make some changes
git amen
```

### `git pamen`
**Command:** `!git commit -a --amend -C HEAD && git push --force-with-lease`

Amends the last commit with all current changes and force-pushes to remote with safety checks.

**Usage:**
```bash
# Make some changes
git pamen
```

**Warning:** Only use this on feature branches, never on shared branches like `develop` or `main`.

## Push Aliases

### `git pushf`
**Command:** `push --force-with-lease`

Force-pushes to remote but only if the remote hasn't been updated by someone else. This is safer than `--force` because it prevents accidentally overwriting someone else's work.

**Usage:**
```bash
git pushf
```

**Warning:** Only use this on feature branches, never on shared branches.

## Common Workflows

### Starting work on a new feature
```bash
git checkout develop
git fpull
git checkout -b feature/my-new-feature
```

### Keeping your feature branch up to date
```bash
git rod
# If conflicts occur, resolve them and run:
git rc
```

### Cleaning up your commits before merging
```bash
git rfi
# Use the interactive rebase editor to squash, reorder, or edit commits
```

### Making quick fixes to your last commit
```bash
# Make changes
git amen
# If already pushed
git pushf
```

### Checking if you're in sync with develop
```bash
git bstatus
```

## Setup

These aliases are automatically configured when the devcontainer is created via the `postCreateCommand` in [devcontainer.json](devcontainer.json).

To manually set up these aliases, run:
```bash
bash .devcontainer/setup-git-aliases.sh
```

## Notes

- All aliases that start with `!` are shell commands, not git commands
- `--force-with-lease` is safer than `--force` because it checks that the remote hasn't changed
- Interactive rebase (`git rfi`) is powerful but requires understanding of git rebase
- Always be careful with force-pushing (`git pushf`, `git pamen`) on shared branches
