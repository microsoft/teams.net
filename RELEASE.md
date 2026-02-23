# Release Process

This document describes how to release packages for the Teams SDK for .NET. It assumes you have required entitlements in Azure DevOps for triggering releases.

## Pipelines Overview

| Pipeline | File | Trigger | Signing | Destination | Approval |
|----------|------|---------|---------|-------------|----------|
| **teams.net-pr** | ci.yaml | PR to `main`/`release/*` | No | Pipeline artifacts only | None |
| **teams.net-preview** | publish-preview.yaml | Manual (`Internal`/`Public`) | Public only | Internal feed or nuget.org | Public only |
| **teams.net** | publish.yml | Manual | Yes | nuget.org | Required |
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

To produce a non-preview release (e.g., `0.0.0`, without suffix), you must **edit version.json before running publish.yml**:

1. Create a PR to change version.json:
   ```json
   {
     "version": "0.0.0"
   }
   ```
2. Merge the PR
3. Run the **teams.net** (`publish.yml`) pipeline manually
4. Approve the push to nuget.org
5. Create another PR to bump the version for the next preview cycle:
   ```json
   {
     "version": "0.0.0+1-preview.{height}"
   }
   ```

### Note on publicReleaseRefSpec

The `publicReleaseRefSpec` in version.json controls metadata (e.g., whether a build is considered "public" for telemetry), but it does **not** affect the version number itself. The version string is determined entirely by the `"version"` field.

## Approvers

The `teams-net-publish` environment in Azure DevOps controls who can approve releases. To modify approvers:

1. Go to **Pipelines** > **Environments**
2. Select **teams-net-publish**
3. Click **Approvals and checks**
4. Add/remove approvers as needed

## Publishing Preview Packages (publish-preview pipeline)

The `publish-preview` pipeline is triggered manually and requires selecting a **Publish Type**: `Internal` or `Public`.

### Internal Previews

Pushes unsigned packages to the internal ADO `TeamsSDKPreviews` feed.

1. Go to **Pipelines** > **publish-preview**
2. Click **Run pipeline**
3. Select the branch to build from
4. Choose **Internal** as the Publish Type
5. Pipeline runs: Build > Test > Pack > Push to internal feed

No approval is required. Packages are available immediately in the internal feed.

### Public Previews

Signs packages (Authenticode + NuGet) and pushes to nuget.org.

1. Go to **Pipelines** > **publish-preview**
2. Click **Run pipeline**
3. Select the branch to build from
4. Choose **Public** as the Publish Type
5. Pipeline runs: Build > Test > Sign > Pack
6. **PushToNuGet stage** waits for approval
7. Approver reviews in ADO and clicks **Approve**
8. Packages are pushed to nuget.org

#### Installing Published Preview Packages

Preview packages, once published, work identically to stable releases and are available on the same profile:

```bash
dotnet add package Microsoft.Teams.Apps --version 0.0.0-preview.N
```

Available preview versions can be found on the [teams-sdk nuget.org profile](https://www.nuget.org/profiles/teams-sdk) or by using:

```bash
dotnet package search Microsoft.Teams.Apps --prerelease
```

## Production Releases (teams.net pipeline)

Production releases are triggered manually via `publish.yml`.

1. Go to **Pipelines** > **teams.net**
2. Click **Run pipeline**
3. Select the branch/tag to release
4. Pipeline runs: Build > Test > Sign > Pack
5. **PushToNuGet stage** waits for approval
6. Approver reviews in ADO and clicks **Approve**
7. Packages are pushed to nuget.org

## CI Validation (teams.net-pr pipeline)

The `teams.net-pr` pipeline runs automatically on PRs targeting `main` or `release/*` branches (excluding `core/**` paths). It does not publish packages.

1. Open or update a PR targeting `main` or `release/*`
2. Pipeline runs: Build > Test > Pack
3. Unsigned packages are produced as downloadable pipeline artifacts (for local testing)
