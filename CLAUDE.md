# CLAUDE.md — Project Intelligence

## Project overview

This is the new **Microsoft Teams .NET SDK** (`core\Core.slnx`). It provides libraries for building Microsoft Teams bots, message extensions, tabs, and AI-powered apps in C#. The codebase targets .NET 8+ and is organized into:

- `core/src` — Core SDK packages (API models, app framework, AI, cards, plugins, extensions).
- `core/tests` — xUnit test projects mirroring the library structure.
- `core/samples` — Example apps demonstrating SDK features.

This is an update from `Libraries` focusing on a cleaner architecture based on layers (Core, Compat, Apps). Avoid Breaking Changes when possible.

## Build & test

```bash
dotnet build core/Core.slnx      # Build everything
dotnet test core/Core.slnx       # Run all tests
```

## Multi-Agent Pipeline (Squad Model)

This project uses a **squad-style pipeline** with five specialized agents located in `.claude/agents/`. Each agent has a distinct role and hands off to the next.

### Pipeline flow

```
User request
    |
    v
[pm-spec] -----> [architect] -----> [implementer] -----> [code-reviewer] -----> [tester]
    |                 |                   |                     |                    |
    +------------ all agents log decisions to decisions.md --------——--------------+
```

### Agent routing rules

| Trigger | Agent | When to use |
|---------|-------|-------------|
| New feature request, user story, or bug report | `pm-spec` | **Always start here** for non-trivial work. Skip only for single-line fixes where the change is obvious. |
| "Design this", "how should we structure", architecture questions | `architect` | When the spec is ready or for standalone design questions. |
| "Implement this", "write the code", or after design approval | `implementer` | When the design is approved and the file plan is clear. |
| "Review this", after implementation, or before merging | `code-reviewer` | After implementation is complete. Always run before creating a PR. |
| "Test this", "write tests", or after review approval | `tester` | After code review passes. Also use standalone for adding test coverage. |

### Routing decision tree

1. **Is this a trivial fix?** (typo, one-line change, obvious bug) → Skip straight to `implementer`, then `code-reviewer`.
2. **Is this a new feature or non-trivial change?** → Start at `pm-spec`, flow through all agents in order.
3. **Is this a design question only?** → Use `architect` directly.
4. **Is this a test-only task?** → Use `tester` directly.
5. **Did review find blockers?** → Loop back to `implementer`, then re-run `code-reviewer`.
6. **Did tests fail?** → Loop back to `implementer` to fix, then re-run `tester`.

### The decisions.md drop-box

All agents append to `decisions.md` when they make non-obvious choices or trade-offs. This creates a shared, chronological record that:
- Prevents agents from unknowingly contradicting earlier decisions.
- Gives the user a single place to audit reasoning.
- Persists context across agent hand-offs.

**Rule:** Before making a significant choice, check `decisions.md` for prior decisions on the same topic.

### Running the pipeline

To invoke an agent, use Claude Code's agent system:

```
# Full pipeline (start from spec)
Use the pm-spec agent to spec out: <describe feature>

# Individual agents
Use the architect agent to design: <describe what>
Use the implementer agent to implement: <describe what>
Use the code-reviewer agent to review the recent changes
Use the tester agent to write tests for: <describe what>
```

### Agent hand-off protocol

When an agent completes its work:
1. Summarize what was done and any decisions made.
2. State what the next agent in the pipeline should focus on.
3. List any open questions or blockers for the next agent.
4. If looping back (e.g., review requested changes), state exactly what needs to change.
