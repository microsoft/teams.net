# Release Process

This project uses [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) (nbgv) for automatic version management.

There are two package sets, each with its own `version.json`:

- **Core** (`core/`): `core/version.json` (with a per-project override at `core/src/Microsoft.Teams.Apps/version.json`)
- **Legacy** (`Libraries/`): root `version.json`

## Creating a Release

### Core

1. Create a branch from `releases/core` and merge `main` into it:
   ```bash
   git checkout -b prep-release/<next-version> releases/core
   git merge origin/main
   ```

2. Edit `core/version.json` to remove the `-preview.{height}` suffix (e.g., set `"version": "1.0.2"`)
   - Do **not** modify `core/src/Microsoft.Teams.Apps/version.json` — keep its preview suffix as-is for now

3. Commit, push, and create a PR to `releases/core` (base: `releases/core`, compare: `prep-release/<next-version>`):
   - The PR will include all changes from `main` plus the version bump
   - Get teammate approval and merge

4. Trigger the `Teams.NET-ESRP` pipeline from `releases/core` with `packageSet=Core, publishType=Public`

5. Approve the push to nuget.org

6. Bump the version on `main` for the next release cycle:
   - Edit `core/version.json` to increment the patch version (e.g., `"1.0.2-preview.{height}"` → `"1.0.3-preview.{height}"`)
   - Commit and push (or PR)

### Legacy

1. Create a branch from `releases/vN` and merge `main` into it:
   ```bash
   git checkout -b prep-release/<next-version> releases/v2
   git merge origin/main
   ```

2. Edit root `version.json` to remove the `-preview.{height}` suffix (e.g., set `"version": "2.0.7"`)

3. Commit, push, and create a PR to `releases/vN` (base: `releases/v2`, compare: `prep-release/<next-version>`):
   - The PR will include all changes from `main` plus the version bump
   - Get teammate approval and merge

4. Trigger the `Teams.NET-ESRP` pipeline from `releases/vN` with `packageSet=Legacy, publishType=Public`

5. Approve the push to nuget.org

6. Bump the version on `main` for the next release cycle:
   - Edit root `version.json` to increment the patch version (e.g., `"2.0.7-preview.{height}"` → `"2.0.8-preview.{height}"`)
   - Commit and push (or PR)

## Hotfixes

To fix a bug in a released version without including new preview changes:

> Consider if a normal release would work instead — merging `main` to the release branch includes all updates and is simpler. Only use a hotfix if you need to exclude preview changes from `main`.

1. Create a branch from the release branch:
   ```bash
   git checkout releases/v2
   git checkout -b hotfix/fix-description
   ```

2. Make your fix and commit

3. Create a PR to the release branch, get approval, and merge

4. Trigger the release pipeline

5. Cherry-pick the fix back to `main`:
   ```bash
   git checkout main
   git cherry-pick <commit-sha>
   git push origin main
   ```

## Experimental Features

To publish experimental versions from a feature branch:

1. Create your feature branch from `main`

2. Edit `version.json` on the feature branch:
   ```json
   { "version": "<current-version>-myfeature.{height}" }
   ```
   Commits produce: `<current-version>-myfeature.1`, `<current-version>-myfeature.2`, etc.

3. Publish from the feature branch using the release pipeline

4. When ready, merge to `main` (`main`'s `version.json` takes over)

## Bumping Major/Minor Version

To bump from `2.0.x` to `2.1.x` or `3.0.x`:

1. Edit `version.json` on `main`
2. Update the version (e.g., `"2.0.x-preview.{height}"` → `"2.1.0-preview.{height}"` or `"3.0.0-preview.{height}"`)
3. Commit and push

## How Versioning Works

Versions are computed automatically from git history based on `version.json`:

- **Main branch**: `X.Y.Z-preview.1`, `X.Y.Z-preview.2`, etc. (prerelease)
- **Release branch**: `X.Y.Z` (stable, published to nuget.org)
- **Feature branch**: versions include the commit hash (e.g., `2.0.7-preview.42-g1a2b3c4`)

When `version.json` has a `-preview` (or `-alpha`) suffix, every build produces a preview package. When the suffix is removed on a release branch, builds produce stable packages.

## Publishing

The publish pipeline (`Teams.NET-ESRP` / `publish.yaml`) is manually triggered.

1. Go to **Pipelines > Teams.NET-ESRP** in ADO
2. Click **Run pipeline**
3. Select the branch to build from
4. Choose **Package Set**: `Legacy` or `Core`
5. Choose **Publish Type**:
   - **Internal** — publishes unsigned packages to the `TeamsSDKPreviews` ADO feed. No approval required.
   - **Public** — signs (Authenticode + NuGet) and publishes packages to nuget.org. Requires approval.
6. Stages: Build → Test → Sign (Public only) → Pack → Publish

## Approvers

The `teams-net-publish` ADO pipeline environment controls who can approve public releases. To modify approvers:

1. Go to **Pipelines > Environments** in ADO
2. Select **teams-net-publish**
3. Click **Approvals and checks**
4. Add/remove approvers as needed

## Appendix: Pipeline Reference

| Pipeline | File | Trigger |
|----------|------|---------|
| **Teams.NET-PR** | `ci.yaml` | PRs targeting `main` or `releases/*`; pushes to `main` |
| **Teams.NET-ESRP** | `publish.yaml` | Manual (select `packageSet` and `publishType`) |

### CI path detection

The `Teams.NET-PR` pipeline has a `DetectChanges` stage that skips builds when the relevant package set is unchanged. Docs-only PRs produce a green run with no builds.

- `Libraries/**`, `Samples/**`, `Tests/**`, `Microsoft.Teams.sln`, `version.json` → Legacy
- `core/**` → Core
- `.editorconfig`, `.azdo/**`, `Makefile` → both (shared infrastructure)
- `**/*.md`, `docs/**`, `Assets/**` → neither (skipped)
