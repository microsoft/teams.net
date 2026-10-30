// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema.Entities;

namespace PlanBot;

internal sealed class PlanDemo(ILogger logger, CancellationToken stopping, TimeProvider? timeProvider = null)
{
    private readonly ConcurrentDictionary<Task, byte> _continuations = new();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    internal int PendingCount => _continuations.Count;

    internal Task WaitForPendingAsync() => Task.WhenAll(_continuations.Keys);

    internal async Task HandleAsync(Context<MessageActivity> context, CancellationToken cancellationToken)
    {
        string command = (context.Activity.TextWithoutMentions ?? "").ToLowerInvariant();
        if (command is not ("complete" or "complete-stream" or "stop" or "error" or "proactive"))
        {
            await context.SendAsync("Try: complete, complete-stream, stop, error, or proactive.", cancellationToken);
            return;
        }

        Plan plan = Plan.CreateFromContext(context);
        switch (command)
        {
            case "stop":
                await plan.StartAsync([LookupStep()], cancellationToken);
                await DelayAsync(5, cancellationToken);
                await plan.StopAsync(cancellationToken);
                break;
            case "error":
                await plan.StartAsync([LookupStep()], cancellationToken);
                await DelayAsync(2, cancellationToken);
                await plan.UpdateAsync(
                    [Step("lookup", 2, "Source lookup failed", PlanStepStatuses.Error,
                        "The source could not be retrieved, so the lookup cannot continue.")], cancellationToken);
                await DelayAsync(2, cancellationToken);
                await plan.ErrorAsync(cancellationToken);
                break;
            case "proactive":
                await plan.StartAsync(
                    [Step("report", 1, "Preparing background report", PlanStepStatuses.Active,
                        "This report continues in the background after the handler returns.")], cancellationToken);
                string saved = JsonSerializer.Serialize(new PlanOptions
                {
                    ConversationId = plan.ConversationId,
                    ReplyToId = plan.ReplyToId
                });
                // Persist only the IDs. The scoped client stays in this process, not the Context.
                Task continuation = ContinueLaterAsync(context.Api, saved, stopping);
                _continuations.TryAdd(continuation, 0);
                _ = continuation.ContinueWith(task => _continuations.TryRemove(task, out _),
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                break;
            case "complete-stream":
                await PrepareAsync(plan, "The response is ready to stream.", cancellationToken);
                TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);
                foreach (string chunk in new[] { "Here is ", "the answer ", "based on the sources." })
                {
                    await writer.AppendResponseAsync(chunk, cancellationToken);
                    await DelayAsync(0.5, cancellationToken);
                }
                await writer.FinalizeResponseAsync(plan.Completion(), cancellationToken);
                break;
            default:
                await PrepareAsync(plan, "The response is ready to send.", cancellationToken);
                await plan.CompleteAsync("Here is the answer based on the sources.", cancellationToken);
                break;
        }
    }

    private Task DelayAsync(double seconds, CancellationToken cancellationToken)
        => Task.Delay(TimeSpan.FromSeconds(seconds), _timeProvider, cancellationToken);

    private static PlanStep Step(string id, int version, string title, PlanStepStatus status, string description)
        => new() { Id = id, Version = version, Title = title, Status = status, Description = description };

    private static PlanStep LookupStep() => Step("lookup", 1, "Checking sources", PlanStepStatuses.Active,
        "Looking for relevant information in the Teams SDK documentation.");

    private async Task PrepareAsync(Plan plan, string readyDescription, CancellationToken cancellationToken)
    {
        await plan.StartAsync(
            [LookupStep(), Step("answer", 1, "Draft answer", PlanStepStatuses.Proposed,
                "Summarize the findings once the sources have been checked.")], cancellationToken);
        await DelayAsync(2, cancellationToken);
        await plan.UpdateAsync(
            [Step("lookup", 2, "Waiting for source response", PlanStepStatuses.Blocked,
                "The source has not responded yet; waiting before continuing.")], cancellationToken);
        await DelayAsync(2, cancellationToken);
        PlanStep found = Step("lookup", 3, "Sources checked", PlanStepStatuses.Completed,
            "Found the relevant Teams SDK documentation for the answer.");
        found.Sources =
        [
            new StepSource
            {
                Title = "Teams SDK docs",
                Url = new Uri("https://microsoft.github.io/teams-sdk/"),
                Icon = CitationIcons.Text
            }
        ];
        await plan.UpdateAsync(
            [found, Step("answer", 2, "Drafting answer", PlanStepStatuses.Active,
                "Turning the source information into a concise response.")], cancellationToken);
        await DelayAsync(2, cancellationToken);
        await plan.UpdateAsync(
            [Step("answer", 3, "Answer ready", PlanStepStatuses.Completed, readyDescription)], cancellationToken);
        await DelayAsync(2, cancellationToken);
    }

    private async Task ContinueLaterAsync(ApiClient api, string saved, CancellationToken cancellationToken)
    {
        try
        {
            await DelayAsync(10, cancellationToken);
            PlanOptions options = JsonSerializer.Deserialize<PlanOptions>(saved)
                ?? throw new InvalidOperationException("The saved Plan binding is missing.");
            Plan plan = new(api, options);
            await plan.UpdateAsync(
                [Step("report", 2, "Report ready", PlanStepStatuses.Completed,
                    "The background work is complete and the report is ready to send."),
                 Step("deliver", 1, "Delivering report", PlanStepStatuses.Active,
                    "Finalizing delivery of the completed report back to the user.")], cancellationToken);
            await DelayAsync(2, cancellationToken);
            await plan.UpdateAsync(
                [Step("deliver", 2, "Delivered report", PlanStepStatuses.Completed,
                    "Finalized delivery of the completed report back to the user.")], cancellationToken);
            await DelayAsync(10, cancellationToken);
            await plan.CompleteAsync("Your background report is ready.", cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Delayed Plan work cancelled during shutdown.");
        }
        catch (Exception error)
        {
            // This is the application-owned background task boundary; failures must be observed.
            logger.LogError(error, "Delayed Plan work failed.");
        }
    }
}
