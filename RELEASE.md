# Release Process

This document describes how to release packages for the Teams SDK for .NET.

## Pipelines Overview

| Pipeline | File | Trigger | Signing | Destination | Approval |
|----------|------|---------|---------|-------------|----------|
| **teams.net-pr** | ci.yaml | PR to `main`/`release` | Yes | nuget.org | Required |
| **teams.net** | publish.yml | Manual | Yes | nuget.org | Required |
| **BotCore-CD** | cd-core.yaml | PR/push to next/* (core/**) | No | Internal feed | Auto (`next/core` branch) |

## Downloading Preview Packages from Pipeline Artifacts

After a pipeline run completes, signed packages are available as downloadable artifacts (unsigned for BotCore-CD).

### Steps to Download

1. Go to **Azure DevOps** > **Pipelines**
2. Select the pipeline (e.g., **teams.net-pr**)
3. Click on the completed **run** you want packages from
4. Find **Artifacts**:
   - Look for "1 published" link in the run summary, OR
   - Click the **Artifacts** tab at the top of the run page
5. Click **Packages** to browse or download

### Using Downloaded Packages Locally

After downloading and extracting the artifact:

```bash
# Option 1: Add as a local NuGet source
dotnet nuget add source /path/to/extracted/Packages --name LocalPreview

# Option 2: Reference in NuGet.config
```

Add to your `NuGet.config`:
```xml
<configuration>
  <packageSources>
    <add key="LocalPreview" value="/path/to/extracted/Packages" />
  </packageSources>
</configuration>
```

Then restore/install packages as normal:
```bash
dotnet restore
```

## Publishing to nuget.org

### Preview Releases (teams.net-pr pipeline)

Preview packages are automatically built when PRs are merged to `main` or `release/*` branches.

1. Merge PR to `main`
2. Pipeline runs: Build > Test > Sign > Pack
3. **PushToNuGet stage** waits for approval (main branch only)
4. Approver reviews in ADO and clicks **Approve**
5. Packages are pushed to nuget.org

### Production Releases (teams.net pipeline)

Production releases are triggered manually.

1. Go to **Pipelines** > **teams.net**
2. Click **Run pipeline**
3. Select the branch/tag to release
4. Pipeline runs: Build > Test > Sign > Pack
5. **PushToNuGet stage** waits for approval
6. Approver reviews in ADO and clicks **Approve**
7. Packages are pushed to nuget.org

## Versioning

Versions are managed by **Nerdbank.GitVersioning** via [version.json](version.json).

### Current Configuration

```json
{
  "version": "2.0.6-preview.{height}"
}
```

**All builds currently produce preview versions** like `2.0.6-preview.142` because the `-preview.{height}` suffix is baked into version.json.

### Example Package Names

| Build Height | Package Name |
|--------------|--------------|
| 142 | `Microsoft.Teams.Apps.2.0.6-preview.142.nupkg` |
| 143 | `Microsoft.Teams.Apps.2.0.6-preview.143.nupkg` |

### Producing a Stable Release

To produce a non-preview release (e.g., `2.0.6`), you must **edit version.json before running publish.yml**:

1. Create a PR to change version.json:
   ```json
   {
     "version": "2.0.6"
   }
   ```
2. Merge the PR
3. Run the **teams.net** (publish.yml) pipeline manually
4. Approve the push to nuget.org
5. Create another PR to bump the version for the next preview cycle:
   ```json
   {
     "version": "2.0.7-preview.{height}"
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
