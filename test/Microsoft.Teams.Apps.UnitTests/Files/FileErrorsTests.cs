// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

/// <summary>
/// The shape of the file exception hierarchy, which callers branch on.
/// </summary>
public class FileErrorsTests
{
    [Fact]
    public void EveryFileFailure_IsCatchableAsOneType()
    {
        // Re-parenting under FileException widens the hierarchy rather than narrowing it: existing catch clauses
        // still hold, and one clause now covers a failure mode added later.
        Assert.IsAssignableFrom<FileException>(new FileUrlExpiredException(FileUrlExpiredReason.FirstFetch));
        Assert.IsAssignableFrom<FileException>(new FileScopeNotSupportedException(ConversationType.GroupChat));
        Assert.IsAssignableFrom<FileException>(new FileRetrievalException(FileRetrievalFailureReason.AccessDenied));
    }

    [Fact]
    public void ExistingCatchClausesStillMatch()
    {
        Assert.IsAssignableFrom<Exception>(new FileUrlExpiredException(FileUrlExpiredReason.Reread));
        Assert.IsType<FileUrlExpiredException>(new FileUrlExpiredException(FileUrlExpiredReason.Reread));
        Assert.IsType<FileScopeNotSupportedException>(new FileScopeNotSupportedException(ConversationType.Channel));
    }

    [Fact]
    public void RetrievalError_DefaultsToTheAppWording_WhenNoActorWasSelected()
    {
        // The failure can precede credential selection, and a message that names nobody is worse than one that names
        // the common case.
        FileRetrievalException error = new(FileRetrievalFailureReason.AccessDenied);

        Assert.Null(error.Actor);
        Assert.Contains("the app", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(FileActor.App, "the app")]
    [InlineData(FileActor.AgenticUser, "the agentic user")]
    public void EveryActorIsNamedInProse(FileActor actor, string expected)
    {
        // Both members of the enum are covered, which is the point: `Actor` sits in return position on
        // `FileRetrievalException`, so a member added later without prose here would produce an error message that
        // names no identity at all. The rows must track the enum.
        FileRetrievalException error = new(FileRetrievalFailureReason.AccessDenied, actor);

        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RetrievalError_OmitsTheServiceSuffix_WhenTheServiceSaidNothing()
    {
        FileRetrievalException error = new(FileRetrievalFailureReason.AccessDenied, FileActor.AgenticUser);

        Assert.Null(error.Details);
        Assert.DoesNotContain("service said", error.Message, StringComparison.Ordinal);
        Assert.Contains("the agentic user", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpiredUrl_FirstFetch_StatesTheUrlCannotBeRenewed_AndOffersNoRemedy()
    {
        FileUrlExpiredException error = new(FileUrlExpiredReason.FirstFetch);

        Assert.DoesNotContain("not available via the SDK", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Files.Read.All", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Graph", error.Message, StringComparison.Ordinal);
        Assert.Contains("sent again", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpiredUrl_Reread_StillPointsAtReusingTheDownloadedFile()
    {
        FileUrlExpiredException error = new(FileUrlExpiredReason.Reread);

        Assert.Contains("DownloadedFile", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Files.Read.All", error.Message, StringComparison.Ordinal);
    }
}
