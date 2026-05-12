# ADO Pipeline Consolidation — Design

**Status:** Approved (design phase)
**Author:** Kavin Singh
**Date:** 2026-05-12

## Problem

The repo has two sets of NuGet packages — **Legacy** (`Libraries/`, `Samples/`, `Tests/`) and **Core** (`core/`) — released independently. Today each set has its own ADO pipeline files:

- `.azdo/ci.yaml` — Legacy PR validation
- `.azdo/cd-core.yaml` — Core PR + CI build, with an in-pipeline `PushToADOFeed` shortcut
- `.azdo/publish.yaml` — Manual release pipeline that **only targets Legacy** (Core has no public-release path)
- `.azdo/templates/sign-and-pack.yaml` — Shared signing template, but hard-coded to `Libraries/**` patterns

This produces three problems:

1. **Core can't be released publicly.** `publish.yaml` packs only `Libraries/**/*.csproj`; the signing template only signs `Libraries/**/*.dll`. When Core becomes the released set, nothing publishes it.
2. **Build logic duplicated.** `ci.yaml`, `cd-core.yaml`, and the build stage inside `publish.yaml` each re-implement `dotnet restore/build/test/pack` independently. Divergence has already started (e.g., `cd-core.yaml` runs from `core/`; `publish.yaml` doesn't).
3. **Stale docs.** `RELEASE.md` references `next/*` branches that were renamed to `core/*`, and claims `ci.yaml` excludes `core/**` paths — it doesn't.

`.github/workflows/build-test-lint.yml` and `.github/workflows/core-ci.yaml` overlap with the ADO pipelines but are **out of scope** for this work.

## Goals

- One CI pipeline file. Path-aware: skips the Legacy build when nothing in Legacy changed, skips the Core build when nothing in Core changed.
- One CD pipeline file. Parameterized: `packageSet ∈ {Legacy, Core}` × `publishType ∈ {Internal, Public}`.
- CI never publishes. All pushes (internal and public, both package sets) flow through the CD pipeline.
- Shared build/test/pack logic lives in one template, reused by both pipelines.
- Signing template parameterized so the same template handles either package set.
- `RELEASE.md` rewritten to reflect the new structure and document Legacy/Core branch pairings.

## Non-goals

- Changes to GitHub Actions workflows (`.github/workflows/*`). Known overlap; deferred.
- Splitting `version.json` semantics. Legacy uses root `version.json`; Core uses `core/version.json`; both already exist and are versioned independently. No change.
- Adding new gating policies (e.g., requiring stable releases only from specific branches). The pipeline doesn't enforce branch/`packageSet` pairings — `RELEASE.md` documents them.
- Touching ESRP service connections, app registrations, or the `teams-net-publish` approval environment.

## Architecture

```
.azdo/
├── ci.yaml                          ← unified CI (parameterized via change detection)
├── publish.yaml                     ← unified CD (packageSet × publishType)
└── templates/
    ├── build-test-pack.yaml         ← new: shared restore/build/test/pack
    └── sign-and-pack.yaml           ← modified: parameterized Authenticode + NuGet signing
```

Templates are YAML-reuse only — they do not appear as ADO pipeline definitions.

## CI pipeline (`.azdo/ci.yaml`)

### Triggers

```yaml
trigger:
  branches:
    include: [main]

pr:
  branches:
    include: [main, releases/*]
```

No `paths:` filters at the trigger level. Every PR fires the pipeline; the `DetectChanges` stage decides which builds run.

**Rationale:** Trigger-level path filters can't gate individual build stages within a single pipeline run, and a docs-only PR that completes a green "DetectChanges + zero builds" run is more accurate (and easier to debug) than one that never appears in pipeline history. Required-status-check considerations are handled by ADO's branch-policy semantics (the in-pipeline run posts a true status; docs-only runs report green honestly because there was nothing to build).

### Stages

#### 1. `DetectChanges`

One job. Computes the diff base:

- For PRs: `origin/$(System.PullRequest.TargetBranch)...HEAD`
- For pushes: `HEAD^...HEAD`

Runs `git diff --name-only <base>...HEAD` and sets two output variables:

| Variable | True when any changed path matches |
|---|---|
| `legacyChanged` | `Libraries/**`, `Samples/**`, `Tests/**`, `Microsoft.Teams.sln`, `version.json`, `.editorconfig`, `.azdo/**`, `Makefile` |
| `coreChanged` | `core/**`, `.editorconfig`, `.azdo/**`, `Makefile` |

Shared infrastructure (`.editorconfig`, `.azdo/**`, `Makefile`) sets **both** flags — a pipeline change must validate against both groups before merge.

Docs-only changes (`**/*.md`, `docs/**`, `core/docs/**`, `Assets/**`) match neither group. Both build stages skip; the pipeline reports green with no build performed.

#### 2. `Build_Legacy`

```yaml
- stage: Build_Legacy
  dependsOn: DetectChanges
  condition: eq(stageDependencies.DetectChanges.outputs['detect.flags.legacyChanged'], 'true')
  jobs:
    - job: BuildTestPack
      steps:
        - template: templates/build-test-pack.yaml
          parameters:
            sourceRoot: '.'
            packPattern: 'Libraries/**/*.csproj'
```

`dotnet build` and `dotnet test` from the repo root pick up `Microsoft.Teams.sln`, which already includes Libraries + Samples + Tests projects. Pack is scoped to `Libraries/**/*.csproj` — only deliverable projects produce nupkgs. Pack output goes to a pipeline artifact; no push.

#### 3. `Build_Core`

```yaml
- stage: Build_Core
  dependsOn: DetectChanges
  condition: eq(stageDependencies.DetectChanges.outputs['detect.flags.coreChanged'], 'true')
  jobs:
    - job: BuildTestPack
      steps:
        - template: templates/build-test-pack.yaml
          parameters:
            sourceRoot: 'core'
            packPattern: 'core/src/**/*.csproj'
```

`dotnet build` / `dotnet test` from `core/` pick up `core/core.slnx` (covers `core/src`, `core/samples`, `core/test`). Pack is scoped to `core/src/**/*.csproj`.

`Build_Legacy` and `Build_Core` are independent stages — both run in parallel when both flags are true.

## CD pipeline (`.azdo/publish.yaml`)

### Triggers

```yaml
trigger: none
pr: none
```

Manual only.

### Parameters

Surfaced in the ADO "Run pipeline" dialog as dropdowns:

```yaml
parameters:
  - name: packageSet
    displayName: 'Package Set'
    type: string
    default: 'Core'
    values: [Legacy, Core]
  - name: publishType
    displayName: 'Publish Type'
    type: string
    default: 'Internal'
    values: [Internal, Public]
```

### Per-`packageSet` variables (compile-time)

Resolved via `${{ if eq(parameters.packageSet, 'Legacy') }}` / `'Core'` blocks:

| Variable | `Legacy` | `Core` |
|---|---|---|
| `sourceRoot` | `.` | `core` |
| `packPattern` | `Libraries/**/*.csproj` | `core/src/**/*.csproj` |
| `signAssemblyPattern` | `Libraries/**/*.dll` | `core/src/**/*.dll` |

ESRP identity (`appRegistrationTenantId`, `authenticodeSignId`, `nugetSignId`), service connections (`TeamsESRP-CP-230012`, `TeamsESRP-CP-401405`, `Microsoft.Teams.*`), and the `teams-net-publish` approval environment are **shared across both sets**. All packages are under the `Microsoft.Teams.*` nuget.org credential.

### Structure

Extends `1ES.Official.PipelineTemplate.yml@1esPipelines` (same as today). Three conditional stages:

#### `Build_Test_Pack_Push_Internal` — when `publishType == 'Internal'`

- Creates a temporary `nuget.config` pointing at `TeamsSDKPreviews` (preserve existing behavior — needed for restore inside 1ES context).
- `NuGetAuthenticate@1`.
- Calls `templates/build-test-pack.yaml` with `sourceRoot` and `packPattern` resolved from `packageSet`. `publishArtifact: false` (the 1ES extension publishes via its own task).
- `1ES.PublishNuget@1` pushes `$(Build.ArtifactStagingDirectory)/*.nupkg` to `$(System.TeamProject)/TeamsSDKPreviews`.
- `1ES.PublishPipelineArtifact@1` publishes the `Packages` artifact.

No approval. Internal feed pushes are immediate.

#### `Build_Test_Sign_Pack` — when `publishType == 'Public'`

- Same restore/build/test path as Internal (reusing `build-test-pack.yaml` with `pack: false` or by calling the build steps and skipping pack — implementation detail for the plan stage).
- Calls `templates/sign-and-pack.yaml` with `assemblyPattern: $(signAssemblyPattern)`, `packagePattern: $(packPattern)`. This authenticode-signs the DLLs, packs to nupkg, then signs the nupkgs.
- `1ES.PublishPipelineArtifact@1` publishes signed `Packages` artifact.

#### `PushToNuGet` — when `publishType == 'Public'`, `dependsOn: Build_Test_Sign_Pack`

- Deployment job gated by `environment: teams-net-publish` (manual approval).
- Downloads the `Packages` artifact.
- `1ES.PublishNuget@1` with `nuGetFeedType: external`, `publishFeedCredentials: 'Microsoft.Teams.*'` → nuget.org.

### Branch enforcement

None in the pipeline. A run with `Public` + `Legacy` from a feature branch is technically valid; `RELEASE.md` documents the expected pairings (see below). This keeps the pipeline simple and avoids hard-coded branch regex maintenance.

### Removed behavior

- The `PushToADOFeed` pipeline-variable shortcut from `cd-core.yaml` is dropped. All internal pushes now go through `publish.yaml` with `publishType: Internal`.

## Shared templates

### `.azdo/templates/build-test-pack.yaml` (new)

**Parameters:**

| Name | Type | Default | Purpose |
|---|---|---|---|
| `sourceRoot` | string | (required) | `.` or `core` — working directory for restore/build/test |
| `packPattern` | string | (required) | Glob passed to `dotnet pack` `packagesToPack` |
| `publishTestResults` | boolean | `true` | Toggle `PublishTestResults@2` step |
| `publishArtifact` | boolean | `true` | Toggle `PublishPipelineArtifact@1` — set false when caller uses `1ES.PublishPipelineArtifact@1` |
| `buildConfiguration` | string | `Release` | Build/test/pack configuration |

**Steps:**

1. `UseDotNet@2` (sdk 8.0.x)
2. `UseDotNet@2` (sdk 10.0.x)
3. `dotnet restore` (workingDirectory: `${{ parameters.sourceRoot }}`)
4. `dotnet build --no-restore --configuration ${{ parameters.buildConfiguration }}`
5. `dotnet test --no-build --configuration ${{ parameters.buildConfiguration }} --logger trx`
6. `PublishTestResults@2` — conditional on `publishTestResults`, `condition: succeededOrFailed()`
7. `dotnet pack --no-build --configuration ${{ parameters.buildConfiguration }}` — packs only projects matching `packPattern`, output to `$(Build.ArtifactStagingDirectory)`, with `/p:SymbolPackageFormat=snupkg`
8. `PublishPipelineArtifact@1` — conditional on `publishArtifact`, publishes `Packages`

### `.azdo/templates/sign-and-pack.yaml` (modified)

**New parameters:**

| Name | Type | Default | Purpose |
|---|---|---|---|
| `assemblyPattern` | string | `Libraries/**/*.dll` | Authenticode signing pattern |
| `packagePattern` | string | `Libraries/**/*.csproj` | Pack pattern |

Defaults match today's hard-coded behavior — back-compat in case any other caller emerges. `publish.yaml` always passes both explicitly.

Body is otherwise unchanged from today's template:

- `EsrpCodeSigning@5` (CP-230012) signs DLLs at `${{ parameters.assemblyPattern }}` under `$(folderPath)`
- `DotNetCoreCLI@2 pack` with `packagesToPack: $(folderPath)/${{ parameters.packagePattern }}`
- `EsrpCodeSigning@5` (CP-401405) signs the resulting `*.nupkg` / `*.snupkg`

## `RELEASE.md` rewrite

### New Pipelines Overview table

| Pipeline | File | Trigger | Scope | Signing | Destination | Approval |
|---|---|---|---|---|---|---|
| **Teams.NET-PR** | `ci.yaml` | PR `main` + `releases/*`; push `main` | Legacy and/or Core via in-pipeline path detection | No | Pipeline artifacts only | None |
| **Teams.NET-ESRP** | `publish.yaml` | Manual (`packageSet` × `publishType`) | Legacy or Core (per run) | `Public` only | `TeamsSDKPreviews` or nuget.org | `Public` only |

### New "Switching the released set" section

Expected branch/parameter pairings:

| Goal | Branch | `packageSet` | `publishType` |
|---|---|---|---|
| Core preview → internal feed | `main` | `Core` | `Internal` |
| Core stable → nuget.org | `releases/core` | `Core` | `Public` |
| Legacy preview → internal feed | `main` | `Legacy` | `Internal` |
| Legacy stable → nuget.org | `releases/vN` | `Legacy` | `Public` |

Stable releases follow the existing flow for both sets: bump `version.json` (root for Legacy, `core/version.json` for Core) on the appropriate `releases/*` branch to remove the preview suffix, queue `publish.yaml` from that branch with `publishType: Public`, approve.

### Fixes to stale content

- `next/*` branch references → `core/*` (those are PR/CI source branches; releases use `releases/core`)
- Claim that "Teams.NET-PR excludes `core/**` paths" — removed; the new CI handles both groups
- `release/*` (singular) references → `releases/*` (plural, matching `version.json`'s `publicReleaseRefSpec`)

## File deltas

| Action | Path | Notes |
|---|---|---|
| ADD | `.azdo/templates/build-test-pack.yaml` | New shared template |
| MODIFY | `.azdo/templates/sign-and-pack.yaml` | Add `assemblyPattern` / `packagePattern` parameters with Libraries-pattern defaults |
| REWRITE | `.azdo/ci.yaml` | Unified parameterized CI with DetectChanges + Build_Legacy + Build_Core |
| MODIFY | `.azdo/publish.yaml` | Add `packageSet` parameter, route per-set variables, use shared build template |
| DELETE | `.azdo/cd-core.yaml` | Rolled into the unified `ci.yaml` |
| REWRITE | `RELEASE.md` | New Pipelines Overview, Legacy/Core split, branch pairings, stale-content fixes |

## Manual ADO-portal cutover (outside the repo)

Required after merge, performed by an ADO admin:

1. **Delete the BotCore-CD pipeline definition.** It points at the now-removed `cd-core.yaml`. Pipelines > BotCore-CD > Delete.
2. **Update branch policies on `main` and `releases/*`.** Remove the BotCore-CD required status check. Keep Teams.NET-PR required — it now validates both groups via the new `ci.yaml`.
3. **No changes** to the `teams-net-publish` environment, Teams.NET-PR pipeline definition (file path unchanged), or Teams.NET-ESRP pipeline definition (file path unchanged; new parameter appears automatically in the run dialog).

## Migration sequence

Single atomic PR against `main`.

1. Open the consolidation PR.
2. **Self-validation:** the PR modifies `.azdo/**`, which is in both groups' DetectChanges rules. Both `Build_Legacy` and `Build_Core` stages run on the PR itself. If the new CI is broken, the PR's own status check shows it before merge.
3. **Pre-merge smoke tests** (ADO admin queues manually against the PR branch):
   - `publish.yaml` with `packageSet=Core, publishType=Internal` → verifies Core internal push end-to-end
   - `publish.yaml` with `packageSet=Legacy, publishType=Internal` → verifies Legacy internal push still works
   - Do **not** test `Public` from a feature branch (would invoke ESRP signing + approval flow against unmerged code)
4. Merge.
5. ADO admin completes the portal steps above same-day.
6. Next preview publish of either set serves as the production smoke test.

## Rollback

`git revert` the PR. All pipeline definitions reference repo paths, so reverting restores `cd-core.yaml` and the old `ci.yaml` / `publish.yaml` automatically. The only manual step on rollback: ADO admin recreates the BotCore-CD pipeline definition pointing at the restored `cd-core.yaml` and re-adds it to branch policy. Document this in the PR description so anyone reverting knows what to do.

## Open questions

None at design-approval time. Implementation-plan stage will resolve exact YAML syntax for the `DetectChanges` output-variable expression (ADO's `stageDependencies.X.outputs['job.step.var']` syntax is version-sensitive) and the precise `git diff` invocation for the merge-base detection.
