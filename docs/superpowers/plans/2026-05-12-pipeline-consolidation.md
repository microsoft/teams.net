# ADO Pipeline Consolidation — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Collapse two ADO pipeline sets (Legacy under `Libraries/`+`Samples/`+`Tests/` and Core under `core/`) into one parameterized CI pipeline with in-pipeline path detection and one parameterized CD pipeline (`packageSet` × `publishType`), sharing a build-test-pack template and a parameterized sign-and-pack template.

**Architecture:** `.azdo/ci.yaml` always runs on PR/push; a `DetectChanges` stage flags which package set(s) changed; two conditional build stages (`Build_Legacy`, `Build_Core`) skip when their group is unchanged. `.azdo/publish.yaml` extends 1ES Official Pipeline Template; runtime parameters `packageSet` (Legacy|Core) and `publishType` (Internal|Public) drive per-set glob patterns and per-type stage selection. Both pipelines call `.azdo/templates/build-test-pack.yaml` for build/test/pack; the Public flow additionally calls `.azdo/templates/sign-and-pack.yaml` for ESRP signing.

**Tech Stack:** Azure DevOps YAML pipelines, 1ES Official Pipeline Template, ESRP CodeSigning, dotnet CLI (.NET 8 + .NET 10), Nerdbank.GitVersioning, bash for change detection.

**Reference spec:** `docs/superpowers/specs/2026-05-12-pipeline-consolidation-design.md`

---

## File Structure

```
.azdo/
├── ci.yaml                          ← REWRITE (unified parameterized CI)
├── publish.yaml                     ← MODIFY (add packageSet parameter)
├── cd-core.yaml                     ← DELETE (rolled into ci.yaml)
└── templates/
    ├── build-test-pack.yaml         ← CREATE (shared restore/build/test/pack)
    └── sign-and-pack.yaml           ← MODIFY (parameterize patterns)

RELEASE.md                           ← REWRITE (new pipelines overview, branch pairings, stale fixes)
```

**Responsibility split:**
- `build-test-pack.yaml` — All restore/build/test/pack logic. Parameterized by `sourceRoot` and `packPattern`. Used by both `ci.yaml` and `publish.yaml`. Does NOT push anywhere.
- `sign-and-pack.yaml` — ESRP-specific. Authenticode-signs DLLs, packs nupkgs, signs nupkgs. Parameterized by `assemblyPattern` and `packagePattern`. Used only by `publish.yaml` Public stage.
- `ci.yaml` — Triggers + DetectChanges + two conditional build stages.
- `publish.yaml` — Manual trigger + 1ES extends + per-set/per-type stage selection.

---

## YAML Validation Helper

Several tasks call out a "validate YAML syntax" sub-step. Use this command (pick the one your environment supports):

**Python (preferred — universal):**
```bash
python -c "import sys, yaml; yaml.safe_load(open(sys.argv[1])); print('OK')" PATH/TO/FILE.yaml
```

**Azure CLI (validates ADO semantics too, requires auth):**
```bash
az pipelines runs validate-yaml --path PATH/TO/FILE.yaml --organization https://dev.azure.com/DomoreexpGithub --project Github_Pipelines
```

**Fallback:** Manual review against this plan's snippets + the spec.

If none are available locally, validation effectively happens at pipeline queue time (Task 4 and Task 7 cover that).

---

## Task 1: Create `build-test-pack.yaml` template

**Files:**
- Create: `.azdo/templates/build-test-pack.yaml`

- [ ] **Step 1: Create the template file**

Write `.azdo/templates/build-test-pack.yaml`:

