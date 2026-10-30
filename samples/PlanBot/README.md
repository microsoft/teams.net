# PlanBot

Demonstrates the `Plan` and `Step` features.
Plan APIs are experimental; the project opts in with `ExperimentalTeamsPlan`.

Configure a registered Teams bot as described in the [repository quick start](../../README.md),
then run:

```sh
dotnet run --project samples/PlanBot/PlanBot.csproj
```

| Command | Behavior |
|---|---|
| `complete` | Versioned progress, a blocked step, a documentation source with the shared Text citation icon, and a final message |
| `complete-stream` | The same progress followed by existing writer streaming; only its final message carries Plan completion |
| `stop` | Start an active lookup, wait five seconds, then stop before completing the work |
| `error` | Start a lookup, update it to an error step, then explicitly report the plan error |
| `proactive` | Return from the handler, reconstruct from saved IDs after 10 seconds, update steps, and complete later |

`complete-stream` passes `plan.Completion()` to `writer.FinalizeResponseAsync(...)`.
The helper only builds prompt-bound completion metadata; `plan.CompleteAsync(...)` instead sends a final message directly.

The delayed example keeps an authenticated, service-scoped client in memory and serializes
only `PlanOptions` containing the conversation and originating prompt IDs. 

Application code owns every lifecycle call and increments each step's version
whenever its title, status, description, or sources change.
