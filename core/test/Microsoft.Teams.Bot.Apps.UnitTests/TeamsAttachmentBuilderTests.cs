// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class TeamsAttachmentBuilderTests
{
    [Fact]
    public void Build_FirstCall_ReturnsAttachment()
    {
        TeamsAttachment result = TeamsActivity.CreateBuilder()
            .WithAttachment(new TeamsAttachment { ContentType = "text/plain" })
            .Build()
            .Attachments![0];

        // Verify via TeamsAttachmentBuilder directly
        TeamsAttachmentBuilder builder = new TeamsAttachmentBuilder()
            .WithContentType("text/plain")
            .WithContent("hello");

        TeamsAttachment attachment = builder.Build();
        Assert.NotNull(attachment);
        Assert.Equal("text/plain", attachment.ContentType);
    }

    [Fact]
    public void Build_SecondCall_ThrowsInvalidOperationException()
    {
        // Arrange
        TeamsAttachmentBuilder builder = new TeamsAttachmentBuilder()
            .WithContentType("text/plain");

        builder.Build(); // first call — OK

        // Act & Assert — second call must throw
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_AfterMutation_ThrowsOnSecondBuild()
    {
        // Demonstrate that the guard prevents post-Build mutations from being silently
        // applied to an already-returned attachment.
        TeamsAttachmentBuilder builder = new TeamsAttachmentBuilder()
            .WithContentType("application/vnd.microsoft.card.adaptive")
            .WithContent(new { type = "AdaptiveCard" });

        TeamsAttachment first = builder.Build();
        Assert.Equal("application/vnd.microsoft.card.adaptive", first.ContentType);

        // Attempting to build again (e.g. after changing the content) must fail
        Assert.Throws<InvalidOperationException>(() =>
        {
            builder.WithContent(new { type = "Modified" });
            builder.Build();
        });
    }
}
