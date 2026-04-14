// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Api.Tests;

public class ConversationTests
{
    [Fact]
    public void ToThreadId_ConstructsThreadedConversationId()
    {
        var result = Conversation.ToThreadId("19:abc@thread.skype", "1680000000000");
        Assert.Equal("19:abc@thread.skype;messageid=1680000000000", result);
    }

    [Fact]
    public void ToThreadId_WorksWithDifferentConversationIdFormats()
    {
        var result = Conversation.ToThreadId("19:meeting_abc@thread.v2", "999");
        Assert.Equal("19:meeting_abc@thread.v2;messageid=999", result);
    }

    [Fact]
    public void ToThreadId_ThrowsOnEmptyConversationId()
    {
        Assert.Throws<ArgumentException>(() => Conversation.ToThreadId("", "123"));
    }

    [Fact]
    public void ToThreadId_ThrowsOnNullConversationId()
    {
        Assert.Throws<ArgumentException>(() => Conversation.ToThreadId(null!, "123"));
    }

    [Fact]
    public void ToThreadId_ThrowsOnEmptyMessageId()
    {
        Assert.Throws<ArgumentException>(() => Conversation.ToThreadId("19:abc@thread.skype", ""));
    }

    [Fact]
    public void ToThreadId_ThrowsOnZeroMessageId()
    {
        Assert.Throws<ArgumentException>(() => Conversation.ToThreadId("19:abc@thread.skype", "0"));
    }

    [Fact]
    public void ToThreadId_ThrowsOnNonNumericMessageId()
    {
        Assert.Throws<ArgumentException>(() => Conversation.ToThreadId("19:abc@thread.skype", "abc"));
    }

    [Fact]
    public void ToThreadId_ThrowsOnNegativeMessageId()
    {
        Assert.Throws<ArgumentException>(() => Conversation.ToThreadId("19:abc@thread.skype", "-1"));
    }

    [Fact]
    public void ToThreadId_ThrowsOnDecimalMessageId()
    {
        Assert.Throws<ArgumentException>(() => Conversation.ToThreadId("19:abc@thread.skype", "1.5"));
    }

    [Fact]
    public void ToThreadId_StripsExistingMessageIdAndReplacesWithThreadRoot()
    {
        var result = Conversation.ToThreadId("19:abc@thread.skype;messageid=111", "222");
        Assert.Equal("19:abc@thread.skype;messageid=222", result);
    }
}
