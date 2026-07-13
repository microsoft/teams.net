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
///
/// The writer is reusable: appending or sending an informative update after
/// <see cref="FinalizeResponseAsync"/> reopens the stream on the same instance and starts a new
/// streamed message. Finalizing is idempotent until the next append/informative update.
///
/// Streaming errors are surfaced per the Teams streaming error codes: cancellation and the
/// two-minute timeout are handled gracefully (a timed-out stream finalizes by updating the
/// original message in place), while <see cref="StreamNotAllowedException"/> and other
/// <see cref="TerminalStreamException"/> errors propagate to the caller.
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
    private bool _timedOut;
    private readonly System.Text.StringBuilder _accumulated = new();
    private DateTime _lastChunkSent = DateTime.MinValue;

    /// <summary>
    /// Whether the stream has been cancelled, for example when the user pressed the Stop button.
    /// </summary>
    public bool Cancelled => _cancelled;

    /// <summary>
    /// Whether streaming exceeded the two-minute limit. When true, the final message is
    /// sent by updating the original streamed message in place rather than as a streamed chunk.
    /// </summary>
    public bool TimedOut => _timedOut;

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
        if (_cancelled)
            return;

        // Sending after finalize reopens the stream on the same instance, starting a new streamed message.
        if (_finalized)
        {
            _logger.LogDebug("Reopening stream after finalize for a new informative update.");
            ResetForNextStream();
        }

        if (_lastChunkSent > DateTime.MinValue)
            throw new InvalidOperationException("Cannot send an informative update after streaming has started.");

        _sequence++;
        _logger.LogDebug("Sending informative streaming update (sequence {Sequence}).", _sequence);
        SendActivityResponse? response = await TrySendChunkAsync(BuildActivity(text, StreamTypes.Informative), cancellationToken).ConfigureAwait(false);
        _streamId ??= response?.Id;
        _logger.LogDebug("Stream started with streamId '{StreamId}'.", _streamId);
    }

    /// <summary>
    /// Appends <paramref name="chunk"/> to the accumulated text and sends the
    /// full accumulated text as an intermediate streaming update (streamType = "streaming").
    /// </summary>
    /// <remarks>
    /// Appending after <see cref="FinalizeResponseAsync"/> reopens the stream on the same
    /// instance, starting a new streamed message.
    /// </remarks>
    public async Task AppendResponseAsync(string chunk, CancellationToken cancellationToken = default)
    {
        if (_cancelled)
            return;

        // Appending after finalize reopens the stream on the same instance, starting a new streamed message.
        if (_finalized)
        {
            _logger.LogDebug("Reopening stream after finalize for a new streamed message.");
            ResetForNextStream();
        }

        _accumulated.Append(chunk);

        // Once the stream has timed out, stop sending chunks; the accumulated text is sent
        // by FinalizeResponseAsync, which updates the original message in place.
        if (_timedOut)
            return;

        if (DateTime.UtcNow - _lastChunkSent < _minChunkInterval)
        {
            _logger.LogTrace("Rate-limited: skipping intermediate send (interval {Interval}ms).", _minChunkInterval.TotalMilliseconds);
            return;
        }

        _sequence++;
        _logger.LogDebug("Sending streaming chunk (sequence {Sequence}, accumulated {Length} chars).", _sequence, _accumulated.Length);
        SendActivityResponse? response = await TrySendChunkAsync(BuildActivity(_accumulated.ToString(), StreamTypes.Streaming), cancellationToken).ConfigureAwait(false);
        _streamId ??= response?.Id;

        if (_cancelled || _timedOut)
            return;

        _lastChunkSent = DateTime.UtcNow;
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
    /// <remarks>
    /// Finalizing is idempotent until the next append or informative update reopens the stream.
    /// If streaming exceeded the two-minute limit, the final content updates the original
    /// streamed message in place instead of sending a new streamed chunk.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the final activity has neither text nor attachments.
    /// </exception>
    public async Task FinalizeResponseAsync(MessageActivity? final = null, CancellationToken cancellationToken = default)
    {
        // Finalizing is idempotent until the next append/informative reopens the stream.
        if (_finalized)
            return;

        if (_cancelled)
            return;

        final ??= new MessageActivity();
        final.Text ??= _accumulated.ToString();

        if (string.IsNullOrEmpty(final.Text) && (final.Attachments == null || final.Attachments.Count == 0))
            throw new InvalidOperationException(
                "Cannot finalize with no content. Stream text via AppendResponseAsync, or provide attachments on the final MessageActivity.");

        // Streaming already tripped the two-minute limit; update the original message in place.
        if (_timedOut)
        {
            await SendFinalInPlaceAsync(final, cancellationToken).ConfigureAwait(false);
            _finalized = true;
            _logger.LogDebug("Stream finalized in place after timeout (streamId '{StreamId}').", _streamId);
            return;
        }

        StreamInfoEntity streamInfo = new() { StreamType = StreamTypes.Final };
        if (_streamId != null) streamInfo.StreamId = _streamId;

        MessageActivity activity = new MessageActivityBuilder(final)
            .AddEntity(streamInfo)
            .Build();

        _logger.LogDebug("Finalizing stream (streamId '{StreamId}', {Length} chars, {Sequences} sequences).",
            _streamId, final.Text?.Length ?? 0, _sequence);

        try
        {
            await _client.SendActivityAsync(_conversationId, activity, _reference.ServiceUrl!, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            try
            {
                ThrowIfStreamError(ex);
                throw;
            }
            catch (StreamTimedOutException)
            {
                // The final streamed send tripped the two-minute limit. Update the original
                // message in place with the buffered content: reuse the id and drop the
                // stream markers so this routes to an update, not a new streamed chunk.
                await SendFinalInPlaceAsync(final, cancellationToken).ConfigureAwait(false);
            }
            catch (StreamCancelledException)
            {
                // Cancelled during the final send; nothing more to send.
                _logger.LogDebug("Stream cancelled during finalize; no final message sent (streamId '{StreamId}').", _streamId);
            }
        }

        _finalized = true;
        _logger.LogDebug("Stream finalized (streamId '{StreamId}').", _streamId);
    }

    /// <summary>
    /// Sends a streaming chunk, swallowing soft-stop conditions (cancellation and the
    /// two-minute timeout) by flagging state and returning. Terminal streaming errors
    /// (<see cref="StreamNotAllowedException"/>, <see cref="TerminalStreamException"/>) and
    /// non-streaming errors propagate.
    /// </summary>
    private async Task<SendActivityResponse?> TrySendChunkAsync(TeamsActivity activity, CancellationToken cancellationToken)
    {
        try
        {
            return await _client.SendActivityAsync(_conversationId, activity, _reference.ServiceUrl!, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            try
            {
                ThrowIfStreamError(ex);
            }
            catch (StreamCancelledException)
            {
                _logger.LogDebug("Chunk send stopped: stream cancelled (streamId '{StreamId}').", _streamId);
                return null; // soft stop: FinalizeResponseAsync returns without sending.
            }
            catch (StreamTimedOutException)
            {
                _logger.LogDebug("Chunk send stopped: stream timed out (streamId '{StreamId}').", _streamId);
                return null; // soft stop: FinalizeResponseAsync updates the message in place.
            }

            // Non-streaming error: rethrow the original exception.
            throw;
        }
    }

    /// <summary>
    /// Maps a failed streaming send to a typed streaming exception. Cancellation and the
    /// two-minute timeout also flag internal state so callers can stop the send loop.
    /// Non-streaming failures fall through so the caller can rethrow the original exception.
    /// See https://learn.microsoft.com/en-us/microsoftteams/platform/bots/streaming-ux?tabs=csharp#error-codes.
    /// </summary>
    private void ThrowIfStreamError(HttpRequestException ex)
    {
        string message = ex.Message ?? string.Empty;

        if (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            if (message.Contains("exceeded streaming time", StringComparison.OrdinalIgnoreCase))
            {
                _timedOut = true;
                _logger.LogWarning("The bot failed to complete streaming within the two-minute limit (streamId '{StreamId}').", _streamId);
                throw new StreamTimedOutException(message, ex);
            }

            if (message.Contains("cancel", StringComparison.OrdinalIgnoreCase))
            {
                _cancelled = true;
                _logger.LogWarning("The streaming was stopped by the user (streamId '{StreamId}').", _streamId);
                throw new StreamCancelledException(message, ex);
            }

            if (message.Contains("not allowed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("The streaming API isn't allowed for the user or bot (streamId '{StreamId}').", _streamId);
                throw new StreamNotAllowedException(message, ex);
            }

            _logger.LogWarning("Teams returned a streaming error (streamId '{StreamId}'): {Message}", _streamId, message);
            throw new TerminalStreamException(message, ex);
        }

        // Preserve the historical treatment of Gone/NoContent as a user cancellation.
        if (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NoContent)
        {
            _cancelled = true;
            _logger.LogWarning("The streaming was stopped by the user (streamId '{StreamId}').", _streamId);
            throw new StreamCancelledException(message, ex);
        }
    }

    /// <summary>
    /// Sends the buffered content as a plain final message by updating the original streamed
    /// message in place. Drops the <c>streaminfo</c> entity and stream channel data so the
    /// send routes through the update path (reusing the streamId) instead of creating a
    /// duplicate streamed chunk.
    /// </summary>
    private async Task SendFinalInPlaceAsync(MessageActivity final, CancellationToken cancellationToken)
    {
        // Drop streaming markers so Teams treats this as a normal message edit.
        final.Entities?.RemoveAll(e => e is StreamInfoEntity);
        if (final.ChannelData?.Properties is { } props)
        {
            props.Remove("streamId");
            props.Remove("streamType");
            props.Remove("streamSequence");
        }

        TeamsActivity activity = new MessageActivityBuilder(final)
            .Build();

        if (_streamId != null)
        {
            _logger.LogDebug("Updating original streamed message in place after timeout (streamId '{StreamId}').", _streamId);
            await _client.UpdateActivityAsync(_conversationId, _streamId, activity, _reference.ServiceUrl!, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // No streamed message exists yet; send the buffered content as a normal message.
            await _client.SendActivityAsync(_conversationId, activity, _reference.ServiceUrl!, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Prepares the writer to start a new stream cycle after finalize. The cancelled flag is
    /// sticky: once a stream is cancelled it stays cancelled across reuse.
    /// </summary>
    private void ResetForNextStream()
    {
        _streamId = null;
        _sequence = 0;
        _finalized = false;
        _timedOut = false;
        _accumulated.Clear();
        _lastChunkSent = DateTime.MinValue;
    }

    private StreamingActivity BuildActivity(string text, string streamType)
    {
        return StreamingActivity.CreateBuilder()
            .WithText(text)
            .WithStreamInfo(streamType, _streamId, _sequence)
            .Build();
    }
}
