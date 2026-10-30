// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Teams.Core.Hosting;
using Moq;

namespace Microsoft.Teams.Core.UnitTests.Hosting;

/// <summary>
/// <see cref="BotTokenProvider.GetAgenticAppTokenAsync"/> acquires an app-only token as an agent instance.
/// </summary>
public class BotTokenProviderAgenticAppTests
{
    private const string AgenticAppId = "d94529f7-d988-4caa-8635-a47201acec74";
    private const string Scope = "api://socket-mode/.default";

    private sealed class Recorder
    {
        public List<(string Scope, AuthorizationHeaderProviderOptions Options)> Calls { get; } = [];

        public BotTokenProvider Provider { get; }

        public Recorder()
        {
            Mock<IAuthorizationHeaderProvider> header = new();
            header
                .Setup(h => h.CreateAuthorizationHeaderForAppAsync(It.IsAny<string>(), It.IsAny<AuthorizationHeaderProviderOptions>(), It.IsAny<CancellationToken>()))
                .Returns((string scope, AuthorizationHeaderProviderOptions options, CancellationToken _) =>
                {
                    Calls.Add((scope, options));
                    return Task.FromResult("Bearer agent-instance-token");
                });

            Provider = new BotTokenProvider(header.Object, "AzureAd");
        }
    }

    [Fact]
    public async Task AcquiresAnAppTokenAsTheAgentInstanceForTheScope()
    {
        Recorder recorder = new();

        string? token = await recorder.Provider.GetAgenticAppTokenAsync(AgenticAppId, Scope);

        Assert.Equal("agent-instance-token", token);
        (string scope, AuthorizationHeaderProviderOptions options) = Assert.Single(recorder.Calls);
        Assert.Equal(Scope, scope);
        Assert.Equal("AzureAd", options.AcquireTokenOptions.AuthenticationOptionsName);
        Assert.Null(options.AcquireTokenOptions.Tenant);

        // WithAgentIdentity marks the acquisition with the agent instance; compare that marker rather than the
        // whole dictionary, which also holds a freshly created options object.
        AuthorizationHeaderProviderOptions expected = new() { AcquireTokenOptions = new AcquireTokenOptions() };
        expected.WithAgentIdentity(AgenticAppId);
        KeyValuePair<string, object> marker = Assert.Single(
            expected.AcquireTokenOptions.ExtraParameters!,
            parameter => AgenticAppId.Equals(parameter.Value));
        Assert.Equal(AgenticAppId, options.AcquireTokenOptions.ExtraParameters?[marker.Key]);
    }

    [Fact]
    public async Task OverridesTheTenantWhenOneIsGiven()
    {
        Recorder recorder = new();

        await recorder.Provider.GetAgenticAppTokenAsync(AgenticAppId, Scope, "agent-tenant");

        Assert.Equal("agent-tenant", Assert.Single(recorder.Calls).Options.AcquireTokenOptions.Tenant);
    }

    [Theory]
    [InlineData(null, Scope)]
    [InlineData("", Scope)]
    [InlineData(" ", Scope)]
    [InlineData(AgenticAppId, null)]
    [InlineData(AgenticAppId, "")]
    [InlineData(AgenticAppId, " ")]
    public async Task Throws_WithoutAcquiring_WhenTheAgentInstanceOrScopeIsMissing(string? agenticAppId, string? scope)
    {
        Recorder recorder = new();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => recorder.Provider.GetAgenticAppTokenAsync(agenticAppId!, scope!));

        Assert.Empty(recorder.Calls);
    }
}
