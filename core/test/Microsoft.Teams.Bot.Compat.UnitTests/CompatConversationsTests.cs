// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Compat.UnitTests;

public class CompatConversationsTests
{
    [Fact]
    public void CompatConversations_AppendsUserAgentToWrappedClient()
    {
        // Arrange
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);
        var originalUserAgent = conversationClient.DefaultCustomHeaders["User-Agent"];

        // Act
        CompatConversations compatConversations = new(conversationClient);

        // Assert
        var modifiedUserAgent = conversationClient.DefaultCustomHeaders["User-Agent"];
        Assert.NotEqual(originalUserAgent, modifiedUserAgent);

        // Should have Compat prepended
        Assert.StartsWith("Microsoft.Teams.Bot.Compat/", modifiedUserAgent);

        // Should contain original Core user agent
        Assert.Contains("Microsoft.Teams.Bot.Core/", modifiedUserAgent);

        // Should have space-separated format
        Assert.Contains(" ", modifiedUserAgent);

        // Validate it matches the pattern: Compat/version Core/version
        Assert.Matches(@"^Microsoft\.Teams\.Bot\.Compat/[\w\.\-\+]+ Microsoft\.Teams\.Bot\.Core/[\w\.\-\+]+$", modifiedUserAgent);
    }
}
