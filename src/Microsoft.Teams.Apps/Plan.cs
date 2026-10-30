// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;

namespace Microsoft.Teams.Apps;

/// <summary>Explicit, prompt-bound plan updates. This helper does not own a workflow or a stream.</summary>
/// <remarks>
/// Persist the two IDs and supply a service-scoped API client when reconstructing for delayed work.
/// Targeted Plan scenarios are not supported. Plan activities use ordinary create routing, and
/// <see cref="Completion"/> does not inherit targeting from the originating context.
/// </remarks>
[Experimental("ExperimentalTeamsPlan")]
public sealed class Plan
{
    private readonly ConversationApiClient _client;

    /// <summary>The full conversation ID, including any thread suffix.</summary>
    [JsonPropertyName("conversationId")]
    public string ConversationId { get; }

    /// <summary>The originating prompt's activity ID.</summary>
    [JsonPropertyName("replyToId")]
    public string ReplyToId { get; }

    /// <summary>Raised after a successful send; typing acknowledgements have no message response.</summary>
    public event EventHandler<PlanSentEventArgs>? Sent;

    /// <summary>Creates a helper from saved IDs and an already service-scoped client. Sends nothing.</summary>
    public Plan(ApiClient apiClient, string conversationId, string replyToId)
        : this(apiClient, new PlanOptions { ConversationId = conversationId, ReplyToId = replyToId })
    {
    }

    /// <summary>Creates a helper from a saved prompt binding and an already service-scoped client. Sends nothing.</summary>
    public Plan(ApiClient apiClient, PlanOptions options)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ReplyToId);
        _client = apiClient.Conversations
            ?? throw new ArgumentException("Supply an API client scoped to the conversation's service URL.", nameof(apiClient));
        ConversationId = options.ConversationId;
        ReplyToId = options.ReplyToId;
    }

    /// <summary>Creates a standalone helper bound to the incoming activity's own ID.</summary>
    public static Plan CreateFromContext<TActivity>(Context<TActivity> context) where TActivity : TeamsActivity
    {
        ArgumentNullException.ThrowIfNull(context);
        string conversationId = context.Activity.Conversation?.Id
            ?? throw new ArgumentException("Activity must have a Conversation with an Id.", nameof(context));
        string replyToId = context.Activity.Id
            ?? throw new ArgumentException("Activity must have an Id.", nameof(context));
        return new Plan(context.Api, new PlanOptions { ConversationId = conversationId, ReplyToId = replyToId });
    }

    /// <summary>Starts a plan with the supplied steps.</summary>
    public Task StartAsync(IEnumerable<PlanStep> steps, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return SendUpdateAsync(PlanTypes.Start, steps, cancellationToken);
    }

    /// <summary>Adds or updates only the supplied steps. The application owns their versions.</summary>
    public Task UpdateAsync(IEnumerable<PlanStep> steps, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return SendUpdateAsync(PlanTypes.Continue, steps, cancellationToken);
    }

    /// <summary>Explicitly stops the plan.</summary>
    public Task StopAsync(CancellationToken cancellationToken = default)
        => SendUpdateAsync(PlanTypes.Stop, null, cancellationToken);

    /// <summary>Explicitly reports a plan error.</summary>
    public Task ErrorAsync(CancellationToken cancellationToken = default)
        => SendUpdateAsync(PlanTypes.Error, null, cancellationToken);

    /// <summary>Builds a fresh completion message for this Plan's prompt without sending it.</summary>
    /// <remarks>Pass it to a writer targeting the same conversation when finalizing.</remarks>
    public MessageActivityInput Completion()
        => new MessageActivityInput { ReplyToId = ReplyToId }
            .AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete });

    /// <summary>Completes the plan by creating a final message.</summary>
    public Task<SendActivityResponse?> CompleteAsync(string message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return CompleteAsync(new MessageActivityInput { Text = message }, cancellationToken);
    }

    /// <summary>Creates a final message, preserving caller content and existing completion metadata without mutating it.</summary>
    public Task<SendActivityResponse?> CompleteAsync(MessageActivityInput message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        MessageActivityInput activity = message.Copy();
        PlanInfoEntity[] plans = activity.Entities?.OfType<PlanInfoEntity>().ToArray() ?? [];
        if (plans.Any(entity => entity.PlanType != PlanTypes.Complete))
            throw new ArgumentException("CompleteAsync cannot send non-complete Plan operations.", nameof(message));
        if (plans.Length == 0)
            activity.AddEntity(new PlanInfoEntity { PlanType = PlanTypes.Complete });
        return SendActivityAsync(activity, cancellationToken);
    }

    private async Task SendUpdateAsync(PlanType operation, IEnumerable<PlanStep>? steps, CancellationToken cancellationToken)
    {
        TeamsActivityInput activity = new(TeamsActivityTypes.Typing)
        {
            Entities = [new PlanInfoEntity { PlanType = operation, Steps = steps?.ToList() }]
        };
        // Snapshot caller-owned steps and sources before handing the activity to the client.
        activity = JsonSerializer.Deserialize(activity.ToJson(), TeamsActivityInputJsonContext.Default.TeamsActivityInput)!;
        await SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SendActivityResponse?> SendActivityAsync(TeamsActivityInput activity, CancellationToken cancellationToken)
    {
        activity.ReplyToId = ReplyToId;
        SendActivityResponse? result = await _client.CreateActivityAsync(
            ConversationId, activity, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (activity.Type == TeamsActivityTypes.Typing)
            result = null;
        Sent?.Invoke(this, new PlanSentEventArgs(result));
        return result;
    }
}

/// <summary>A successful Plan send. Controls acknowledge progress but do not create chat messages.</summary>
[Experimental("ExperimentalTeamsPlan")]
public sealed class PlanSentEventArgs(SendActivityResponse? response) : EventArgs
{
    /// <summary>The final message response, or null for a typing acknowledgement.</summary>
    public SendActivityResponse? Response { get; } = response;
}