```yaml
# Shared template: dotnet restore, build, test, and (optionally) pack and publish artifact.
# Used by ci.yaml (always packs) and publish.yaml (packs for Internal; skips pack for Public,
# where sign-and-pack.yaml packs after signing DLLs).

parameters:
  - name: sourceRoot
    type: string
    # Required. '.' for Legacy (root Microsoft.Teams.sln), 'core' for Core (core.slnx).
  - name: packPattern
    type: string
    default: ''
    # Required when pack: true. Glob passed to dotnet pack packagesToPack.
  - name: pack
    type: boolean
    default: true
  - name: publishTestResults
    type: boolean
    default: true
  - name: publishArtifact
    type: boolean
    default: true
    # Set false when caller publishes via 1ES.PublishPipelineArtifact@1.
  - name: buildConfiguration
    type: string
    default: 'Release'

steps:
  - task: UseDotNet@2
    displayName: 'Use .NET 8 SDK'
    inputs:
      packageType: 'sdk'
      version: '8.0.x'

  - task: UseDotNet@2
    displayName: 'Use .NET 10 SDK'
    inputs:
      packageType: 'sdk'
      version: '10.0.x'

  - script: dotnet restore
    displayName: 'Restore'
    workingDirectory: '$(Build.SourcesDirectory)/${{ parameters.sourceRoot }}'

  - script: dotnet build --no-restore --configuration ${{ parameters.buildConfiguration }}
    displayName: 'Build'
    workingDirectory: '$(Build.SourcesDirectory)/${{ parameters.sourceRoot }}'

  - script: dotnet test --no-build --configuration ${{ parameters.buildConfiguration }} --logger trx
    displayName: 'Test'
    workingDirectory: '$(Build.SourcesDirectory)/${{ parameters.sourceRoot }}'

  - ${{ if eq(parameters.publishTestResults, true) }}:
    - task: PublishTestResults@2
      displayName: 'Publish Test Results'
      condition: succeededOrFailed()
      inputs:
        testResultsFormat: 'VSTest'
        testResultsFiles: '**/*.trx'
        mergeTestResults: true
        searchFolder: '$(Build.SourcesDirectory)/${{ parameters.sourceRoot }}'

  - ${{ if eq(parameters.pack, true) }}:
    - task: DotNetCoreCLI@2
      displayName: 'Pack'
      inputs:
        command: 'pack'
        packagesToPack: '${{ parameters.packPattern }}'
        nobuild: true
        configuration: '${{ parameters.buildConfiguration }}'
        outputDir: '$(Build.ArtifactStagingDirectory)'
        buildProperties: 'SymbolPackageFormat=snupkg'

    - ${{ if eq(parameters.publishArtifact, true) }}:
      - task: PublishPipelineArtifact@1
        displayName: 'Publish NuGet Packages as Pipeline Artifact'
        inputs:
          targetPath: '$(Build.ArtifactStagingDirectory)'
          artifact: 'Packages'
          publishLocation: 'pipeline'
```

- [ ] **Step 2: Validate YAML syntax**

Run: `python -c "import yaml; yaml.safe_load(open('.azdo/templates/build-test-pack.yaml')); print('OK')"`
Expected: `OK`

- [ ] **Step 3: Commit**

```bash
git add .azdo/templates/build-test-pack.yaml
git commit -m "ci: add shared build-test-pack template

Shared restore/build/test/pack steps used by both ci.yaml and
publish.yaml. Parameterized by sourceRoot (. for Legacy, core for Core)
and packPattern. pack: false skips the pack/artifact steps for the
Public CD stage, where sign-and-pack.yaml packs after signing."
```

---

## Task 2: Parameterize `sign-and-pack.yaml`

**Files:**
- Modify: `.azdo/templates/sign-and-pack.yaml`

The existing template hard-codes `Libraries/**/*.dll` (line 17) and `$(folderPath)/Libraries/**/*.csproj` (line 51). Add parameters with Libraries-pattern defaults so existing callers stay working through the migration window.

- [ ] **Step 1: Add `parameters:` block at the top of the file**

Insert before the existing `steps:` line:

```yaml
parameters:
  - name: assemblyPattern
    type: string
    default: 'Libraries/**/*.dll'
  - name: packagePattern
    type: string
    default: 'Libraries/**/*.csproj'
```

The full top of the file should now read:

```yaml
# Shared template: Authenticode sign DLLs, pack NuGet packages, sign NuGet packages.
# Requires the calling pipeline to define variables:
#   $(appRegistrationTenantId), $(authenticodeSignId), $(nugetSignId), $(folderPath), $(buildConfiguration)
# Parameters:
#   assemblyPattern — DLL glob for Authenticode signing (relative to folderPath)
#   packagePattern  — csproj glob for pack (relative to folderPath)

parameters:
  - name: assemblyPattern
    type: string
    default: 'Libraries/**/*.dll'
  - name: packagePattern
    type: string
    default: 'Libraries/**/*.csproj'

steps:
  - task: EsrpCodeSigning@5
    ...
```

- [ ] **Step 2: Replace the hard-coded DLL pattern**

Change line 17 (Authenticode `Pattern:`) from:

```yaml
      Pattern: 'Libraries/**/*.dll'
```

To:

```yaml
      Pattern: '${{ parameters.assemblyPattern }}'
```

- [ ] **Step 3: Replace the hard-coded csproj pattern**

Change line 51 (`packagesToPack:`) from:

```yaml
      packagesToPack: '$(folderPath)/Libraries/**/*.csproj'
```

To:

```yaml
      packagesToPack: '$(folderPath)/${{ parameters.packagePattern }}'
```

- [ ] **Step 4: Validate YAML syntax**

