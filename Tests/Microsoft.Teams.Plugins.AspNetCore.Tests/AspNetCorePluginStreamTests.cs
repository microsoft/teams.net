// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Plugins.AspNetCore.Tests;

public class AspNetCorePluginStreamTests
{
    [Fact]
    public async Task Stream_EmitMessage_FlushesAfter500ms()
    {
        var sendCallCount = 0;
        var sendTimes = new List<DateTime>();
        var stream = new AspNetCorePlugin.Stream
        {
            Send = activity =>
            {
                sendCallCount++;
                sendTimes.Add(DateTime.Now);
                activity.Id = $"test-id-{sendCallCount}";
                return Task.FromResult(activity);
            }
        };

        var startTime = DateTime.Now;

        stream.Emit("Test message");
        await Task.Delay(600); // Wait longer than 500ms timeout

        Assert.True(sendCallCount > 0, "Should have sent at least one message");
        Assert.True(sendTimes.Any(t => t >= startTime.AddMilliseconds(450)),
            "Should have waited approximately 500ms before sending");
    }

    [Fact]
    public async Task Stream_MultipleEmits_RestartsTimer()
    {
        var sendCallCount = 0;
        var stream = new AspNetCorePlugin.Stream
        {
            Send = activity =>
            {
                sendCallCount++;
                activity.Id = $"test-id-{sendCallCount}";
                return Task.FromResult(activity);
            }
        };

        stream.Emit("First message");
        await Task.Delay(300); // Wait less than 500ms

        stream.Emit("Second message"); // This should reset the timer
        await Task.Delay(300); // Still less than 500ms from second emit

        Assert.Equal(0, sendCallCount); // Should not have sent yet

        await Task.Delay(300); // Now over 500ms from second emit

        Assert.True(sendCallCount > 0, "Should have sent messages after timer expired");
    }

    [Fact]
    public async Task Stream_SendTimeout_HandledGracefully()
    {
        var callCount = 0;
        var stream = new AspNetCorePlugin.Stream
        {
            Send = activity =>
            {
                callCount++;
                if (callCount == 1) // Fail first attempt
                {
                    throw new TimeoutException("Operation timed out");
                }

                // Succeed on second attempt
                activity.Id = $"success-after-timeout-{callCount}";
                return Task.FromResult(activity);
            }
        };

        stream.Emit("Test message with timeout");
        await Task.Delay(600); // Wait for flush and retries

        var result = await stream.Close();

        Assert.True(callCount > 1, "Should have retried after timeout");
        Assert.NotNull(result);
        Assert.Contains("Test message with timeout", result.Text);
    }

    [Fact(Skip = "Flaky in CI — skip pending investigation")]
    public async Task Stream_UpdateStatus_SendsTypingActivity()
    {
        var sentActivities = new List<IActivity>();
        var stream = new AspNetCorePlugin.Stream
        {
            Send = activity =>
            {
                sentActivities.Add(activity);
                return Task.FromResult(activity);
            }
        };

        stream.Update("Thinking...");
        await Task.Delay(1000); // Wait for the flush task to complete

        Assert.True(stream.Count > 0, "Should have processed the update");
        Assert.Equal(2, stream.Sequence); // Should increment sequence after sending

        Assert.True(sentActivities.Count > 0, "Should have sent at least one activity");
        var sentActivity = sentActivities.First();
        Assert.IsType<TypingActivity>(sentActivity);
        Assert.Equal("Thinking...", ((TypingActivity)sentActivity).Text);
        Assert.Equal(StreamType.Informative, ((TypingActivity)sentActivity).ChannelData?.StreamType);
    }

    [Fact]
    public async Task Stream_Close_FinalMessageHasStreamTypeFinal_AfterInformativeUpdate()
    {
        var sendCallCount = 0;
        var stream = new AspNetCorePlugin.Stream
        {
            Send = activity =>
            {
                sendCallCount++;
                activity.Id = $"id-{sendCallCount}";
                return Task.FromResult(activity);
            }
        };

        // Update + Emit both queue activities; Close() waits for the flush to drain the
        // queue and complete, so no fixed sleeps are needed.
        stream.Update("Thinking...");
        stream.Emit("Done");

        var result = await stream.Close();

        Assert.NotNull(result);
        // Final message must have StreamType.Final, not the accumulated Informative
        // from the prior typing update.
        Assert.Equal(StreamType.Final, result.ChannelData?.StreamType);

        // The streaminfo entity on the final message should also be Final.
        var streamInfo = result.Entities?.OfType<StreamInfoEntity>().Single();
        Assert.NotNull(streamInfo);
        Assert.Equal(StreamType.Final, streamInfo.StreamType);
    }

    [Fact]
    public async Task Stream_Close_WaitsForInFlightFlushToComplete()
    {
        var sendCallCount = 0;
        var firstSendCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondSendStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondSendRelease = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stream = new AspNetCorePlugin.Stream
        {
            Send = async activity =>
            {
                sendCallCount++;
                var thisCall = sendCallCount;
                if (thisCall == 2)
                {
                    secondSendStarted.TrySetResult(true);
                    await secondSendRelease.Task;
                }
                activity.Id = $"id-{thisCall}";
                if (thisCall == 1) firstSendCompleted.TrySetResult(true);
                return activity;
            }
        };

        // First flush: emit and wait for the send to actually complete (deterministic
        // signal from the Send delegate). _id is assigned by the SendActivity helper after
        // the await — yielding once lets that post-await code run before we proceed.
        stream.Emit("chunk 1");
        await firstSendCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Yield();
        Assert.Equal(1, sendCallCount);

        // Second flush: Send blocks → queue drained, _id set, _lock held.
        stream.Emit("chunk 2");
        await secondSendStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var closeTask = stream.Close();

        // With the race-fix, Close() must not progress past its wait loop while the
        // flush is mid-await. Pre-fix, closeTask would race ahead and call Send for the
        // final activity (sendCallCount → 3) before we release the second flush.
        // We yield several times rather than sleep — Close() polls every 50ms and we
        // want to give it ample chance to make progress if the bug is present.
        for (var i = 0; i < 10; i++) await Task.Yield();
        await Task.Delay(100);
        Assert.False(closeTask.IsCompleted);
        Assert.Equal(2, sendCallCount);

        // Releasing the second flush lets the lock drop, and Close() then sends the final.
        secondSendRelease.SetResult(true);

        var result = await closeTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotNull(result);
        Assert.Equal(3, sendCallCount);
    }
}