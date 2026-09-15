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
}
