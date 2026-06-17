// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;

namespace Microsoft.Teams.Apps;

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
///     var writer = TeamsStreamingWriter.CreateFromContext(context);
///     await writer.SendInformativeUpdateAsync("Thinking…"); //optional placeholder while the bot thinks
///     await writer.AppendResponseAsync(" Hello");
///     await writer.AppendResponseAsync(", world");
///     await writer.FinalizeResponseAsync();            // sends accumulated " Hello, world"
/// </code>
///
/// To attach entities, attachments, suggested actions, or feedback to the final message,
/// build a <see cref="MessageActivity"/> and pass it in. If its <c>Text</c> is null the
/// writer fills in the accumulated streamed text.
/// <code>
///     MessageActivity final = new MessageActivity().AddAttachment(card);
///     final.AddEntity(citation);
///     final.AddFeedback(FeedbackTypes.Default);
///     await writer.FinalizeResponseAsync(final);
/// </code>
/// </remarks>
public sealed class TeamsStreamingWriter
{
    // Teams streaming API enforces a rate limit; send intermediate updates at most once per interval.
    private static readonly TimeSpan _minChunkInterval = TimeSpan.FromMilliseconds(500);

    private readonly ConversationClient _client;
    private readonly TeamsActivity _reference;
    private readonly string _conversationId;
    private readonly ILogger _logger;
    // Assigned from the server's 201 response after the first send; null until then.
    private string? _streamId;
    private int _sequence;
    private bool _finalized;
    private bool _cancelled;
    private readonly System.Text.StringBuilder _accumulated = new();
    private DateTime _lastChunkSent = DateTime.MinValue;

    internal TeamsStreamingWriter(ConversationClient client, TeamsActivity reference, ILogger? logger = null)
    {
        _client = client;
        _reference = reference;
        _conversationId = reference.Conversation?.Id ?? throw new ArgumentException("Activity must have a Conversation with an Id.", nameof(reference));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Creates a <see cref="TeamsStreamingWriter"/> bound to the given context.
    /// </summary>
    public static TeamsStreamingWriter CreateFromContext<TActivity>(Context<TActivity> context) where TActivity : TeamsActivity
    {
        ArgumentNullException.ThrowIfNull(context);
        return new TeamsStreamingWriter(context.TeamsBotApplication.ConversationClient, context.Activity);
    }

    /// <summary>
    /// Sends an informative placeholder (streamType = "informative").
    /// Optional — if omitted the first <see cref="AppendResponseAsync"/> call begins the stream.
    /// </summary>
    public async Task SendInformativeUpdateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_lastChunkSent > DateTime.MinValue)
            throw new InvalidOperationException("Cannot send an informative update after streaming has started.");

        _sequence++;
        _logger.LogDebug("Sending informative streaming update (sequence {Sequence}).", _sequence);
        SendActivityResponse? response = await _client.SendActivityAsync(BuildActivity(text, StreamTypes.Informative), cancellationToken: cancellationToken).ConfigureAwait(false);
        _streamId ??= response?.Id;
        _logger.LogDebug("Stream started with streamId '{StreamId}'.", _streamId);
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

        _accumulated.Append(chunk);

        if (DateTime.UtcNow - _lastChunkSent < _minChunkInterval)
        {
            _logger.LogTrace("Rate-limited: skipping intermediate send (interval {Interval}ms).", _minChunkInterval.TotalMilliseconds);
            return;
        }

        _sequence++;
        try
        {
            _logger.LogDebug("Sending streaming chunk (sequence {Sequence}, accumulated {Length} chars).", _sequence, _accumulated.Length);
            SendActivityResponse? response = await _client.SendActivityAsync(BuildActivity(_accumulated.ToString(), StreamTypes.Streaming), cancellationToken: cancellationToken).ConfigureAwait(false);
            _streamId ??= response?.Id;
            _lastChunkSent = DateTime.UtcNow;
        }
        catch (HttpRequestException ex) when (
            ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NoContent
            || ex.Message.Contains("Content stream was cancelled", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Stream cancelled by user (streamId '{StreamId}').", _streamId);
            _cancelled = true;
        }
    }

    /// <summary>
    /// Sends the final streaming activity and marks the stream complete.
    /// </summary>
    /// <param name="final">
    /// The final message activity. If <c>null</c>, a plain <see cref="MessageActivity"/> is built
    /// from the accumulated streamed text. If non-null, the caller-supplied activity is used as-is;
    /// when its <see cref="MessageActivity.Text"/> is <c>null</c>, the accumulated text is filled in
    /// (pass <c>""</c> explicitly to send an attachment-only reply).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="FinalizeResponseAsync"/> has already been called, or if the final
    /// activity has neither text nor attachments.
    /// </exception>
    public async Task FinalizeResponseAsync(MessageActivity? final = null, CancellationToken cancellationToken = default)
    {
        if (_finalized)
            throw new InvalidOperationException("Cannot finalize after FinalizeResponseAsync has already been called.");

        if (_cancelled)
            return;

        final ??= new MessageActivity();
        final.Text ??= _accumulated.ToString();

        if (string.IsNullOrEmpty(final.Text) && (final.Attachments == null || final.Attachments.Count == 0))
            throw new InvalidOperationException(
                "Cannot finalize with no content. Stream text via AppendResponseAsync, or provide attachments on the final MessageActivity.");

        StreamInfoEntity streamInfo = new() { StreamType = StreamTypes.Final };
        if (_streamId != null) streamInfo.StreamId = _streamId;

        TeamsActivity activity = new TeamsActivityBuilder(final)
            .WithConversationReference(_reference)
            .AddEntity(streamInfo)
            .Build();

        _logger.LogDebug("Finalizing stream (streamId '{StreamId}', {Length} chars, {Sequences} sequences).",
            _streamId, final.Text?.Length ?? 0, _sequence);

        await _client.SendActivityAsync(activity, cancellationToken: cancellationToken).ConfigureAwait(false);

        _finalized = true;
        _logger.LogDebug("Stream finalized (streamId '{StreamId}').", _streamId);
    }

    private TeamsActivity BuildActivity(string text, string streamType)
    {
        StreamingActivity streaming = new(text);
        streaming.StreamInfo.StreamType = streamType;
        streaming.StreamInfo.StreamSequence = _sequence;
        if (_streamId != null)
            streaming.StreamInfo.StreamId = _streamId;

        return new TeamsActivityBuilder(streaming)
            .WithConversationReference(_reference)
            .Build();
    }
}
