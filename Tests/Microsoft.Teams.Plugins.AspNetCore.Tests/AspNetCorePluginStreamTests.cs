// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Plugins.AspNetCore.Tests;

public class AspNetCorePluginStreamTests
{
    [Fact]
    public async Task Stream_EmitMessage_FlushesImmediately()
    {
        var sendCallCount = 0;
        var stream = new AspNetCorePlugin.Stream
        {
            Send = async activity =>
            {
                sendCallCount++;
                activity.Id = $"test-id-{sendCallCount}";
                await Task.Delay(50); // Simulate some delay
                return activity;
            }
        };

        stream.Emit("Test message");
        await Task.Delay(60); // Wait for flush to complete
        Assert.True(sendCallCount > 0, "Should have sent at least one message");
    }

    [Fact]
    public async Task Stream_MultipleEmits_TimerCheck()
    {
        var sendCallCount = 0;
        var stream = new AspNetCorePlugin.Stream
        {
            Send = async activity =>
            {
                sendCallCount++;
                activity.Id = $"test-id-{sendCallCount}";
                await Task.Delay(50); // Simulate some delay
                return activity;
            }
        };

        stream.Emit("First message");
        stream.Emit("Second message");
        stream.Emit("Third message");
        stream.Emit("Fourth message");
        stream.Emit("Fifth message");
        stream.Emit("Sixth message");
        stream.Emit("Seventh message");
        stream.Emit("Eighth message");
        stream.Emit("Ninth message");
        stream.Emit("Tenth message");
        stream.Emit("Eleventh message");
        stream.Emit("Twelfth message");

        await Task.Delay(70); // Wait for initial flush

        Assert.Equal(1, sendCallCount); // First message should trigger flush immediately

        stream.Emit("Thirteenth message");

        await Task.Delay(300); // Less than 500ms from first flush
        Assert.True(sendCallCount == 1, "Should have sent only 1 message so far");

        await Task.Delay(300); // Now more than 500ms from first flush
        Assert.True(sendCallCount == 2, "Should have sent 2 messages by now");
    }

    [Fact]
    public async Task Stream_ErrorHandledGracefully()
    {
        var callCount = 0;
        var stream = new AspNetCorePlugin.Stream
        {
            Send = async activity =>
            {
                callCount++;
                if (callCount == 1) // Fail first attempt
                {
                    throw new TimeoutException("Operation timed out");
                }

                // Succeed on second attempt
                activity.Id = $"success-after-timeout-{callCount}";
                await Task.Delay(50); // Simulate some delay
                return activity;
            }
        };

        stream.Emit("Test message with timeout");
        await Task.Delay(600); // Wait for flush and 1 retry

        var result = await stream.Close();

        Assert.True(callCount > 1, "Should have retried after timeout");
        Assert.NotNull(result);
        Assert.Contains("Test message with timeout", result.Text);
    }

    [Fact]
    public async Task Stream_UpdateStatus_SendsTypingActivity()
    {
        var sentActivities = new List<IActivity>();
        var stream = new AspNetCorePlugin.Stream
        {
            Send = async activity =>
            {
                sentActivities.Add(activity);
                await Task.Delay(50); // Simulate some delay
                return activity;
            }
        };

        stream.Update("Thinking...");

        await Task.Delay(70); // Wait for flush to complete

        Assert.True(stream.Count > 0, "Should have processed the update");
        Assert.Equal(2, stream.Sequence); // Should increment sequence after sending
        Assert.True(sentActivities.Count > 0, "Should have sent at least one activity");

        var sentActivity = sentActivities.First();
        Assert.IsType<TypingActivity>(sentActivity);
        Assert.Equal("Thinking...", ((TypingActivity)sentActivity).Text);
        Assert.Equal(StreamType.Informative, ((TypingActivity)sentActivity).ChannelData?.StreamType);
    }

    [Fact]
    public async Task Stream_ConcurrentEmits_DoNotFlushSimultaneously()
    {
        var concurrentEntries = 0;
        var maxConcurrentEntries = 0;

        var stream = new AspNetCorePlugin.Stream
        {
            Send = async activity =>
            {
                // Track concurrent entries to the Send method (simulates Flush execution)
                Interlocked.Increment(ref concurrentEntries);
                maxConcurrentEntries = Math.Max(maxConcurrentEntries, concurrentEntries);
                Interlocked.Decrement(ref concurrentEntries);

                activity.Id = "test-id";
                await Task.Delay(50); // Simulate some delay
                return activity;
            }
        };

        // Emit from multiple threads simultaneously
        var barrier = new Barrier(10);
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            barrier.SignalAndWait(); // 10 threads must arrive before any can continue
            stream.Emit("Concurrent message");
        })).ToArray();

        await Task.WhenAll(tasks);

        await Task.Delay(70); // Wait for all flushes to complete

        Assert.True(maxConcurrentEntries == 1, 
            $"Flush entered concurrently {maxConcurrentEntries} times, expected 1");
    }

}