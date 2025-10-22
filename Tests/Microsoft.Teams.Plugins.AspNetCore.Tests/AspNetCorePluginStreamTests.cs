// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Plugins.AspNetCore.Tests;

public class AspNetCorePluginStreamTests
{
    [Fact]
    public void Stream_EmitMessage_FlushesImmediately()
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
                await Task.Delay(50); 
                sendCallCount++;
                activity.Id = $"test-id-{sendCallCount}";
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

        await Task.Delay(60);  // for send to run

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
        await Task.Delay(600); // Wait for flush and 1 retry

        var result = await stream.Close();

        Assert.True(callCount > 1, "Should have retried after timeout");
        Assert.NotNull(result);
        Assert.Contains("Test message with timeout", result.Text);
    }

    [Fact]
    public void Stream_UpdateStatus_SendsTypingActivity()
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

        Assert.True(stream.Count > 0, "Should have processed the update");
        Assert.Equal(2, stream.Sequence); // Should increment sequence after sending
        Assert.True(sentActivities.Count > 0, "Should have sent at least one activity");

        var sentActivity = sentActivities.First();
        Assert.IsType<TypingActivity>(sentActivity);
        Assert.Equal("Thinking...", ((TypingActivity)sentActivity).Text);
        Assert.Equal(StreamType.Informative, ((TypingActivity)sentActivity).ChannelData?.StreamType);
    }

}