Run: `python -c "import yaml; yaml.safe_load(open('.azdo/templates/sign-and-pack.yaml')); print('OK')"`
Expected: `OK`

- [ ] **Step 5: Confirm back-compat by diff inspection**

Run: `git diff .azdo/templates/sign-and-pack.yaml`
Expected: only the three changes above. No NuGet-signing block touched. Defaults preserve current behavior — `publish.yaml` callers that haven't been updated still get `Libraries/**` patterns.

- [ ] **Step 6: Commit**

```bash
git add .azdo/templates/sign-and-pack.yaml
git commit -m "ci: parameterize sign-and-pack template DLL and csproj patterns

Add assemblyPattern and packagePattern parameters with Libraries-pattern
defaults so existing callers keep working. publish.yaml will pass
explicit patterns per packageSet in a later commit."
```

---

## Task 3: Rewrite `ci.yaml` as unified parameterized CI

**Files:**
- Modify: `.azdo/ci.yaml` (full rewrite)

This is the largest single-file change. We replace the Legacy-only CI with one pipeline that handles both Legacy and Core via in-pipeline change detection.

- [ ] **Step 1: Replace the entire contents of `.azdo/ci.yaml`**

Write `.azdo/ci.yaml`:

```yaml
# =============================================================================
# Teams.NET-PR — unified CI pipeline.
#
# Validates Legacy (Libraries/, Samples/, Tests/) and Core (core/) package sets.
# Always runs; the DetectChanges stage decides which build stages execute.
# Neither stage publishes anything — pack output goes to a pipeline artifact only.
# Pushes are handled by publish.yaml.
#
# Spec: docs/superpowers/specs/2026-05-12-pipeline-consolidation-design.md
# =============================================================================

trigger:
  branches:
    include:
      - main

pr:
  branches:
    include:
      - main
      - releases/*

pool:
  vmImage: 'ubuntu-22.04'

variables:
  buildConfiguration: 'Release'

stages:
- stage: DetectChanges
  displayName: 'Detect Changed Paths'
  jobs:
  - job: detect
    displayName: 'Compute change flags'
    steps:
    - checkout: self
      fetchDepth: 0

    - bash: |
        set -euo pipefail

        if [ -n "${SYSTEM_PULLREQUEST_TARGETBRANCH:-}" ]; then
          target="${SYSTEM_PULLREQUEST_TARGETBRANCH#refs/heads/}"
          git fetch origin "$target" --depth=200 || true
          base="origin/$target"
        else
          base="HEAD^"
        fi

        echo "Diff base: $base"
        changed=$(git diff --name-only "$base"...HEAD)
        echo "Changed paths:"
        echo "$changed"

        legacy=false
        core=false
        while IFS= read -r f; do
          [ -z "$f" ] && continue
          case "$f" in
            Libraries/*|Samples/*|Tests/*|Microsoft.Teams.sln|version.json)
              legacy=true
              ;;
            core/*)
              core=true
              ;;
            .editorconfig|.azdo/*|Makefile)
              legacy=true
              core=true
              ;;
          esac
        done <<< "$changed"

        echo "legacyChanged=$legacy"
        echo "coreChanged=$core"
        echo "##vso[task.setvariable variable=legacyChanged;isOutput=true]$legacy"
        echo "##vso[task.setvariable variable=coreChanged;isOutput=true]$core"
      name: flags
      displayName: 'git diff and flag groups'

- stage: Build_Legacy
  displayName: 'Build Legacy'
  dependsOn: DetectChanges
  condition: eq(dependencies.DetectChanges.outputs['detect.flags.legacyChanged'], 'true')
  jobs:
  - job: BuildTestPack
    displayName: 'Build, Test, Pack — Legacy'
    steps:
    - template: templates/build-test-pack.yaml
      parameters:
        sourceRoot: '.'
        packPattern: 'Libraries/**/*.csproj'

- stage: Build_Core
  displayName: 'Build Core'
  dependsOn: DetectChanges
  condition: eq(dependencies.DetectChanges.outputs['detect.flags.coreChanged'], 'true')
  jobs:
  - job: BuildTestPack
    displayName: 'Build, Test, Pack — Core'
    steps:
    - template: templates/build-test-pack.yaml
      parameters:
        sourceRoot: 'core'
        packPattern: 'core/src/**/*.csproj'
```

- [ ] **Step 2: Validate YAML syntax**

Run: `python -c "import yaml; yaml.safe_load(open('.azdo/ci.yaml')); print('OK')"`
Expected: `OK`

- [ ] **Step 3: Sanity-check the DetectChanges bash on three scenarios**

Run each on the engineer's machine to confirm the case patterns work:

