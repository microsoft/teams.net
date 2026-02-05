# Release Process

This document describes how to release packages for the Teams SDK for .NET. It assumes you have required entitlements in Azure DevOps for triggering releases.

## Pipelines Overview

| Pipeline | File | Trigger | Signing | Destination | Approval |
|----------|------|---------|---------|-------------|----------|
| **teams.net-pr** | ci.yaml | PR to `main`/`release/*` | Yes | nuget.org (`main` only) | Required (for public previews) |
| **teams.net** | publish.yml | Manual | Yes | nuget.org | Required |
| **BotCore-CD** | cd-core.yaml | PR/push to next/* (core/**) | No | Internal feed | Auto (`next/core` branch) |

Note: `next/core` releases are not made available publicly. For information on internal feed consumption, please contact a teams-sdk team member internally.

## Versioning

Versions are managed by **Nerdbank.GitVersioning** via [version.json](version.json).

### Current Configuration

```json
{
  "version": "X.Y.Z-preview.{height}"
}
```

**All builds currently produce preview versions** like `X.Y.Z-preview.N` because the `-preview.{height}` suffix is baked into version.json.

### Example Package Names

| Build Height | Package Name |
|--------------|--------------|
| N | `Microsoft.Teams.Apps.X.Y.Z-preview.N.nupkg` |
| N+1 | `Microsoft.Teams.Apps.X.Y.Z-preview.N+1.nupkg` |

> **Note:** Manually running the pipeline on a branch not in `publicReleaseRefSpec` (e.g., a feature branch) produces versions with the commit hash appended, like `X.Y.Z-preview.N-g1a2b3c4`. This is useful for testing packages before a PR is approved/merged.

### Producing a Stable Release Version

To produce a non-preview release (e.g., `X.Y.Z`), you must **edit version.json before running publish.yml**:

1. Create a PR to change version.json:
   ```json
   {
     "version": "X.Y.Z"
   }
   ```
2. Merge the PR
3. Run the **teams.net** (`publish.yml`) pipeline manually
4. Approve the push to nuget.org
5. Create another PR to bump the version for the next preview cycle:
   ```json
   {
     "version": "X.Y.Z+1-preview.{height}"
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

## Publishing to nuget.org

### Preview Releases (teams.net-pr pipeline)

The `teams.net-pr` pipeline runs on PRs and merges to `main` or `release/*` branches.

**On PR (build validation):**
1. Open or update a PR targeting `main` or `release/*`
2. Pipeline runs: Build > Test > Sign > Pack
3. Signed packages are produced as pipeline artifacts

**After merge to `main` (publish):**
1. `PushToNuGet` stage is triggered
2. **PushToNuGet stage** waits for approval
3. Approver reviews in ADO and clicks **Approve**
4. Packages are pushed to nuget.org

#### Installing Published Preview Packages

Preview packages, once published, work identically to stable releases and are available on the same profile:

```bash
dotnet add package Microsoft.Teams.Apps --version X.Y.Z-preview.N
```

Available preview versions can be found on the [teams-sdk nuget.org profile](https://www.nuget.org/profiles/teams-sdk) or by using:

```bash
dotnet package search Microsoft.Teams.Apps --prerelease
```

### Production Releases (teams.net pipeline)

Production releases are triggered manually.

1. Go to **Pipelines** > **teams.net**
2. Click **Run pipeline**
3. Select the branch/tag to release
4. Pipeline runs: Build > Test > Sign > Pack
5. **PushToNuGet stage** waits for approval
6. Approver reviews in ADO and clicks **Approve**
7. Packages are pushed to nuget.org
