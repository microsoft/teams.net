# Decision Log

This file is a shared drop-box for the multi-agent pipeline. Each agent appends decisions and trade-offs here as they arise during the workflow. Entries are append-only and chronological.

## Format

Each entry should follow this structure:

```
### [YYYY-MM-DD] <Decision title>
**Agent:** pm-spec | architect | implementer | code-reviewer | tester
**Context:** Why this decision came up.
**Decision:** What was decided.
**Alternatives considered:** What else was on the table.
**Consequences:** What this means going forward.
```

---

<!-- Append new decisions below this line -->

### [2026-04-15] Spec for new Teams REST API endpoints — awaiting endpoint details
**Agent:** pm-spec
**Context:** User requested implementation of Teams REST API endpoints but indicated the specific REST details will be provided later. A skeleton spec has been drafted to capture the existing architecture, patterns, and open questions so the pipeline can resume quickly once details arrive.
**Decision:** Draft a blocked spec skeleton rather than guessing at endpoints. All open questions are documented so the user knows exactly what information is needed.
**Alternatives considered:** (1) Wait silently for details — rejected because capturing current patterns and open questions now saves time later. (2) Guess at common Teams REST endpoints — rejected because speculative work risks rework.
**Consequences:** The pipeline is paused at the pm-spec stage. Once the user provides REST endpoint details (methods, paths, request/response shapes), the spec can be completed and handed off to the architect.

### [2026-04-15] REST endpoint inventory — reverse-engineered from old Libraries/ code
**Agent:** pm-spec
**Context:** User directed us to reverse-engineer REST endpoints from `Libraries/Microsoft.Teams.Api/Clients/`. Full inventory of both old (24 endpoints) and new (28 endpoints) SDKs completed.
**Decision:** Saved comprehensive spec at `specs/rest-api-endpoints.md` with full endpoint tables for both SDKs and a gap analysis identifying 6 potential missing items (3 activity, 1 auth URL, 2 credential delegation).
**Alternatives considered:** (1) Only document the gaps — rejected because the full inventory provides a single source of truth for the pipeline. (2) Immediately implement gaps — rejected because user wants to review gaps first.
**Consequences:** The new SDK is actually more complete (28 vs 24 endpoints). Only 3-4 gaps need investigation: ReplyAsync, CreateTargetedAsync, DeleteTargetedAsync, and GetSignInUrl. User will decide which to address next.

### [2026-04-15] All REST API gaps resolved — no missing functionality
**Agent:** pm-spec
**Context:** Investigated all 4 potential gaps between old Libraries/ and new core/ SDK endpoints.
**Decision:** All gaps are resolved with no implementation needed. (1) ReplyAsync → unified into SendActivityAsync via ReplyToId. (2) CreateTargetedAsync → unified into SendActivityAsync via Recipient.IsTargeted. (3) DeleteTargetedAsync → exists as DeleteTargetedActivityAsync. (4) GetSignInUrl → intentionally superseded by GetSignInResource which returns richer structured data. BotTokenClient credential delegation handled by new auth infrastructure.
**Alternatives considered:** Could add backward-compatible wrapper methods (e.g., a dedicated ReplyAsync) for discoverability — rejected because the unified API is cleaner and the compat layer already bridges old consumers.
**Consequences:** The new SDK is confirmed feature-complete (28 endpoints vs 24 old). Spec updated at specs/rest-api-endpoints.md. No further pipeline stages needed for this task.
