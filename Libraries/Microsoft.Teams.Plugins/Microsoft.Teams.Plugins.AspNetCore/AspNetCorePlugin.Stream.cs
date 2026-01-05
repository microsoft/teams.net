// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Apps.Plugins;

using static Microsoft.Teams.Common.Extensions.TaskExtensions;

namespace Microsoft.Teams.Plugins.AspNetCore;

/// <summary>
/// Streaming implementation for Microsoft Teams activities.
///
/// Queues message and typing activities and flushes them in chunks
/// to avoid rate limits and preserve message order.
///
/// Flow:
/// 1. <see cref="Emit(IActivity)"/> adds activities to the queue.
/// 2. <see cref="Flush"/> processes up to 10 queued items under a lock.
/// 3. Informative typing updates are sent immediately if no message started.
/// 4. Message text are combined into a typing chunk.
/// 5. Another flush is scheduled if more items remain.
/// 6. <see cref="Close"/> waits for the queue to empty and sends the final message.
/// </summary>
public partial class AspNetCorePlugin
{
    public class Stream : IStreamer
    {
        public bool Closed => _closedAt is not null;
        public int Count => _count;
        public int Sequence => _index;

        public required Func<IActivity, Task<IActivity>> Send { get; set; }
        public event IStreamer.OnChunkHandler OnChunk = (_) => { };

        protected int _index = 1;
        protected string? _id;
        protected string _text = string.Empty;
        protected ChannelData _channelData = new();
        protected List<Attachment> _attachments = [];
        protected List<IEntity> _entities = [];
        protected ConcurrentQueue<IActivity> _queue = [];

        private DateTime? _closedAt;
        private int _count = 0;
        private MessageActivity? _result;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private Timer? _timeout;
        private const int _timeoutMs = 5000;

        /// <summary>
        /// Enqueues a message activity for streaming.
        /// </summary>
        public void Emit(MessageActivity activity)
        {
            _queue.Enqueue(activity);
            if (_timeout == null)
            {
                _ = Flush();
            }
        }

        /// <summary>
        /// Enqueues a typing activity for streaming.
        /// </summary>
        public void Emit(TypingActivity activity)
        {
            _queue.Enqueue(activity);
            if (_timeout == null)
            {
                _ = Flush();
            }
        }

        /// <summary>
        /// Emits plain text as a message activity.
        /// </summary>
        public void Emit(string text)
        {
            Emit(new MessageActivity(text));
        }

        /// <summary>
        /// Sends an informative typing update (e.g., "Thinking...").
        /// </summary>
        public void Update(string text)
        {
            Emit(new TypingActivity(text)
            {
                ChannelData = new()
                {
                    StreamType = StreamType.Informative
                }
            });
        }

        public async Task<bool> WaitForIdAndQueueAsync()
        {
            var start = DateTime.UtcNow;

            while (_id == null || _queue.Count > 0)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > _timeoutMs)
                {
                    return false; // timed out
                }

                await Task.Delay(50);
            }

            return true; // success
        }

        /// <summary>
        /// Closes the stream after all queued activities have been sent.
        /// Returns the final message activity.
        /// </summary>
        public async Task<MessageActivity?> Close()
        {
            if (_index == 1 && _queue.Count == 0 && _lock.CurrentCount > 0) return null;

            if (_result is not null) return _result;
            bool ready = await WaitForIdAndQueueAsync();
            if (!ready)
            {
                return null; // timed out waiting for ID and queue to empty
            }

            if (_text == string.Empty && _attachments.Count == 0) // when only informative updates are present
            {
                return null;
            }

            var activity = new MessageActivity(_text)
                .AddAttachment(_attachments.ToArray());

            if (_id is not null)
            {
                activity.WithId(_id);
            }
            activity.WithData(_channelData);
            activity.AddEntity(_entities.ToArray());
            activity.AddStreamFinal();

            var res = await Retry(() => Send(activity)).ConfigureAwait(false);
            OnChunk(res);

            _result = activity;
            _closedAt = DateTime.Now;
            _index = 1;
            _id = null;
            _text = string.Empty;
            _attachments = [];
            _entities = [];
            _channelData = new();

            return (MessageActivity)res;
        }

        /// <summary>
        /// Flushes up to 10 queued activities.
        /// Combines message chunks and sends informative updates.
        /// Reschedules itself if more items remain.
        /// </summary>
        protected async Task Flush()
        {
            if (_queue.Count == 0) return;


            if (!await _lock.WaitAsync(0))
            {
                return; // another flush is running, exit
            }

            try
            {
                if (_timeout != null)
                {
                    _timeout.Dispose();
                    _timeout = null;
                }

                var i = 0;

                Queue<TypingActivity> informativeUpdates = new();

                while (i <= 10 && _queue.TryDequeue(out var activity))
                {
                    if (activity is MessageActivity message)
                    {
                        _text += message.Text;
                        _attachments.AddRange(message.Attachments ?? []);
                        _entities.AddRange(message.Entities ?? []);
                    }

                    if (activity.ChannelData is not null)
                    {
                        _channelData = _channelData.Merge(activity.ChannelData);
                    }

                    if (activity is TypingActivity typing && typing.ChannelData?.StreamType == StreamType.Informative && _text == string.Empty)
                    {
                        // If `_text` is not empty then it's possible that streaming has started.
                        // And so informative updates cannot be sent.
                        informativeUpdates.Enqueue(typing);
                    }

                    i++;
                    _count++;
                }

                if (i == 0) return;

                // Send informative updates
                if (informativeUpdates.Count > 0)
                {
                    while (informativeUpdates.TryDequeue(out var typing))
                    {
                        await SendActivity(typing);
                    }
                }

                // Send text chunk
                if (_text != string.Empty)
                {
                    var toSend = new TypingActivity(_text);
                    await SendActivity(toSend);
                }

                if (_queue.Count > 0)
                {
                    _timeout = new Timer(_ =>
                    {
                        _ = Flush();
                    }, null, 500, Timeout.Infinite);
                }

                async Task SendActivity(TypingActivity toSend)
                {
                    if (_id is not null)
                    {
                        toSend.WithId(_id);
                    }
                    
                    toSend.AddStreamUpdate(_index);

                    var res = await Retry(() => Send(toSend)).ConfigureAwait(false);
                    OnChunk(res);
                    _id ??= res.Id;
                    _index++;
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}