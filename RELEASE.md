# Release Process

This document describes how to release packages for the Teams SDK for .NET. It assumes you have required entitlements in Azure DevOps for triggering releases.

## Creating a Release

1. Create a branch from `main`:
   ```bash
   git checkout -b prep-release/<version> origin/main
   ```

2. Update `version.json` to set the stable version (remove the `-preview.{height}` suffix):
   - **Core**: edit `core/version.json` (e.g., set `"version": "1.0.3"`)
     - Do **not** modify `core/src/Microsoft.Teams.Apps/version.json` — keep its preview suffix as-is for now
   - **Legacy**: edit root `version.json` (e.g., set `"version": "2.0.8"`)

3. Commit, then merge the release branch history using the `ours` strategy:
   ```bash
   git add .
   git commit -m "set version to <version>"
   git merge -s ours origin/releases/core   # or origin/releases/v2 for Legacy
   ```
   This records the release branch's history as merged without introducing conflicts.

4. Push and create a PR to the release branch:
   ```bash
   git push -u origin prep-release/<version>
   ```
   Create a PR (base: `releases/core` or `releases/v2`, compare: `prep-release/<version>`). Get teammate approval and merge.

5. Trigger the `Teams.NET-ESRP` pipeline from the release branch with `publishType=Public`:
   - **Core**: `packageSet=Core` from `releases/core`
   - **Legacy**: `packageSet=Legacy` from `releases/v2`

6. Approve the push to nuget.org

7. Bump the version on `main` for the next release cycle:
   - **Core**: edit `core/version.json` (e.g., `"1.0.3-preview.{height}"` → `"1.0.4-preview.{height}"`)
   - **Legacy**: edit root `version.json` (e.g., `"2.0.8-preview.{height}"` → `"2.0.9-preview.{height}"`)
   - Commit and push (or PR)

## Hotfixes

To fix a bug in a released version without including new preview changes:

> Consider if a normal release would work instead — merging `main` to the release branch includes all updates and is simpler. Only use a hotfix if you need to exclude preview changes from `main`.

1. Create a branch from the release branch:
   ```bash
   git checkout -b hotfix/fix-description origin/releases/core  # or origin/releases/v2
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

There are two package sets, each with its own `version.json`:

- **Core** (`core/`): `core/version.json` (with a per-project override at `core/src/Microsoft.Teams.Apps/version.json`)
- **Legacy** (`Libraries/`): root `version.json`

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

This project uses [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) (nbgv) for automatic version management.

## Publishing

The publish pipeline (`Teams.NET-ESRP` / `publish.yaml`) is manually triggered.

1. Go to **Pipelines > Teams.NET-ESRP** in ADO
2. Click **Run pipeline**
3. Select the branch to build from (either `releases/v2` for Legacy or `releases/core` for Core)
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

## Appendix: Tagging and GitHub Release

> **Do this after the publish pipeline finishes and packages land on nuget.org** — not before. The tag is created at the release branch tip when you publish the draft, so it should point at the exact commit whose artifacts shipped.

Create a draft release at the release branch tip:

```bash
gh release create v<version> -R microsoft/teams.net \
  --target releases/<branch> --title "v<version>" --draft \
  --generate-notes --notes-start-tag v<previous-version>
```

- `--target` is the release branch packages were published from: `releases/core` for Core, `releases/v<N>` for Legacy.
- The tag is created at the release branch tip when you publish the draft.

If the auto-generated PR list comes back too small (only the release PR itself, because squash-merges hide ancestry), query the real PR delta from `main`:

```bash
gh api -X GET search/issues \
  -f q='repo:microsoft/teams.net is:pr is:merged base:main merged:>=<previous-release-publish-date>' \
  --jq '.items[] | "* \(.title) by @\(.user.login) in \(.html_url)"' | tac > /tmp/notes.md
```

Edit the draft (`gh release edit <id> --notes-file /tmp/notes.md`), then publish from the GitHub UI to create the tag.
