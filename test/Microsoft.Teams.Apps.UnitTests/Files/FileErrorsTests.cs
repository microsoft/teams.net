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
        Assert.IsAssignableFrom<FileException>(new FileCredentialException(FileActor.App));
        Assert.IsAssignableFrom<FileException>(new FileAccessException(403, FileActor.AgenticUser));
    }

    [Fact]
    public void ExistingCatchClausesStillMatch()
    {
        Assert.IsAssignableFrom<Exception>(new FileUrlExpiredException(FileUrlExpiredReason.Reread));
        Assert.IsType<FileUrlExpiredException>(new FileUrlExpiredException(FileUrlExpiredReason.Reread));
        Assert.IsType<FileScopeNotSupportedException>(new FileScopeNotSupportedException(ConversationType.Channel));
    }

    [Fact]
    public void CredentialError_NamesNoIdentity_WhenNoneWasSelected()
    {
        // This previously defaulted to the app's wording. That reads as guidance: the app message tells the reader an
        // app identity "may be used but is not supported", which is the wrong remedy for a failure where no identity
        // was ever chosen. Naming nobody is the honest answer.
        FileCredentialException error = new(actor: null);

        Assert.Null(error.Actor);
        Assert.Contains("no identity had been selected", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("the app has no usable Graph credential", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(FileActor.App, "the app")]
    [InlineData(FileActor.AgenticUser, "the agentic user")]
    public void EveryActorIsNamedInProse(FileActor actor, string expected)
    {
        // Both members of the enum are covered, which is the point: `Actor` sits in return position on
        // `FileAccessException`, so a member added later without prose here would produce an error message that
        // names no identity at all. The rows must track the enum.
        FileAccessException error = new(403, actor);

        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AccessError_KeepsA401DistinguishableFromA403()
    {
        // The two have different remedies: a 401 says the token was rejected, a 403 says the identity lacks the
        // grant. Collapsing them sends a reader to inspect file sharing when the real fault is the token.
        FileAccessException unauthorized = new(401, FileActor.AgenticUser);
        FileAccessException forbidden = new(403, FileActor.AgenticUser);

        Assert.Equal(401, unauthorized.Status);
        Assert.Contains("rejected", unauthorized.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("never have been shared", unauthorized.Message, StringComparison.Ordinal);
        Assert.Contains("never have been shared", forbidden.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AccessError_OmitsTheServiceSuffix_WhenTheServiceSaidNothing()
    {
        FileAccessException error = new(403, FileActor.AgenticUser);

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
