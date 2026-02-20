# Sessions Command

Manage Claude Code session history — list, load, alias, and resume past sessions stored in `~/.claude/sessions/`.

## Usage

```
/sessions [list|load|alias|info] [options]
```

## Actions

| Action | Description |
|---|---|
| `/sessions` | List all sessions with date, size, and alias |
| `/sessions list --limit 10` | Show last 10 sessions |
| `/sessions list --date 2026-02-01` | Filter by date |
| `/sessions load <id|alias>` | Display session content |
| `/sessions alias <id> <name>` | Assign a name to a session |
| `/sessions info <id>` | Show session metadata |

## How Sessions Are Used

1. At session start, the `SessionStart` hook loads the previous session context automatically
2. Use `/sessions load <alias>` to manually resume a specific past session
3. Use `/sessions alias` to name important sessions before they age out

## Implementation

- Manager: `~/.claude/scripts/lib/session-manager`
- Aliases: `~/.claude/scripts/lib/session-aliases`
- Storage: `~/.claude/sessions/*.jsonl`

## Notes

- Sessions are stored as JSONL files in `~/.claude/sessions/`
- Aliases are stored in `~/.claude/session-aliases.json`
- Session IDs can be shortened (first 4-8 characters usually unique enough)
- Use aliases for frequently referenced sessions