```bash
# Scenario A: Legacy-only change
for f in "Libraries/Foo.cs" "Tests/Bar.cs"; do
  case "$f" in
    Libraries/*|Samples/*|Tests/*|Microsoft.Teams.sln|version.json) echo "$f -> legacy" ;;
    core/*) echo "$f -> core" ;;
    .editorconfig|.azdo/*|Makefile) echo "$f -> both" ;;
    *) echo "$f -> neither" ;;
  esac
done
```
Expected: two `legacy` lines.

```bash
# Scenario B: Core-only change
for f in "core/src/Foo.cs"; do
  case "$f" in
    Libraries/*|Samples/*|Tests/*|Microsoft.Teams.sln|version.json) echo "$f -> legacy" ;;
    core/*) echo "$f -> core" ;;
    .editorconfig|.azdo/*|Makefile) echo "$f -> both" ;;
    *) echo "$f -> neither" ;;
  esac
done
```
Expected: one `core` line.

```bash
# Scenario C: Shared file (.azdo) — should set both
for f in ".azdo/ci.yaml"; do
  case "$f" in
    Libraries/*|Samples/*|Tests/*|Microsoft.Teams.sln|version.json) echo "$f -> legacy" ;;
    core/*) echo "$f -> core" ;;
    .editorconfig|.azdo/*|Makefile) echo "$f -> both" ;;
    *) echo "$f -> neither" ;;
  esac
done
```
Expected: one `both` line.

- [ ] **Step 4: Commit**

```bash
git add .azdo/ci.yaml
git commit -m "ci: rewrite ci.yaml as unified Legacy + Core CI

Always runs on PR (main, releases/*) and push (main). DetectChanges
stage runs git diff against the target/previous commit and flags
whether Legacy and/or Core paths changed. Build_Legacy and Build_Core
are independent stages with conditions on the respective flags;
shared files (.editorconfig, .azdo/*, Makefile) flag both groups."
```

---

## Task 4: Smoke-test the new CI on a feature branch

This is a live-pipeline validation step. Skip if you're working in a worktree without ADO access — Task 7 covers integrated validation via the final PR.

**Files:**
- (none — pure validation)

- [ ] **Step 1: Push the current branch to remote**

```bash
git push -u origin HEAD
```

- [ ] **Step 2: Open a draft PR against `main`**

Use the GitHub/ADO UI. PR must target `main` to fire the `pr:` trigger.

- [ ] **Step 3: Watch the Teams.NET-PR pipeline run**

Open Pipelines > Teams.NET-PR > latest run. Verify:
- `DetectChanges` stage completes (~30s). Open the `flags` step log — `legacyChanged=true` and `coreChanged=true` (because `.azdo/**` was touched, which flags both).
- `Build_Legacy` and `Build_Core` stages both run, both green, both publish a `Packages` artifact.

Expected total time: ~5-7 minutes.

If `Build_Legacy` fails restoring/building/testing, the new template has a bug — check the workingDirectory parameter and the dotnet command flags against the existing `.azdo/ci.yaml` history before the rewrite.

If `Build_Core` fails, same check against the old `.azdo/cd-core.yaml`.

- [ ] **Step 4: Verify the artifact contents**

Download the `Packages` artifact from each build stage. Confirm:
- `Build_Legacy` Packages contains `Microsoft.Teams.Apps.*.nupkg`, `Microsoft.Teams.Cards.*.nupkg`, etc. — 14-16 Libraries packages.
- `Build_Core` Packages contains exactly `Microsoft.Teams.Core.*.nupkg`, `Microsoft.Teams.Apps.*.nupkg`, `Microsoft.Teams.Apps.BotBuilder.*.nupkg` — three packages (matches `core/src/*`).

(No commit; this is verification only.)

---

## Task 5: Modify `publish.yaml` to add `packageSet` parameter

**Files:**
- Modify: `.azdo/publish.yaml`

The current publish.yaml is 227 lines and Legacy-only. After this change it supports `packageSet` × `publishType` (four combinations).

- [ ] **Step 1: Add `packageSet` parameter**

In the `parameters:` block (currently only contains `publishType`), add a new `packageSet` parameter immediately before `publishType`:

```yaml
parameters:
- name: packageSet
  displayName: 'Package Set'
  type: string
  default: 'Core'
  values:
  - Legacy
  - Core
- name: publishType
  displayName: 'Publish Type'
  type: string
  default: 'Internal'
  values:
  - Internal
  - Public
```

The order matters — ADO renders parameters top-to-bottom in the run dialog. Package Set first, then Publish Type.

- [ ] **Step 2: Replace the Internal stage's inline build/pack steps with the template**

