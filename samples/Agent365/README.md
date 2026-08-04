# Agent 365 Sample

Demonstrates handling Agent 365 `agentLifecycle` events in a Teams bot.

`Program.cs` mirrors the Agent 365 sample in `teams.ts`: it logs one general lifecycle handler plus typed handlers for each observed `AgenticUser*` lifecycle variant. It also includes a simple echo message handler so the bot can still respond to regular messages.

## Lifecycle handlers

The sample registers:

| Handler | Event |
|---------|-------|
| `OnAgentLifecycle` | All `agentLifecycle` events |
| `OnAgenticUserIdentityCreated` | `AgenticUserIdentityCreated` |
| `OnAgenticUserIdentityUpdated` | `AgenticUserIdentityUpdated` |
| `OnAgenticUserManagerUpdated` | `AgenticUserManagerUpdated` |
| `OnAgenticUserEnabled` | `AgenticUserEnabled` |
| `OnAgenticUserDisabled` | `AgenticUserDisabled` |
| `OnAgenticUserDeleted` | `AgenticUserDeleted` |
| `OnAgenticUserUndeleted` | `AgenticUserUndeleted` |
| `OnAgenticUserWorkloadOnboardingUpdated` | `AgenticUserWorkloadOnboardingUpdated` |

## Running the sample

```bash
dotnet run --project samples/Agent365/Agent365.csproj
```
