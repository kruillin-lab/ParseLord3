# Total Recall Memory Schema

> This file documents how the memory system works. Loaded every session.

## Four-Tier Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  CLAUDE.local.md (Working Memory)                           │
│  - Auto-loaded every session                                │
│  - ~1500 word limit                                         │
│  - Only behavior-changing facts                             │
│  - Updated frequently, compacted regularly                  │
└─────────────────────────────────────────────────────────────┘
                              ↑ promote
┌─────────────────────────────────────────────────────────────┐
│  memory/registers/*.md (Domain Knowledge)                   │
│  - Loaded on-demand when domain is relevant                 │
│  - Organized by topic: people, projects, decisions, etc.    │
│  - Stable facts with confidence levels                      │
└─────────────────────────────────────────────────────────────┘
                              ↑ promote
┌─────────────────────────────────────────────────────────────┐
│  memory/daily/*.md (Daily Logs)                             │
│  - All writes land here first (append-only)                 │
│  - Timestamped entries                                      │
│  - Source of truth for when things happened                 │
└─────────────────────────────────────────────────────────────┘
                              ↓ archive
┌─────────────────────────────────────────────────────────────┐
│  memory/archive/ (Historical)                               │
│  - Superseded entries                                       │
│  - Completed projects                                       │
│  - Old daily logs (>30 days)                                │
└─────────────────────────────────────────────────────────────┘
```

## Write Gate Rules

Before writing ANYTHING to memory, apply this gate:

1. **Does it change future behavior?** → WRITE
2. **Is it a commitment with consequences?** → WRITE
3. **Is it a decision with rationale?** → WRITE
4. **Is it a stable fact that will matter again?** → WRITE
5. **Did the user explicitly say "remember this"?** → ALWAYS WRITE

**If none are true → DO NOT WRITE.**

### Examples

| Input | Write? | Why |
|-------|--------|-----|
| "I prefer tabs over spaces" | YES | Changes future code formatting |
| "The meeting went well" | NO | Doesn't change behavior |
| "Never use console.log in production" | YES | Constraint on future code |
| "Remember that John prefers email" | YES | User explicit + future behavior |
| "I ran the tests" | NO | Transient fact, no future impact |

## Read Rules

### Auto-Loaded Every Session
- `.claude/rules/total-recall.md` — Protocol rules
- `CLAUDE.local.md` — Working memory
- `memory/SCHEMA.md` — This file

### Check on Session Start
- `memory/registers/open-loops.md` — Active follow-ups
- `memory/daily/[today].md` — Today's log
- `memory/daily/[yesterday].md` — Yesterday's log

### Load On-Demand
- `memory/registers/people.md` — When a person is mentioned
- `memory/registers/projects.md` — When a project is discussed
- `memory/registers/decisions.md` — When past choices are questioned
- `memory/registers/preferences.md` — When task involves user style
- `memory/registers/tech-stack.md` — When technical choices come up

## Routing Table

| Trigger | Destination | Notes |
|---------|-------------|-------|
| User corrects you | Daily log + Register + CLAUDE.local.md | Highest priority |
| User says "remember" | Daily log (+ register if clearly durable) | Explicit request |
| Decision made | Daily log → decisions.md | Include rationale |
| Commitment/deadline | Daily log → open-loops.md | Track follow-up |
| Preference stated | Daily log → preferences.md | Only if stable |
| Person info | Daily log → people.md | Roles, preferences |
| Project update | Daily log → projects.md | State changes |
| Technical choice | Daily log → tech-stack.md | Tools, constraints |

## Contradiction Protocol

When new info conflicts with existing memory:

1. **NEVER silently overwrite**
2. Mark old entry as `[superseded: YYYY-MM-DD]` with reason
3. Write new entry with reference to what it replaces
4. Ask user to confirm if confidence is low

### Example

```markdown
## API Base URL
- ~~https://api.old.com~~ [superseded: 2024-01-15, migrated to new domain]
- https://api.new.com (current)
```

## Correction Handling

Corrections have **HIGHEST PRIORITY**. When the user corrects you:

1. Write to today's daily log immediately
2. Update the relevant register (mark old as superseded)
3. Update CLAUDE.local.md if it changes default behavior
4. Search for the old claim everywhere and update all instances

**A correction that only lasts one session is compliance, not learning.**

## Entry Format

### Daily Log Entry
```markdown
## HH:MM — [tag]

[Content]

confidence: high|medium|low
source: user-stated|inferred|observed
```

### Register Entry
```markdown
## [Topic]

[Content]

- confidence: high|medium|low
- evidence: [source]
- last_verified: YYYY-MM-DD
- supersedes: [old entry if applicable]
```

## Maintenance Cadences

### Immediate
- Corrections → propagate to all tiers now
- Explicit "remember this" → write now

### End of Session (Pre-Compaction)
- Sweep for unsaved decisions, corrections, commitments
- Update CLAUDE.local.md "Session Continuity" section
- Write `[session-flush]` entries to daily log

### Periodic (/recall-maintain)
- Check CLAUDE.local.md word count (~1500 limit)
- Demote stale items to registers
- Archive old daily logs (>30 days)
- Review open-loops for stale items

### Quarterly
- Archive completed projects
- Consolidate fragmented registers
- Review confidence levels

## File Locations

| File | Purpose | Loaded |
|------|---------|--------|
| `.claude/rules/total-recall.md` | Protocol rules | Auto (every session) |
| `CLAUDE.local.md` | Working memory | Auto (every session) |
| `memory/SCHEMA.md` | This documentation | On session start |
| `memory/daily/*.md` | Daily logs | Check today + yesterday |
| `memory/registers/*.md` | Domain knowledge | On demand |
| `memory/archive/` | Historical data | On search only |

## Commands Reference

| Command | Purpose |
|---------|---------|
| `/recall-init` | Scaffold the memory system |
| `/recall-init-ids` | Add durable IDs to entries |
| `/recall-write <note>` | Write with gate evaluation |
| `/recall-log <note>` | Quick append, no gate |
| `/recall-search <query>` | Search all tiers |
| `/recall-promote` | Daily log → registers |
| `/recall-status` | Health check |
| `/recall-maintain` | Pressure-based cleanup |
| `/recall-forget <query>` | Mark as superseded |
| `/recall-context` | Show loaded context |