Inside `stage: Build_Test_Pack_Push_Internal` > `job: BuildTestPackPush` > `steps:`, locate the block from `- task: UseDotNet@2  # .NET 8` through `- task: DotNetCoreCLI@2  # Pack` (currently lines 67-120 in publish.yaml — the two UseDotNet tasks, dotnet restore/build/test, PublishTestResults, and the DotNetCoreCLI pack task).

Replace that entire block with:

```yaml
            - ${{ if eq(parameters.packageSet, 'Legacy') }}:
              - template: .azdo/templates/build-test-pack.yaml@self
                parameters:
                  sourceRoot: '.'
                  packPattern: 'Libraries/**/*.csproj'
                  publishArtifact: false
            - ${{ if eq(parameters.packageSet, 'Core') }}:
              - template: .azdo/templates/build-test-pack.yaml@self
                parameters:
                  sourceRoot: 'core'
                  packPattern: 'core/src/**/*.csproj'
                  publishArtifact: false
```

Keep the surrounding steps unchanged: `- checkout: self`, the `pwsh` block creating `nuget.config`, `NuGetAuthenticate@1` (these come BEFORE the replaced block), and the `1ES.PublishNuget@1` and `1ES.PublishPipelineArtifact@1` tasks (these come AFTER).

- [ ] **Step 3: Replace the Public stage's inline build steps with the template**

Inside `stage: Build_Test_Sign_Pack` > `job: BuildTestSignPack` > `steps:`, locate the same UseDotNet/restore/build/test/PublishTestResults block (currently lines 149-192). The Public stage does NOT have a `dotnet pack` step here (sign-and-pack handles that).

Replace that block with:

```yaml
            - ${{ if eq(parameters.packageSet, 'Legacy') }}:
              - template: .azdo/templates/build-test-pack.yaml@self
                parameters:
                  sourceRoot: '.'
                  pack: false
                  publishArtifact: false
            - ${{ if eq(parameters.packageSet, 'Core') }}:
              - template: .azdo/templates/build-test-pack.yaml@self
                parameters:
                  sourceRoot: 'core'
                  pack: false
                  publishArtifact: false
```

Note `pack: false` here — sign-and-pack does the packing after signing DLLs.

- [ ] **Step 4: Update the sign-and-pack template call to pass per-set patterns**

Locate the existing line in the Public stage (currently line 194):

```yaml
            - template: .azdo/templates/sign-and-pack.yaml@self
```

Replace with a conditional pair:

```yaml
            - ${{ if eq(parameters.packageSet, 'Legacy') }}:
              - template: .azdo/templates/sign-and-pack.yaml@self
                parameters:
                  assemblyPattern: 'Libraries/**/*.dll'
                  packagePattern: 'Libraries/**/*.csproj'
            - ${{ if eq(parameters.packageSet, 'Core') }}:
              - template: .azdo/templates/sign-and-pack.yaml@self
                parameters:
                  assemblyPattern: 'core/src/**/*.dll'
                  packagePattern: 'core/src/**/*.csproj'
```

- [ ] **Step 5: Validate YAML syntax**

Run: `python -c "import yaml; yaml.safe_load(open('.azdo/publish.yaml')); print('OK')"`
Expected: `OK`

- [ ] **Step 6: Diff inspection — confirm only the four edits above**

Run: `git diff .azdo/publish.yaml`

Expected changes (and nothing else):
1. New `packageSet` parameter at the top
2. Internal stage's UseDotNet→Pack block replaced with two conditional template refs
3. Public stage's UseDotNet→PublishTestResults block replaced with two conditional template refs (with `pack: false`)
4. Sign-and-pack template call replaced with two conditional refs that pass `assemblyPattern` / `packagePattern`

NOT changed (verify these are still present and untouched):
- `resources.repositories.1esPipelines`
- `trigger: none`, `pr: none`
- The `variables:` block with `appRegistrationTenantId` etc. under `if eq(parameters.publishType, 'Public')`
- `extends.template` 1ES line
- The `pwsh` nuget.config blocks in both stages
- `NuGetAuthenticate@1`
- `1ES.PublishNuget@1` (Internal feed push) and `1ES.PublishPipelineArtifact@1` in both stages
- The entire `PushToNuGet` deployment stage and its approval environment

- [ ] **Step 7: Commit**

```bash
git add .azdo/publish.yaml
git commit -m "ci: parameterize publish.yaml on packageSet (Legacy|Core)

Add packageSet runtime parameter; route per-set sourceRoot / pack /
sign patterns through conditional template includes. Both Internal and
Public stages now delegate restore/build/test to build-test-pack.yaml.
Public stage passes pack: false so sign-and-pack.yaml handles packing
after Authenticode signing.

ESRP service connections, signing identities, and the teams-net-publish
approval environment are unchanged — all packages publish under the same
Microsoft.Teams.* nuget.org credential."
```

