# Release Process

This document describes how to release packages for the Teams SDK for .NET. It assumes you have required entitlements in Azure DevOps for triggering releases.

## Pipelines Overview

| Pipeline | File | Trigger | Scope | Signing | Destination | Approval |
|----------|------|---------|-------|---------|-------------|----------|
| **Teams.NET-PR** | `ci.yaml` | PR `main` + `releases/*`; push `main` | Legacy and/or Core via in-pipeline change detection | No | Pipeline artifacts only | None |
| **Teams.NET-ESRP** | `publish.yaml` | Manual (`packageSet` × `publishType`) | Legacy or Core (per run) | `Public` only | `TeamsSDKPreviews` internal feed or nuget.org | `Public` only |

Note: Public packages are available on nuget.org. Internal feed packages are for Microsoft internal use.

The `Teams.NET-PR` pipeline always runs on covered triggers. A `DetectChanges` stage inspects the changed paths and skips the Legacy and/or Core build stages when their respective package set is unchanged. Docs-only PRs produce a green run with both stages skipped.

## Versioning

Versions are managed by **Nerdbank.GitVersioning** (nbgv). Each package set has its own version file:

- **Legacy** (`Libraries/`): root `version.json` (e.g., `2.0.7-preview.{height}`)
- **Core** (`core/`): `core/version.json` (e.g., `1.0`)

Plus one per-project override at `core/src/Microsoft.Teams.Apps/version.json` (currently `2.1.0-alpha.{height}`).

### Preview vs Stable

When `version.json` has a `-preview` (or `-alpha`) suffix, every build produces a preview package (e.g., `Microsoft.Teams.Apps.2.0.7-preview.42.nupkg`). When the suffix is removed and the file is on a branch listed in `publicReleaseRefSpec`, builds produce stable packages.

**Manually-queued runs from a branch not in `publicReleaseRefSpec`** produce versions with the commit hash appended (e.g., `2.0.7-preview.42-g1a2b3c4`). Useful for testing packages from a feature branch before merge.

### Producing a Stable Release

Core stable release (e.g., `1.0.0`):

1. Check out the Core stable branch:
   ```bash
   git checkout releases/core
   git merge main
   ```
2. Edit `core/version.json` to remove any preview suffix (and `core/src/Microsoft.Teams.Apps/version.json` if its independent version applies):
   ```json
   { "version": "1.0.0" }
   ```
3. Commit and push to `releases/core`.
4. Queue `Teams.NET-ESRP` from `releases/core` with `packageSet=Core, publishType=Public`.
5. Approve the push to nuget.org.
6. After the release, bump for the next cycle on `main` (e.g., `"version": "1.0.1-preview.{height}"`).

Legacy stable release: same flow, but on `releases/vN` (e.g., `releases/v2`), editing root `version.json` instead of the Core version files, and queueing with `packageSet=Legacy`.

### Note on `publicReleaseRefSpec`

`publicReleaseRefSpec` controls metadata only (whether nbgv treats a build as "public"). It does **not** affect the version string — that's determined entirely by the `"version"` field.

## Switching the released set

| Goal | Branch | `packageSet` | `publishType` |
|---|---|---|---|
| Core preview → internal feed | `main` | `Core` | `Internal` |
| Core stable → nuget.org | `releases/core` | `Core` | `Public` |
| Legacy preview → internal feed | `main` | `Legacy` | `Internal` |
| Legacy stable → nuget.org | `releases/vN` | `Legacy` | `Public` |

The pipeline does not enforce these pairings — running `Public` against a feature branch will succeed and produce versions with the commit hash appended. Stick to the table for production releases.

## Approvers

The `teams-net-publish` environment in Azure DevOps controls who can approve releases. To modify approvers:

1. Go to **Pipelines** > **Environments**
2. Select **teams-net-publish**
3. Click **Approvals and checks**
4. Add/remove approvers as needed

## Publishing Packages (Teams.NET-ESRP pipeline)

`Teams.NET-ESRP` is triggered manually. Pick both:

- **Package Set**: `Legacy` (releases from `Libraries/`) or `Core` (releases from `core/`)
- **Publish Type**: `Internal` (push to `TeamsSDKPreviews` ADO feed) or `Public` (sign + push to nuget.org)

The version comes from nbgv (root `version.json` for Legacy, `core/version.json` for Core), so the same pipeline produces previews from `main` and stable releases from a `releases/*` branch.

### Internal

Pushes unsigned packages to the `TeamsSDKPreviews` internal ADO feed. No approval required.

1. Pipelines > Teams.NET-ESRP > Run pipeline
2. Select the branch (`main` for previews, `releases/v*` or `releases/core` for stable previews)
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
