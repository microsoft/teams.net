// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.UnitTests;

public class CoreActivityBuilderTests
{
    [Fact]
    public void Constructor_DefaultConstructor_CreatesNewActivity()
    {
        CoreActivityBuilder builder = new();
        CoreActivity activity = builder.Build();

        Assert.NotNull(activity);
    }

    [Fact]
    public void Constructor_WithExistingActivity_UsesProvidedActivity()
    {
        CoreActivity existingActivity = new()
        {
            Id = "test-id",
        };

        CoreActivityBuilder builder = new(existingActivity);
        CoreActivity activity = builder.Build();

        Assert.Equal("test-id", activity.Id);
    }

    [Fact]
    public void Constructor_WithNullActivity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CoreActivityBuilder(null!));
    }

    [Fact]
    public void WithId_SetsActivityId()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithId("test-activity-id")
            .Build();

        Assert.Equal("test-activity-id", activity.Id);
    }

    [Fact]
    public void WithChannelId_SetsChannelId()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithChannelId("msteams")
            .Build();

        Assert.Equal("msteams", activity.ChannelId);
    }

    [Fact]
    public void WithType_SetsActivityType()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithType(ActivityType.Message)
            .Build();

        Assert.Equal(ActivityType.Message, activity.Type);
    }

    [Fact]
    public void WithText_SetsTextContent_As_Property()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithProperty("text", "Hello, World!")
            .Build();

        Assert.Equal("Hello, World!", activity.Properties["text"]);
    }

    [Fact]
    public void FluentAPI_CompleteActivity_BuildsCorrectly()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithType(ActivityType.Message)
            .WithId("activity-123")
            .WithChannelId("msteams")
            .WithProperty("text", "Test message")
            .Build();

        Assert.Equal(ActivityType.Message, activity.Type);
        Assert.Equal("activity-123", activity.Id);
        Assert.Equal("msteams", activity.ChannelId);
        Assert.Equal("Test message", activity.Properties["text"]?.ToString());
    }

    [Fact]
    public void FluentAPI_MethodChaining_ReturnsBuilderInstance()
    {
        CoreActivityBuilder builder = new();

        CoreActivityBuilder result1 = builder.WithId("id");
        CoreActivityBuilder result2 = builder.WithProperty("text", "text");
        CoreActivityBuilder result3 = builder.WithType(ActivityType.Message);

        Assert.Same(builder, result1);
        Assert.Same(builder, result2);
        Assert.Same(builder, result3);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsSameInstance()
    {
        CoreActivityBuilder builder = new CoreActivityBuilder()
            .WithId("test-id");

        CoreActivity activity1 = builder.Build();
        CoreActivity activity2 = builder.Build();

        Assert.Same(activity1, activity2);
    }

    [Fact]
    public void Builder_ModifyingExistingActivity_PreservesOriginalData()
    {
        CoreActivity original = new()
        {
            Id = "original-id",
            Type = ActivityType.Message
        };

        CoreActivity modified = new CoreActivityBuilder(original)
            .WithId("other-id")
            .Build();

        Assert.Equal("other-id", modified.Id);
        Assert.Equal(ActivityType.Message, modified.Type);
    }


    [Fact]
    public void WithId_WithEmptyString_SetsEmptyId()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithId(string.Empty)
            .Build();

        Assert.Equal(string.Empty, activity.Id);
    }

    [Fact]
    public void WithChannelId_WithEmptyString_SetsEmptyChannelId()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithChannelId(string.Empty)
            .Build();

        Assert.Equal(string.Empty, activity.ChannelId);
    }

    [Fact]
    public void WithType_WithEmptyString_SetsEmptyType()
    {
        CoreActivity activity = new CoreActivityBuilder()
            .WithType(string.Empty)
            .Build();

        Assert.Equal(string.Empty, activity.Type);
    }


    [Fact]
    public void Build_AfterModificationThenBuild_ReflectsChanges()
    {
        CoreActivityBuilder builder = new CoreActivityBuilder()
            .WithId("id-1");

        CoreActivity activity1 = builder.Build();
        Assert.Equal("id-1", activity1.Id);

        builder.WithId("id-2");
        CoreActivity activity2 = builder.Build();

        Assert.Same(activity1, activity2);
        Assert.Equal("id-2", activity2.Id);
    }

}
