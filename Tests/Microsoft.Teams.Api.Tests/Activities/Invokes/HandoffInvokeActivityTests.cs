

using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Api.Tests.Activities;

public class HandoffInvokeActivityTests
{
    [Fact]
    public void HandoffInvokeActivity_JsonSerialize()
    {
        var activity = new HandoffActivity()
        {
            Id = "handoffId",
            ChannelId = new ChannelId("channelId"),
            Value = new HandoffActivityValue() { Continuation = " valid continuation" }
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Handoff/action";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/HandoffActivity.json"
        ), json);
    }

    [Fact]
    public void HandoffInvokeActivity_JsonSerialize_Derived()
    {
        InvokeActivity activity = new HandoffActivity()
        {
            Id = "handoffId",
            ChannelId = new ChannelId("channelId"),
            Value = new HandoffActivityValue() { Continuation = " valid continuation" }
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/HandoffActivity.json"
        ), json);
    }

    [Fact]
    public void HandoffInvokeActivity_JsonSerialize_Interface_Derived()
    {
        Activity activity = new HandoffActivity()
        {
            Id = "handoffId",
            ChannelId = new ChannelId("channelId"),
            Value = new HandoffActivityValue() { Continuation = " valid continuation" }
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/HandoffActivity.json"
        ), json);
    }


    [Fact]
    public void HandoffInvokeActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/HandoffActivity.json");
        var activity = JsonSerializer.Deserialize<HandoffActivity>(json);
        var expected = new HandoffActivity()
        {
            Id = "handoffId",
            ChannelId = new ChannelId("channelId"),
            Value = new HandoffActivityValue() { Continuation = " valid continuation" }
        };
        Assert.Equal(expected.ToString(), activity?.ToString());
    }

    [Fact]
    public void HandoffInvokeActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/HandoffActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = new HandoffActivity()
        {
            Id = "handoffId",
            ChannelId = new ChannelId("channelId"),
            Value = new HandoffActivityValue() { Continuation = " valid continuation" }
        };

        Assert.Equal(expected.ToString(), activity?.ToString());
    }

    [Fact]
    public void HandoffInvokeActivity_JsonDeserialize_Interface_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/HandoffActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = new HandoffActivity()
        {
            Id = "handoffId",
            ChannelId = new ChannelId("channelId"),
            Value = new HandoffActivityValue() { Continuation = " valid continuation" }
        };

        Assert.Equal(expected.ToString(), activity?.ToString());
    }

}