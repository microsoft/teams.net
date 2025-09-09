// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Plugins.AspNetCore;

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
    
    [Fact]
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
        await Task.Delay(600); // Wait for the flush task to complete

        Assert.True(stream.Count > 0, "Should have processed the update");
        Assert.Equal(2, stream.Sequence); // Should increment sequence after sending
        
        Assert.True(sentActivities.Count > 0, "Should have sent at least one activity");
        var sentActivity = sentActivities.First();
        Assert.IsType<TypingActivity>(sentActivity);
        Assert.Equal("Thinking...", ((TypingActivity)sentActivity).Text);
        Assert.Equal(StreamType.Informative, ((TypingActivity)sentActivity).ChannelData?.StreamType);
    }

}
