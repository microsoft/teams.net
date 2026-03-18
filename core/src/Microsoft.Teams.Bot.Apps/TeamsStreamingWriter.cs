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
/// Typical usage:
/// <code>
///     var writer = context.GetStreamingWriter();
///     await writer.SendInformativeUpdateAsync("Thinking…"); //optional placeholder while the bot thinks
///     await writer.AppendResponseAsync(" Hello");
///     await writer.AppendResponseAsync(", world");
///     await writer.FinalizeResponseAsync();            // sends accumulated " Hello, world"
/// </code>
///
/// Entities are only sent with the final message activity.
/// Pass them directly to <see cref="FinalizeResponseAsync"/>:
/// <code>
///     await writer.FinalizeResponseAsync(
///         entities: [new CitationEntity(...)]);
/// </code>
/// </remarks>
public sealed class TeamsStreamingWriter
{
    // Teams streaming API enforces a rate limit; send intermediate updates at most once per interval.
    private static readonly TimeSpan _minChunkInterval = TimeSpan.FromMilliseconds(500);

    private readonly ConversationClient _client;
    private readonly TeamsActivity _reference;
    // Assigned from the server's 201 response after the first send; null until then.
    private string? _streamId;
    private int _sequence;
    private bool _finalized;
    private bool _cancelled;
    private string _accumulated = string.Empty;
    private DateTime _lastChunkSent = DateTime.MinValue;

    internal TeamsStreamingWriter(ConversationClient client, TeamsActivity reference)
    {
        _client = client;
        _reference = reference;
    }

    /// <summary>
    /// Sends an informative placeholder (streamType = "informative").
    /// Optional — if omitted the first <see cref="AppendResponseAsync"/> call begins the stream.
    /// </summary>
    public async Task SendInformativeUpdateAsync(string text, CancellationToken cancellationToken = default)
    {
        _sequence++;
        var response = await _client.SendActivityAsync(BuildActivity(text, StreamType.Informative), cancellationToken: cancellationToken).ConfigureAwait(false);
        _streamId ??= response.Id;
    }

    /// <summary>
    /// Appends <paramref name="chunk"/> to the accumulated text and sends the
    /// full accumulated text as an intermediate streaming update (streamType = "streaming").
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="FinalizeResponseAsync"/> has already been called.</exception>
    public async Task AppendResponseAsync(string chunk, CancellationToken cancellationToken = default)
    {
        if (_finalized)
            throw new InvalidOperationException("Cannot append after FinalizeResponseAsync has been called.");

        if (_cancelled)
            return;

        _accumulated += chunk;

        if (DateTime.UtcNow - _lastChunkSent < _minChunkInterval)
            return;

        _sequence++;
        try
        {
            var response = await _client.SendActivityAsync(BuildActivity(_accumulated, StreamType.Streaming), cancellationToken: cancellationToken).ConfigureAwait(false);
            _streamId ??= response.Id;
            _lastChunkSent = DateTime.UtcNow;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Content stream was cancelled by user", StringComparison.OrdinalIgnoreCase))
        {
            _cancelled = true;
        }
    }

    /// <summary>
    /// Sends the accumulated text as the final update (streamType = "final") and marks the stream complete.
    /// </summary>
    /// <param name="attachments">Optional attachments to include in the final message activity.</param>
    /// <param name="entities">Optional entities (e.g. citations, mentions) to include in the final message activity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="FinalizeResponseAsync"/> has already been called, or if no content has been accumulated via <see cref="AppendResponseAsync"/>.</exception>
    public async Task FinalizeResponseAsync(IList<TeamsAttachment>? attachments = null, IList<Entity>? entities = null, CancellationToken cancellationToken = default)
    {
        if (_finalized)
            throw new InvalidOperationException("Cannot finalize after FinalizeResponseAsync has already been called.");

        if (_cancelled)
            return;

        if (string.IsNullOrEmpty(_accumulated))
            throw new InvalidOperationException("Cannot finalize with no content. Call AppendResponseAsync at least once before FinalizeResponseAsync.");

        await _client.SendActivityAsync(BuildActivity(_accumulated, StreamType.Final, attachments, entities), cancellationToken: cancellationToken).ConfigureAwait(false);

        _finalized = true;
    }

    private TeamsActivity BuildActivity(string text, string streamType, IList<TeamsAttachment>? attachments = null, IList<Entity>? entities = null)
    {
        bool isFinal = streamType == StreamType.Final;

        TeamsActivity baseActivity = isFinal
            ? new MessageActivity(text)
            : new StreamingActivity(text);

        StreamInfoEntity streamInfo = new() { StreamType = streamType };

        // streamId is omitted on the very first send; the server assigns it and returns it as the activityId.
        if (_streamId != null)
            streamInfo.StreamId = _streamId;

        // streamSequence must not be set on the final message.
        if (!isFinal)
            streamInfo.StreamSequence = _sequence;

        TeamsActivityBuilder builder = new TeamsActivityBuilder(baseActivity)
            .WithConversationReference(_reference)
            .AddEntity(streamInfo);

        if (isFinal)
        {
            if (entities != null)
                foreach (Entity entity in entities)
                    builder.AddEntity(entity);

            if (attachments?.Count > 0)
                builder.WithAttachments(attachments);
        }

        return builder.Build();
    }
}
