// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.UnitTests.Schema;

public class ConversationTests
{
    [Fact]
    public void ToThreadedConversationId_ConstructsThreadedConversationId()
    {
        string result = ConversationExtensions.ToThreadedConversationId("19:abc@thread.skype", "1680000000000");
        Assert.Equal("19:abc@thread.skype;messageid=1680000000000", result);
    }

    [Fact]
    public void ToThreadedConversationId_WorksWithDifferentConversationIdFormats()
    {
        string result = ConversationExtensions.ToThreadedConversationId("19:meeting_abc@thread.v2", "999");
        Assert.Equal("19:meeting_abc@thread.v2;messageid=999", result);
    }

    [Fact]
    public void ToThreadedConversationId_ThrowsOnEmptyConversationId()
    {
        Assert.Throws<ArgumentException>(() => ConversationExtensions.ToThreadedConversationId("", "123"));
    }

    [Fact]
    public void ToThreadedConversationId_ThrowsOnNullConversationId()
    {
        Assert.Throws<ArgumentException>(() => ConversationExtensions.ToThreadedConversationId(null!, "123"));
    }

    [Fact]
    public void ToThreadedConversationId_ThrowsOnEmptyMessageId()
    {
        Assert.Throws<ArgumentException>(() => ConversationExtensions.ToThreadedConversationId("19:abc@thread.skype", ""));
    }

    [Fact]
    public void ToThreadedConversationId_ThrowsOnZeroMessageId()
    {
        Assert.Throws<ArgumentException>(() => ConversationExtensions.ToThreadedConversationId("19:abc@thread.skype", "0"));
    }

    [Fact]
    public void ToThreadedConversationId_ThrowsOnNonNumericMessageId()
    {
        Assert.Throws<ArgumentException>(() => ConversationExtensions.ToThreadedConversationId("19:abc@thread.skype", "abc"));
    }

    [Fact]
    public void ToThreadedConversationId_ThrowsOnNegativeMessageId()
    {
        Assert.Throws<ArgumentException>(() => ConversationExtensions.ToThreadedConversationId("19:abc@thread.skype", "-1"));
    }

    [Fact]
    public void ToThreadedConversationId_ThrowsOnDecimalMessageId()
    {
        Assert.Throws<ArgumentException>(() => ConversationExtensions.ToThreadedConversationId("19:abc@thread.skype", "1.5"));
    }

    [Fact]
    public void ToThreadedConversationId_StripsExistingMessageIdAndReplacesWithThreadRoot()
    {
        string result = ConversationExtensions.ToThreadedConversationId("19:abc@thread.skype;messageid=111", "222");
        Assert.Equal("19:abc@thread.skype;messageid=222", result);
    }

    [Fact]
    public void ThreadId_StripsMessageIdSuffix()
    {
        Conversation conv = new("19:abc@thread.skype;messageid=1680000000000");
        Assert.Equal("19:abc@thread.skype", conv.ThreadId());
    }

    [Fact]
    public void ThreadId_ReturnsIdWhenNoSuffix()
    {
        Conversation conv = new("19:abc@thread.skype");
        Assert.Equal("19:abc@thread.skype", conv.ThreadId());
    }
}
