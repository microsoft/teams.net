using System.Collections.Concurrent;
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Extensions;

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

        public void Emit(MessageActivity activity)
        {
            _queue.Enqueue(activity);
            Task.Run(Flush);
        }

        public void Emit(TypingActivity activity)
        {
            _queue.Enqueue(activity);
            Task.Run(Flush);
        }

        public void Emit(string text)
        {
            Emit(new MessageActivity(text));
        }

        public void Update(string text)
        {
            Emit(new TypingActivity(text) {
                ChannelData = new() {
                    StreamType = StreamType.Informative
                }
            });
        }

        public async Task<MessageActivity?> Close()
        {
            if (_index == 1 && _queue.Count == 0) return null;
            if (_result is not null) return _result;
            while (_id is null || _queue.Count > 0)
            {
                await Task.Delay(50);
            }

            var activity = new MessageActivity(_text)
                .AddAttachment(_attachments.ToArray());

            activity.WithId(_id);
            activity.WithData(_channelData);
            activity.AddEntity(_entities.ToArray());
            activity.AddStreamFinal();

            var res = await Send(activity).Retry();
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

                    if (activity is TypingActivity typing  && typing.ChannelData?.StreamType == StreamType.Informative && _text == string.Empty) {
                        // If `_text` is not empty then it's possible that streaming has started.
                        // And so informative updates cannot be sent.
                        informativeUpdates.Enqueue(typing);
                    }

                    i++;
                    _count++;
                }

                if (i == 0) return;

                // Send informative updates
                if (informativeUpdates.Count > 0) {
                    while (informativeUpdates.TryDequeue(out var typing)) {
                        await SendActivity(typing);
                    }
                }

                // Send text chunk
                var toSend = new TypingActivity(_text);
                await SendActivity(toSend);

                async Task SendActivity(TypingActivity toSend) {
                    if (_id is not null)
                    {
                        toSend.WithId(_id);
                    }

                    toSend.AddStreamUpdate(_index);
                    var res = await Send(toSend).Retry(delay: 10).ConfigureAwait(false);
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