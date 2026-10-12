// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class FilesCredentialTests
{
    private static readonly AgenticIdentity Agentic = new()
    {
        AgenticAppBlueprintId = "blueprint-1",
        AgenticAppId = "agentic-app-1",
        AgenticUserId = "agentic-user-1",
        TenantId = "tenant-1",
    };

    private static Task<string?> Token(string? value) => Task.FromResult(value);

    [Fact]
    public async Task ReadsAsTheApp_WhenThereIsNoAgenticIdentity()
    {
        GraphCredential credential = FilesCredential.Select(
            agenticIdentity: null,
            graphBaseUrlRoot: null,
            getAppGraphToken: _ => Token("app-token"),
            getAgenticGraphToken: (_, _) => Token("agentic-token"));

        Assert.Equal(FileActor.App, credential.Actor);
        Assert.Equal("app-token", await credential.GetTokenAsync());
    }

    [Fact]
    public async Task ReadsAsTheAgenticUser_WhenAnAgenticIdentityIsPresent()
    {
        // The seam every other file test assumes. Supplying a credential directly, as those tests do, would leave
        // them all passing even if this branch were inverted, and the resulting failure would look like a consent
        // problem rather than a wrong-identity problem.
        GraphCredential credential = FilesCredential.Select(
            Agentic,
            graphBaseUrlRoot: null,
            getAppGraphToken: _ => Token("app-token"),
            getAgenticGraphToken: (_, _) => Token("agentic-token"));

        Assert.Equal(FileActor.AgenticUser, credential.Actor);
        Assert.Equal("agentic-token", await credential.GetTokenAsync());
    }

    [Fact]
    public async Task NeverFallsBackToTheAppToken_ForAnAgenticIdentity()
    {
        // An app token sees a different set than what was shared with the agent, so a silent fallback would 403 on
        // exactly the agent's own files.
        bool appTokenRequested = false;

        GraphCredential credential = FilesCredential.Select(
            Agentic,
            graphBaseUrlRoot: null,
            getAppGraphToken: _ =>
            {
                appTokenRequested = true;
                return Token("app-token");
            },
            getAgenticGraphToken: (_, _) => Token(null));

        Assert.Null(await credential.GetTokenAsync());
        Assert.False(appTokenRequested);
    }

    [Fact]
    public async Task PassesTheAgenticIdentityThroughUnchanged()
    {
        AgenticIdentity? seen = null;

        await FilesCredential.Select(
            Agentic,
            graphBaseUrlRoot: null,
            getAppGraphToken: _ => Token(null),
            getAgenticGraphToken: (identity, _) =>
            {
                seen = identity;
                return Token("agentic-token");
            }).GetTokenAsync();

        Assert.Same(Agentic, seen);
    }

    [Fact]
    public void CarriesTheGraphHostRootAlongsideTheToken_ForBothActors()
    {
        // Keeping the token and its destination on one object removes the failure mode where a new code path wires
        // one through and forgets the other.
        Uri root = new("https://graph.microsoft.us");

        Assert.Equal(root, FilesCredential.Select(null, root, _ => Token("app-token"), (_, _) => Token("agentic-token")).BaseUrlRoot);
        Assert.Equal(root, FilesCredential.Select(Agentic, root, _ => Token("app-token"), (_, _) => Token("agentic-token")).BaseUrlRoot);
    }

    [Fact]
    public void LeavesTheHostRootUnset_WhenNoneIsConfigured_SoThePublicDefaultApplies()
        => Assert.Null(FilesCredential.Select(null, null, _ => Token("app-token"), (_, _) => Token(null)).BaseUrlRoot);

    [Fact]
    public void AcquiresNoToken_UntilOneIsAskedFor()
    {
        // A turn that never touches files should never pay for a token.
        bool appTokenRequested = false;
        bool agenticTokenRequested = false;

        Func<CancellationToken, Task<string?>> app = _ =>
        {
            appTokenRequested = true;
            return Token("app-token");
        };
        Func<AgenticIdentity, CancellationToken, Task<string?>> agentic = (_, _) =>
        {
            agenticTokenRequested = true;
            return Token("agentic-token");
        };

        FilesCredential.Select(Agentic, null, app, agentic);
        FilesCredential.Select(null, null, app, agentic);

        Assert.False(appTokenRequested);
        Assert.False(agenticTokenRequested);
    }

    [Fact]
    public async Task ReportsNoToken_RatherThanThrowing_WhenTheAppHasNoCredentials()
    {
        // Surfaces downstream as a typed NoGraphCredential failure before any HTTP call is made.
        GraphCredential credential = FilesCredential.Select(null, null, _ => Token(null), (_, _) => Token(null));

        Assert.Null(await credential.GetTokenAsync());
    }

    // ==================== SelectInlineImage ====================

    private static readonly AgenticIdentity AgenticUser = new()
    {
        AgenticAppBlueprintId = "blueprint-1",
        AgenticAppId = "agentic-app-1",
        AgenticUserId = "31e29ddb-e4ce-427e-8bda-1a37eb12d43f",
        TenantId = "tenant-1",
    };

    private static BotApiCredential InlineImage(Func<Task<string?>> token, AgenticIdentity? agenticIdentity = null, Uri? serviceUrl = null)
        => FilesCredential.SelectInlineImage(agenticIdentity, serviceUrl, (_, _) => token());

    [Fact]
    public void InlineImage_ReadsAsTheApp_WhenThereIsNoAgenticIdentity_AndAsTheAgenticUser_WhenThereIs()
    {
        Assert.Equal(FileActor.App, InlineImage(() => Token("bot-token")).Actor);
        Assert.Equal(FileActor.AgenticUser, InlineImage(() => Token("bot-token"), AgenticUser).Actor);
    }

    [Theory]
    [InlineData(null, "31e29ddb-e4ce-427e-8bda-1a37eb12d43f")]
    [InlineData("agentic-app-1", null)]
    [InlineData("agentic-app-1", "agentic-user-1")]
    public void InlineImage_ReadsAsTheApp_WhenTheAgenticIdentityCannotBackAnAgenticToken(string? agenticAppId, string? agenticUserId)
    {
        // The turn's API client sends an app token for these, so that is the identity a refusal names.
        AgenticIdentity identity = new() { AgenticAppBlueprintId = "blueprint-1", AgenticAppId = agenticAppId, AgenticUserId = agenticUserId };

        Assert.Equal(FileActor.App, InlineImage(() => Token("bot-token"), identity).Actor);
    }

    [Fact]
    public async Task InlineImage_HandsTheAgenticIdentityToTheTokenSource()
    {
        AgenticIdentity? seen = null;
        BotApiCredential credential = FilesCredential.SelectInlineImage(AgenticUser, null, (identity, _) =>
        {
            seen = identity;
            return Token("bot-token");
        });

        await credential.GetTokenAsync();

        Assert.Same(AgenticUser, seen);
    }

    [Fact]
    public async Task InlineImage_ResolvesTheToken()
    {
        Assert.Equal("plain", await InlineImage(() => Token("plain")).GetTokenAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task InlineImage_ReportsNoToken_ForAnAbsentOrBlankSource(string? token)
    {
        Assert.Null(await InlineImage(() => Token(token)).GetTokenAsync());
    }

    [Fact]
    public void InlineImage_CarriesTheServiceUrlItWasGivenAsTheAllowedDestination()
    {
        BotApiCredential credential = InlineImage(() => Token("bot-token"), serviceUrl: new Uri("https://smba.trafficmanager.net/amer/"));

        Assert.Equal(new Uri("https://smba.trafficmanager.net/amer/"), credential.ServiceUrl);
    }

    [Fact]
    public async Task InlineImage_ReadsTheTokenSourceAtFetchTime_RatherThanWhenBuilt()
    {
        string current = "first";
        BotApiCredential credential = InlineImage(() => Token(current));

        current = "second";

        Assert.Equal("second", await credential.GetTokenAsync());
    }
}
