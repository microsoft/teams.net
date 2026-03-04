// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Manages the send loop for Teams streaming messages.
/// Callers append raw deltas; the writer accumulates them and sends the full
/// text so far on each update. Every chunk — informative, intermediate, and
/// final — is sent as a new POST with a shared <c>streamId</c>
/// so Teams renders them as a single progressively-updating bubble.
/// </summary>
/// <remarks>
/// Typical usage with an informative placeholder:
/// <code>
///     var writer = ctx.GetStreamingWriter();
///     await writer.SendInformativeAsync("Thinking…");
///     await writer.AppendAsync(" Hello");
///     await writer.AppendAsync(", world");
///     await writer.FinalizeAsync();            // sends accumulated " Hello, world"
/// </code>
///
/// Or without a placeholder:
/// <code>
///     var writer = ctx.GetStreamingWriter();
///     await writer.AppendAsync("Hello");
///     await writer.AppendAsync(", world");
///     await writer.FinalizeAsync();
/// </code>
/// </remarks>
public sealed class ActivityStreamingWriter
{
    private readonly ConversationClient _client;
    private readonly TeamsActivity _reference;
    // Assigned from the server's 201 response after the first send; null until then.
    private string? _streamId;
    private int _sequence;
    private bool _finalized;
    private string _accumulated = string.Empty;

    internal ActivityStreamingWriter(ConversationClient client, TeamsActivity reference)
    {
        _client = client;
        _reference = reference;
    }

    /// <summary>
    /// Sends an informative placeholder (streamType = "informative").
    /// Optional — if omitted the first <see cref="AppendAsync"/> call begins the stream.
    /// </summary>
    public async Task SendInformativeAsync(string text, CancellationToken cancellationToken = default)
    {
        _sequence = 1;
        var response = await _client.SendActivityAsync(BuildActivity(text, StreamType.Informative), cancellationToken: cancellationToken).ConfigureAwait(false);
        _streamId = response.Id;
    }

    /// <summary>
    /// Appends <paramref name="chunk"/> to the accumulated text and sends the
    /// full accumulated text as an intermediate streaming update (streamType = "streaming").
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="FinalizeAsync"/> has already been called.</exception>
    public async Task AppendAsync(string chunk, CancellationToken cancellationToken = default)
    {
        if (_finalized)
            throw new InvalidOperationException("Cannot append after FinalizeAsync has been called.");

        _accumulated += chunk;
        _sequence++;
        var response = await _client.SendActivityAsync(BuildActivity(_accumulated, StreamType.Streaming), cancellationToken: cancellationToken).ConfigureAwait(false);
        _streamId ??= response.Id;
    }

    /// <summary>
    /// Sends the accumulated text as the final update (streamType = "final") and marks the stream complete.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="FinalizeAsync"/> has already been called, or if no content has been accumulated via <see cref="AppendAsync"/>.</exception>
    public async Task FinalizeAsync(CancellationToken cancellationToken = default)
    {
        if (_finalized)
            throw new InvalidOperationException("Cannot finalize after FinalizeAsync has already been called.");

        if (string.IsNullOrEmpty(_accumulated))
            throw new InvalidOperationException("Cannot finalize with no content. Call AppendAsync at least once before FinalizeAsync.");

        await _client.SendActivityAsync(BuildActivity(_accumulated, StreamType.Final), cancellationToken: cancellationToken).ConfigureAwait(false);

        _finalized = true;
    }

    private TeamsActivity BuildActivity(string text, string streamType)
    {
        bool isFinal = streamType == StreamType.Final;

        TeamsActivity activity = isFinal
            ? new MessageActivity(text)
            : new TeamsActivityBuilder().WithType(TeamsActivityType.Typing).Build();

        activity.ServiceUrl = _reference.ServiceUrl;
        activity.ChannelId = _reference.ChannelId;
        activity.Conversation = _reference.Conversation;
        activity.From = _reference.Recipient;
        activity.Recipient = _reference.From;

        if (!isFinal)
            activity.Properties["text"] = text;

        StreamInfoEntity streamInfo = new() { StreamType = streamType };

        // streamId is omitted on the very first send; the server assigns it and returns it as the activityId.
        if (_streamId != null)
            streamInfo.StreamId = _streamId;

        // streamSequence must not be set on the final message.
        if (!isFinal)
            streamInfo.StreamSequence = _sequence;

        activity.Entities ??= [];
        activity.Entities.Add(streamInfo);
        activity.Rebase();
        return activity;
    }
}
