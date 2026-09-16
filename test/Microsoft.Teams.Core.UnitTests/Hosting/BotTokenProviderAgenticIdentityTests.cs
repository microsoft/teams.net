// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Core.UnitTests.Hosting;

/// <summary>
/// The identity guard on <see cref="BotTokenProvider.GetAgenticUserTokenAsync"/>.
/// <para><b>Why this is worth its own file rather than being folded into the file-path tests.</b> Those all supply a
/// well-formed identity, so none of them reaches this guard. The guard is what stands between a malformed inbound
/// identity and <c>WithAgentUserIdentity</c>, which takes a <see cref="Guid"/> and would throw on a value that does
/// not parse. Degrading to <c>null</c> lets the caller report "no Graph credential", which is a diagnosable error;
/// throwing here would surface as an unhandled exception from inside token acquisition instead.</para>
/// <para>The <see cref="Guid"/> branch is <b>.NET-specific</b>. TypeScript and Python carry the same two emptiness
/// checks, but neither has to parse the user id, because neither underlying library demands a typed GUID. So this is
/// the one arm of the guard with no counterpart to compare against, and the one most likely to be dropped by someone
/// simplifying the condition.</para>
/// </summary>
public class BotTokenProviderAgenticIdentityTests
{
    /// <summary>Records whether acquisition was reached at all, which is the real assertion in every case below.</summary>
    private sealed class Recorder
    {
        public int Acquisitions { get; private set; }

        public BotTokenProvider Provider { get; }

        public Recorder()
        {
            Mock<IAuthorizationHeaderProvider> header = new();

            header
                .Setup(h => h.CreateAuthorizationHeaderAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<AuthorizationHeaderProviderOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Acquisitions++;
                    return Task.FromResult("Bearer agent-token");
                });

            Provider = new BotTokenProvider(header.Object);
        }
    }

    private const string Scope = "https://graph.microsoft.com/.default";

    private static AgenticIdentity Identity(string? appId, string? userId) => new()
    {
        AgenticAppId = appId!,
        AgenticUserId = userId!,
        AgenticAppBlueprintId = "blueprint-id"
    };

    /// <summary>
    /// A blueprint-level identity names no agentic user, so there is nobody to acquire a token as.
    /// <para>This shape is reachable rather than hypothetical: an identity materializes whenever the blueprint id is
    /// present, so an activity can legitimately carry one that names no instance and no user.</para>
    /// </summary>
    [Theory]
    [InlineData(null, "31e29ddb-e4ce-427e-8bda-1a37eb12d43f")]
    [InlineData("", "31e29ddb-e4ce-427e-8bda-1a37eb12d43f")]
    [InlineData("d94529f7-d988-4caa-8635-a47201acec74", null)]
    [InlineData("d94529f7-d988-4caa-8635-a47201acec74", "")]
    public async Task ReturnsNull_WithoutAcquiring_WhenTheIdentityNamesNoAgenticUser(string? appId, string? userId)
    {
        Recorder recorder = new();

        string? token = await recorder.Provider.GetAgenticUserTokenAsync(Identity(appId, userId), Scope, CancellationToken.None);

        Assert.Null(token);

        // Not merely "returned null": it must not have spent an acquisition finding that out.
        Assert.Equal(0, recorder.Acquisitions);
    }

    /// <summary>
    /// A user id that is not a GUID is refused rather than parsed optimistically.
    /// <para>Without this arm the value reaches <c>WithAgentUserIdentity</c>, which requires a <see cref="Guid"/>, and
    /// the caller gets an exception from inside acquisition rather than a <c>NoGraphCredential</c> it can report.</para>
    /// </summary>
    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("31e29ddb-e4ce-427e-8bda")]
    [InlineData("31e29ddb e4ce 427e 8bda 1a37eb12d43f")]
    public async Task ReturnsNull_WithoutAcquiring_WhenTheAgenticUserIdIsNotAGuid(string userId)
    {
        Recorder recorder = new();

        string? token = await recorder.Provider.GetAgenticUserTokenAsync(
            Identity("d94529f7-d988-4caa-8635-a47201acec74", userId), Scope, CancellationToken.None);

        Assert.Null(token);
        Assert.Equal(0, recorder.Acquisitions);
    }

    /// <summary>
    /// The complementary case, so the tests above cannot pass by the guard rejecting everything.
    /// <para>The ids are the real ones from the test tenant, which is also a reminder that a live agentic user id is a
    /// GUID rather than an opaque string.</para>
    /// </summary>
    [Fact]
    public async Task Acquires_WhenTheIdentityNamesBothAnAppAndAGuidUser()
    {
        Recorder recorder = new();

        string? token = await recorder.Provider.GetAgenticUserTokenAsync(
            Identity("d94529f7-d988-4caa-8635-a47201acec74", "31e29ddb-e4ce-427e-8bda-1a37eb12d43f"),
            Scope,
            CancellationToken.None);

        Assert.Equal("agent-token", token);
        Assert.Equal(1, recorder.Acquisitions);
    }

    /// <summary>A null identity is a programming error rather than a malformed activity, so it throws.</summary>
    [Fact]
    public async Task Throws_WhenTheIdentityIsNull()
    {
        Recorder recorder = new();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => recorder.Provider.GetAgenticUserTokenAsync(null!, Scope, CancellationToken.None));
    }
}