---

## Task 6: Delete `cd-core.yaml`

**Files:**
- Delete: `.azdo/cd-core.yaml`

Now that the unified `ci.yaml` handles both Legacy and Core, the standalone Core CI is redundant. The `PushToADOFeed` shortcut is dropped per spec — internal pushes go through `publish.yaml` with `publishType: Internal`.

- [ ] **Step 1: Confirm no references to `cd-core.yaml` remain in the repo**

Run: `git grep -n "cd-core.yaml"`
Expected: one match in `core/core.slnx` (the slnx references it for IDE convenience) and zero references inside `.azdo/`.

If `.azdo/` matches appear, investigate before deleting — something still depends on the file.

- [ ] **Step 2: Update `core/core.slnx` to reference the new CI file**

Open `core/core.slnx`. Replace:

```xml
    <File Path="../.azdo/cd-core.yaml" />
```

With:

```xml
    <File Path="../.azdo/ci.yaml" />
```

This keeps the file visible in the IDE solution view.

- [ ] **Step 3: Delete `.azdo/cd-core.yaml`**

```bash
git rm .azdo/cd-core.yaml
```

- [ ] **Step 4: Verify the deletion**

Run: `ls .azdo/` (or `Get-ChildItem .azdo/` on PowerShell)
Expected: `ci.yaml`, `publish.yaml`, `templates/` — no `cd-core.yaml`.

Run: `git grep -n "cd-core.yaml"`
Expected: zero matches.

- [ ] **Step 5: Commit**

```bash
git add .azdo/cd-core.yaml core/core.slnx
git commit -m "ci: delete cd-core.yaml; Core CI is now handled by unified ci.yaml

The unified ci.yaml builds Core when DetectChanges flags it. The
PushToADOFeed shortcut is intentionally dropped — internal pushes are
exclusively handled by publish.yaml with publishType: Internal.

Update core/core.slnx to reference the new ci.yaml for IDE visibility."
```

---

## Task 7: Rewrite `RELEASE.md`

**Files:**
- Modify: `RELEASE.md`

The current `RELEASE.md` is Legacy-focused and contains three pieces of stale content: `next/*` branch names (renamed to `core/*` for source, `releases/core` for stable releases), a false claim that `ci.yaml` excludes `core/**` paths, and inconsistent `release/*` (singular) vs `releases/*` (plural) references.

- [ ] **Step 1: Replace the "Pipelines Overview" table**

Locate the existing table (currently lines 5-13). Replace with:

