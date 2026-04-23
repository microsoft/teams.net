# Release Process

This document describes how to release packages for the Teams SDK for .NET. It assumes you have required entitlements in Azure DevOps for triggering releases.

## Pipelines Overview

| Pipeline | File | Trigger | Signing | Destination | Approval |
|----------|------|---------|---------|-------------|----------|
| **Teams.NET-PR** | ci.yaml | PR to `main`/`release/*` | No | Pipeline artifacts only | None |
| **Teams.NET-ESRP** | publish.yaml | Manual (`Internal`/`Public`) | Public only | Internal feed or nuget.org | Public only |
| **BotCore-CD** | cd-core.yaml | PR/push to `next/*` (`core/**`) | No | Internal feed (`next/core` branch) | Auto |

Note: Public packages are available on nuget.org. Internal feed packages are for Microsoft internal use.

## Versioning

Versions are managed by **Nerdbank.GitVersioning** via [version.json](version.json).

### Current Configuration

```json
{
  "version": "0.0.0-preview.{height}"
}
```

**All builds currently produce preview versions** like `0.0.0-preview.N` because the `-preview.{height}` suffix is baked into version.json.

### Example Package Names

| Build Height | Package Name |
|--------------|--------------|
| N | `Microsoft.Teams.Apps.0.0.0-preview.N.nupkg` |
| N+1 | `Microsoft.Teams.Apps.0.0.0-preview.N+1.nupkg` |

> **Note:** Manually running a pipeline on a branch not in `publicReleaseRefSpec` (e.g., a feature branch) produces versions with the commit hash appended, like `0.0.0-preview.N-g1a2b3c4`. This is useful for testing packages before a PR is approved/merged.

### Producing a Stable Release Version

To produce a non-preview release (e.g., `2.0.7`), you must work from the `releases/v2` branch:

1. Merge `main` into `releases/v2`:
   ```bash
   git checkout releases/v2
   git merge main
   ```
2. Edit `version.json` to remove the preview suffix:
   ```json
   {
     "version": "2.0.7"
   }
   ```
3. Commit and push the version change to `releases/v2`
4. Run the **Teams.NET-ESRP** (`publish.yaml`) pipeline manually from the `releases/v2` branch with **Public** publish type
5. Approve the push to nuget.org
6. After the release, bump the version for the next cycle:
   ```json
   {
     "version": "2.0.8-preview.{height}"
   }
   ```

### Producing Preview Releases

Preview releases can be published directly from the `main` branch since `version.json` on `main` includes the preview suffix.

### Note on publicReleaseRefSpec

The `publicReleaseRefSpec` in version.json controls metadata (e.g., whether a build is considered "public" for telemetry), but it does **not** affect the version number itself. The version string is determined entirely by the `"version"` field.

## Approvers

The `teams-net-publish` environment in Azure DevOps controls who can approve releases. To modify approvers:

1. Go to **Pipelines** > **Environments**
2. Select **teams-net-publish**
3. Click **Approvals and checks**
4. Add/remove approvers as needed

## Publishing Packages (Teams.NET-ESRP pipeline)

The `Teams.NET-ESRP` pipeline is triggered manually and requires selecting a **Publish Type**: `Internal` or `Public`. The version of the packages is determined by Nerdbank.GitVersioning from `version.json`, so the same pipeline can publish both preview and stable releases.

**Branch Strategy:**
- **Preview releases**: Publish from `main` branch (version.json contains preview suffix)
- **Stable releases**: Publish from `releases/v2` branch (version.json has no suffix)

### Internal Packages

Pushes unsigned packages to the internal ADO `TeamsSDKPreviews` feed (useful for testing before public release).

1. Go to **Pipelines** > **Teams.NET-ESRP**
2. Click **Run pipeline**
3. Select the branch to build from (`main` for previews, `releases/v2` for stable)
4. Choose **Internal** as the Publish Type
5. Pipeline runs: Build > Test > Pack > Push to internal feed

No approval is required. Packages are available immediately in the internal feed.

### Public Packages

Signs packages (Authenticode + NuGet) and pushes to nuget.org. The package version (preview or stable) is determined by `version.json` on the selected branch.

1. Go to **Pipelines** > **Teams.NET-ESRP**
2. Click **Run pipeline**
3. Select the branch to build from:
   - `main` for preview releases
   - `releases/v2` for stable releases
4. Choose **Public** as the Publish Type
5. Pipeline runs: Build > Test > Sign > Pack
6. **PushToNuGet stage** waits for approval
7. Approver reviews in ADO and clicks **Approve**
8. Packages are pushed to nuget.org

#### Installing Published Packages

Once published, packages are available on the [teams-sdk nuget.org profile](https://www.nuget.org/profiles/teams-sdk).

For stable releases:
```bash
dotnet add package Microsoft.Teams.Apps --version 0.0.0
```

For preview releases:
```bash
dotnet add package Microsoft.Teams.Apps --version 0.0.0-preview.N
```

You can search for available versions using:
```bash
# Stable only
dotnet package search Microsoft.Teams.Apps

# Include prereleases
dotnet package search Microsoft.Teams.Apps --prerelease
```

## CI Validation (Teams.NET-PR pipeline)

The `Teams.NET-PR` pipeline runs automatically on PRs targeting `main` or `release/*` branches (excluding `core/**` paths). It does not publish packages.

1. Open or update a PR targeting `main` or `release/*`
2. Pipeline runs: Build > Test > Pack
3. Unsigned packages are produced as downloadable pipeline artifacts (for local testing)
