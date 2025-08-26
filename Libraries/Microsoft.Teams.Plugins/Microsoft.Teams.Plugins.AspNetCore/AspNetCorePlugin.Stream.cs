// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Apps.Plugins;

using static Microsoft.Teams.Common.Extensions.TaskExtensions;

namespace Microsoft.Teams.Plugins.AspNetCore;

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

        public void Emit(MessageActivity activity)
        {
            if (_timeout != null)
            {
                _timeout.Dispose();
                _timeout = null;
            }

            _queue.Enqueue(activity);
            _timeout = new Timer(_ =>
            {
                _ = Flush();
            }, null, 500, Timeout.Infinite);
        }

        public void Emit(TypingActivity activity)
        {
            if (_timeout != null)
            {
                _timeout.Dispose();
                _timeout = null;
            }

            _queue.Enqueue(activity);
            _timeout = new Timer(_ =>
            {
                _ = Flush();
            }, null, 500, Timeout.Infinite);
        }

        public void Emit(string text)
        {
            Emit(new MessageActivity(text));
        }

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

        public async Task<MessageActivity?> Close()
        {
            if (_index == 1 && _queue.Count == 0 && _lock.CurrentCount > 0) return null;
            if (_result is not null) return _result;
            while (_id is null || _queue.Count > 0)
            {
                await Task.Delay(50);
            }

            if (_text == string.Empty && _attachments.Count == 0) // when only informative updates are present
            {
                _text = "Streaming closed with no content";
            }

            var activity = new MessageActivity(_text)
                .AddAttachment(_attachments.ToArray());

            activity.WithId(_id);
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

        protected async Task Flush()
        {
            if (_queue.Count == 0) return;

            await _lock.WaitAsync();

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