```markdown
## Pipelines Overview

| Pipeline | File | Trigger | Scope | Signing | Destination | Approval |
|----------|------|---------|-------|---------|-------------|----------|
| **Teams.NET-PR** | `ci.yaml` | PR `main` + `releases/*`; push `main` | Legacy and/or Core via in-pipeline change detection | No | Pipeline artifacts only | None |
| **Teams.NET-ESRP** | `publish.yaml` | Manual (`packageSet` × `publishType`) | Legacy or Core (per run) | `Public` only | `TeamsSDKPreviews` internal feed or nuget.org | `Public` only |

Note: Public packages are available on nuget.org. Internal feed packages are for Microsoft internal use.

The `Teams.NET-PR` pipeline always runs on covered triggers. A `DetectChanges` stage inspects the changed paths and skips the Legacy and/or Core build stages when their respective package set is unchanged. Docs-only PRs produce a green run with both stages skipped.
```

(Remove the third row referencing BotCore-CD — that pipeline is deleted in Task 6.)

- [ ] **Step 2: Replace "Versioning" section to cover both package sets**

Locate the `## Versioning` section (currently lines 15-69). Replace with:

```markdown
## Versioning

Versions are managed by **Nerdbank.GitVersioning** (nbgv). Each package set has its own version file:

- **Legacy** (`Libraries/`): root `version.json` (e.g., `2.0.7-preview.{height}`)
- **Core** (`core/`): `core/version.json` (e.g., `1.0`)

Plus one per-project override at `core/src/Microsoft.Teams.Apps/version.json` (currently `2.1.0-alpha.{height}`).

### Preview vs Stable

When `version.json` has a `-preview` (or `-alpha`) suffix, every build produces a preview package (e.g., `Microsoft.Teams.Apps.2.0.7-preview.42.nupkg`). When the suffix is removed and the file is on a branch listed in `publicReleaseRefSpec`, builds produce stable packages.

**Manually-queued runs from a branch not in `publicReleaseRefSpec`** produce versions with the commit hash appended (e.g., `2.0.7-preview.42-g1a2b3c4`). Useful for testing packages from a feature branch before merge.

### Producing a Stable Release

Legacy stable release (e.g., `2.0.7`):

1. Check out the Legacy stable branch:
   ```bash
   git checkout releases/v2
   git merge main
   ```
2. Edit root `version.json` to remove the preview suffix:
   ```json
   { "version": "2.0.7" }
   ```
3. Commit and push to `releases/v2`.
4. Queue `Teams.NET-ESRP` from `releases/v2` with `packageSet=Legacy, publishType=Public`.
5. Approve the push to nuget.org.
6. After the release, bump for the next cycle on `main`: `"version": "2.0.8-preview.{height}"`.

Core stable release: same flow, but on `releases/core`, editing `core/version.json` (and `core/src/Microsoft.Teams.Apps/version.json` if its independent version applies).

### Note on `publicReleaseRefSpec`

`publicReleaseRefSpec` controls metadata only (whether nbgv treats a build as "public"). It does **not** affect the version string — that's determined entirely by the `"version"` field.
```

- [ ] **Step 3: Add "Switching the released set" section**

Insert immediately after the Versioning section (before Approvers):

```markdown
## Switching the released set

| Goal | Branch | `packageSet` | `publishType` |
|---|---|---|---|
| Core preview → internal feed | `main` | `Core` | `Internal` |
| Core stable → nuget.org | `releases/core` | `Core` | `Public` |
| Legacy preview → internal feed | `main` | `Legacy` | `Internal` |
| Legacy stable → nuget.org | `releases/vN` | `Legacy` | `Public` |

The pipeline does not enforce these pairings — running `Public` against a feature branch will succeed and produce versions with the commit hash appended. Stick to the table for production releases.
```

- [ ] **Step 4: Update the "Publishing Packages" section to use packageSet**

Locate `## Publishing Packages (Teams.NET-ESRP pipeline)` (currently around line 80). Update the procedure text:

```markdown
## Publishing Packages (Teams.NET-ESRP pipeline)

`Teams.NET-ESRP` is triggered manually. Pick both:

- **Package Set**: `Legacy` (releases from `Libraries/`) or `Core` (releases from `core/`)
- **Publish Type**: `Internal` (push to `TeamsSDKPreviews` ADO feed) or `Public` (sign + push to nuget.org)

The version comes from nbgv (root `version.json` for Legacy, `core/version.json` for Core), so the same pipeline produces previews from `main` and stable releases from a `releases/*` branch.

### Internal

Pushes unsigned packages to the `TeamsSDKPreviews` internal ADO feed. No approval required.

1. Pipelines > Teams.NET-ESRP > Run pipeline
2. Select the branch (`main` for previews, `releases/v*` or `releases/core` for stable releases)
3. Choose Package Set and Publish Type: `Internal`
4. Stages: Build → Test → Pack → Push to internal feed

### Public

Signs (Authenticode + NuGet) and pushes to nuget.org. Requires approval.

1. Pipelines > Teams.NET-ESRP > Run pipeline
2. Select the branch per the "Switching the released set" table
3. Choose Package Set, Publish Type: `Public`
4. Stages: Build → Test → Sign → Pack → wait for approval → PushToNuGet
5. Approver reviews and clicks Approve
6. Packages land on [nuget.org/profiles/teams-sdk](https://www.nuget.org/profiles/teams-sdk)
```

- [ ] **Step 5: Update "CI Validation" section**

Locate `## CI Validation (Teams.NET-PR pipeline)` (currently around line 138). Replace with:

```markdown
## CI Validation (Teams.NET-PR pipeline)

`Teams.NET-PR` runs on PRs targeting `main` or `releases/*` and on pushes to `main`. It does not publish.

1. Open or update a PR targeting `main` or `releases/*`.
2. The `DetectChanges` stage computes which package sets changed.
3. `Build_Legacy` and `Build_Core` run conditionally based on the change flags. Both can run in parallel; both can be skipped (docs-only PRs).
4. Unsigned packages are produced as downloadable pipeline artifacts (for local testing).

**Path detection rules:**
- `Libraries/**`, `Samples/**`, `Tests/**`, `Microsoft.Teams.sln`, `version.json` → Legacy
- `core/**` → Core
- `.editorconfig`, `.azdo/**`, `Makefile` → both (shared infrastructure)
- `**/*.md`, `docs/**`, `core/docs/**`, `Assets/**` → neither (pipeline runs and reports green with no builds)
```

- [ ] **Step 6: Validate the doc renders**

Open `RELEASE.md` in a Markdown viewer or VS Code preview. Verify:
- All four tables render correctly
- No broken inter-section references
- No leftover references to `next/*` branches or BotCore-CD pipeline

Run: `git grep -n "next/" RELEASE.md` → expected zero matches
Run: `git grep -n "BotCore-CD" RELEASE.md` → expected zero matches
Run: `git grep -n "release/\*" RELEASE.md` → expected zero matches (only `releases/*`)

- [ ] **Step 7: Commit**

```bash
git add RELEASE.md
git commit -m "docs: rewrite RELEASE.md for unified Legacy + Core pipelines

- New Pipelines Overview table reflecting the two-pipeline structure
- Versioning section covers both root version.json and core/version.json
- New 'Switching the released set' table documents branch / packageSet /
  publishType pairings
- CI Validation section documents the DetectChanges rules
- Stale-content fixes: drop next/* branch references (now core/* for
  source, releases/core for stable), remove false 'excludes core/**
  paths' claim, standardize on releases/* (plural)"
```

---

## Task 8: Self-PR validation

Final integration check before merge — all six file changes living together on one branch.

**Files:**
- (none — pure validation)

- [ ] **Step 1: Push branch and open the consolidation PR against `main`**

```bash
git push -u origin HEAD
```

Open the PR via UI. Title: "Consolidate ADO pipelines for Legacy and Core".

PR description should include the rollback note from the spec:

```markdown
## Rollback

Revert this PR. All pipeline definitions reference repo paths, so
reverting restores cd-core.yaml + the old ci.yaml/publish.yaml
automatically. Manual rollback step: ADO admin recreates the BotCore-CD
pipeline definition pointing at the restored cd-core.yaml and re-adds
it to branch policy.
```

- [ ] **Step 2: Watch Teams.NET-PR run on the PR**

Open Pipelines > Teams.NET-PR > latest run.

The PR touches `.azdo/**` (multiple files) and `RELEASE.md`. DetectChanges should flag BOTH `legacyChanged=true` and `coreChanged=true` (because `.azdo/*` is in the shared bucket).

Verify both `Build_Legacy` and `Build_Core` stages run green and produce `Packages` artifacts. If either fails, revert that task's commits and re-run.

- [ ] **Step 3: Manual smoke test — `publish.yaml` Internal, both sets**

ADO admin queues `Teams.NET-ESRP` against the PR branch:

Run #1:
- Branch: this PR's source branch
- Package Set: `Core`
- Publish Type: `Internal`
- Expected: pushes 3 Core packages to `TeamsSDKPreviews` (with `-preview.{height}-g<sha>` suffix because the feature branch isn't in `publicReleaseRefSpec`)

Run #2:
- Branch: this PR's source branch
- Package Set: `Legacy`
- Publish Type: `Internal`
- Expected: pushes ~14-16 Legacy packages to `TeamsSDKPreviews`

If either Internal run fails, fix and recommit; do NOT merge.

Do NOT test `Public` from the feature branch — that triggers ESRP signing + approval flow against unmerged code.

- [ ] **Step 4: Get PR review and merge**

Standard review. Once approved, merge to `main`.

- [ ] **Step 5: Post-merge ADO portal cutover**

These steps happen in the ADO web UI by an admin — not in the repo.

1. **Delete the BotCore-CD pipeline definition.** Pipelines > BotCore-CD > Delete (or rename → archive). The file it points at (`.azdo/cd-core.yaml`) no longer exists; the pipeline would fail to load on its next trigger.
2. **Update branch policies on `main` and each active `releases/*` branch.** Project Settings > Repositories > Microsoft.Teams.SDK > Policies > Branch policies. For each protected branch:
   - Remove the BotCore-CD required-build-validation entry.
   - Keep Teams.NET-PR required (it now validates both groups).
3. **No changes** to the `teams-net-publish` environment, Teams.NET-PR pipeline definition, or Teams.NET-ESRP pipeline definition. Those keep pointing at the same file paths.

- [ ] **Step 6: Post-merge production smoke test**

Wait for the next routine preview release of either package set. The first production-grade run of the new pipeline is the smoke test. Watch the run; if it fails, the rollback procedure in the PR description applies.

---

## Summary of Commits

This plan produces 6 commits, all on one PR branch:

1. `ci: add shared build-test-pack template` (Task 1)
2. `ci: parameterize sign-and-pack template DLL and csproj patterns` (Task 2)
3. `ci: rewrite ci.yaml as unified Legacy + Core CI` (Task 3)
4. `ci: parameterize publish.yaml on packageSet (Legacy|Core)` (Task 5)
5. `ci: delete cd-core.yaml; Core CI is now handled by unified ci.yaml` (Task 6)
6. `docs: rewrite RELEASE.md for unified Legacy + Core pipelines` (Task 7)

Tasks 4 and 8 are validation only — no commits.
