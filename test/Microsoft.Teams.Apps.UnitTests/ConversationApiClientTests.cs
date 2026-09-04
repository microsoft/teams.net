// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class ConversationApiClientTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplyMethods_SetExpectedTargetedFlag(bool isTargeted)
    {
        Mock<ConversationClient> conversationClient = new(
            new HttpClient(),
            NullLogger<ConversationClient>.Instance);
        bool? capturedIsTargeted = null;
        conversationClient
            .Setup(c => c.ReplyToActivityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CoreActivityInput, Uri, bool, BotRequestContext?, Dictionary<string, string>?, CancellationToken>(
                (_, _, _, _, targeted, _, _, _) => capturedIsTargeted = targeted)
            .ReturnsAsync(new SendActivityResponse { Id = "reply-id" });

        ConversationApiClient client = new(
            new Uri("https://test.service.url/"),
            conversationClient.Object);
        MessageActivityInput activity = new MessageActivityInput().WithText("hello");

        if (isTargeted)
        {
            await client.ReplyToTargetedActivityAsync("conversation-id", "root-id", activity);
        }
        else
        {
            await client.ReplyToActivityAsync("conversation-id", "root-id", activity);
        }

        Assert.Equal(isTargeted, capturedIsTargeted);
    }
}
