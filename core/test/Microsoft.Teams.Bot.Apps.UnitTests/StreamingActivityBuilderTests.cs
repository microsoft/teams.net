// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class StreamingActivityBuilderTests
{
    [Fact]
    public void CreateBuilder_DefaultText_CreatesStreamingActivity()
    {
        StreamingActivity activity = StreamingActivity.CreateBuilder().Build();

        Assert.NotNull(activity);
        Assert.Equal(TeamsActivityType.Typing, activity.Type);
        Assert.Equal("", activity.Text);
    }

    [Fact]
    public void CreateBuilder_WithInitialText_SetsText()
    {
        StreamingActivity activity = StreamingActivity.CreateBuilder("Hello").Build();

        Assert.Equal("Hello", activity.Text);
    }

    [Fact]
    public void WithText_UpdatesText()
    {
        StreamingActivity activity = StreamingActivity.CreateBuilder("initial")
            .WithText("updated text")
            .Build();

        Assert.Equal("updated text", activity.Text);
    }

    [Fact]
    public void Build_ActivityHasStreamInfoEntity()
    {
        StreamingActivity activity = StreamingActivity.CreateBuilder("chunk").Build();

        Assert.NotNull(activity.StreamInfo);
        Assert.NotNull(activity.Entities);
        Assert.Contains(activity.Entities, e => e is StreamInfoEntity);
    }

    [Fact]
    public void MethodChaining_ReturnsBuilderInstance()
    {
        StreamingActivityBuilder streamBuilder = StreamingActivity.CreateBuilder();

        StreamingActivityBuilder result1 = streamBuilder.WithId("id");
        StreamingActivityBuilder result2 = streamBuilder.WithText("text");

        Assert.Same(streamBuilder, result1);
        Assert.Same(streamBuilder, result2);
    }

    [Fact]
    public void Build_ReturnsStreamingActivity()
    {
        StreamingActivity activity = StreamingActivity.CreateBuilder("test").Build();

        Assert.IsType<StreamingActivity>(activity);
    }
}
