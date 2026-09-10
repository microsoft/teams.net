// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// That <c>ctx.Files</c> is handed a credential for the right actor.
/// <para>Credential selection and the accessor are each covered on their own, and both stay green if nothing connects them, leaving <c>ctx.Files</c> with no credential and every Graph path unreachable.</para>
/// </summary>
public class ContextFilesCredentialTests
{
    private const string ContentUrl = "https://contoso.sharepoint.com/personal/a/Documents/report.pdf";

    /// <summary>
    /// Captures the scope every acquisition is asked for, so the derived Graph scope is observable.
    /// <para>Fakes the Identity abstraction UNDERNEATH the provider rather than the provider itself, so the real <see cref="BotTokenProvider"/> runs: its option building, its agentic identity validation, and its scheme stripping are all exercised here rather than replaced by a stub that cannot get them wrong.</para>
    /// </summary>
    private sealed class RecordingTokenProvider
    {
        public List<string> Scopes { get; } = [];

        public List<string?> Tenants { get; } = [];

        public BotTokenProvider Provider { get; }

        public RecordingTokenProvider()
        {
            Mock<IAuthorizationHeaderProvider> header = new();

            header
                .Setup(h => h.CreateAuthorizationHeaderForAppAsync(It.IsAny<string>(), It.IsAny<AuthorizationHeaderProviderOptions>(), It.IsAny<CancellationToken>()))
                .Returns((string scope, AuthorizationHeaderProviderOptions options, CancellationToken _) =>
                {
                    Scopes.Add(scope);
                    Tenants.Add(options.AcquireTokenOptions.Tenant);
                    return Task.FromResult("Bearer app-token");
                });

            header
                .Setup(h => h.CreateAuthorizationHeaderAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<AuthorizationHeaderProviderOptions>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .Returns((IEnumerable<string> scopes, AuthorizationHeaderProviderOptions _, System.Security.Claims.ClaimsPrincipal? __, CancellationToken ___) =>
                {
                    Scopes.Add(scopes.Single());
                    return Task.FromResult("Bearer agent-token");
                });

            Provider = new BotTokenProvider(header.Object);
        }
    }

    private static TeamsBotApplication BuildApp(Uri? graphBaseUrl = null, RecordingTokenProvider? tokenProvider = null)
    {
        Mock<UserTokenClient> mockUserTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);
        Mock<ConversationClient> mockConversationClient = new(new HttpClient(), NullLogger<ConversationClient>.Instance);

        ApiClient apiClient = new(new HttpClient(), mockConversationClient.Object, mockUserTokenClient.Object);

        return new TeamsBotApplication(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id", GraphBaseUrl = graphBaseUrl },
            stateLoader: null,
            fileDownloader: null)
        {
            // Back-filled rather than passed, because that is the only wiring path the SDK has: a constructor parameter would never reach a subclass.
            TokenProvider = tokenProvider?.Provider
        };
    }

    /// <summary>An inbound message carrying one Agentic-User-shaped attachment, and optionally an agentic recipient.</summary>
    private static MessageActivity ActivityWith(bool agenticRecipient)
    {
        CoreActivity core = new() { Type = TeamsActivityTypes.Message };

        core.Properties["attachments"] = JsonSerializer.SerializeToElement<IList<TeamsAttachment>>(
        [
            new TeamsAttachment
            {
                ContentType = AttachmentContentType.FileDownloadInfo,
                ContentUrl = new Uri(ContentUrl),
                Name = "report.pdf",
                Content = new FileDownloadInfo { UniqueId = "odsp-unique-id", FileType = "pdf" },
            }
        ]);

        Conversation conversation = new("conv-1");
        conversation.Properties["conversationType"] = JsonSerializer.SerializeToElement("personal");
        conversation.Properties["tenantId"] = JsonSerializer.SerializeToElement("activity-tenant");
        core.Conversation = conversation;

        core.Recipient = agenticRecipient
            ? new ChannelAccount
            {
                Id = "bot-1",
                Name = "Test Bot",
                AgenticAppId = "agent-app",
                AgenticUserId = "31e29ddb-e4ce-427e-8bda-1a37eb12d43f",
                AgenticAppBlueprintId = "blueprint-id",
                TenantId = "tenant-id",
            }
            : new ChannelAccount { Id = "bot-1", Name = "Test Bot" };

        return MessageActivity.FromActivity(core);
    }

    private static async Task<GraphCredential?> CredentialOnFirstFileAsync(bool agenticRecipient, Uri? graphBaseUrl = null)
    {
        Context<MessageActivity> context = new(BuildApp(graphBaseUrl), ActivityWith(agenticRecipient));
        IncomingFile file = Assert.Single(await context.Files.ListAsync());

        return file.Credential;
    }

    [Fact]
    public async Task GivesFilesAnAgenticCredential_WhenTheInboundActivityCarriesAnAgenticUser()
    {
        GraphCredential? credential = await CredentialOnFirstFileAsync(agenticRecipient: true);

        Assert.NotNull(credential);
        Assert.Equal(FileActor.AgenticUser, credential.Actor);
    }

    [Fact]
    public async Task GivesFilesAnAppCredential_WhenTheInboundActivityHasNoAgenticUser()
    {
        GraphCredential? credential = await CredentialOnFirstFileAsync(agenticRecipient: false);

        Assert.NotNull(credential);
        Assert.Equal(FileActor.App, credential.Actor);
    }

    [Fact]
    public async Task CarriesTheConfiguredGraphHostOntoTheCredential()
    {
        // The host and the token travel together, so a sovereign deployment cannot end up authenticating against one cloud and addressing another.
        GraphCredential? credential = await CredentialOnFirstFileAsync(agenticRecipient: true, new Uri("https://graph.microsoft.us"));

        Assert.Equal(new Uri("https://graph.microsoft.us"), credential!.BaseUrlRoot);
    }

    [Theory]
    [InlineData(null, "https://graph.microsoft.com/.default")]
    [InlineData("https://graph.microsoft.us", "https://graph.microsoft.us/.default")]
    public async Task DerivesTheGraphScopeFromTheConfiguredHost(string? graphBaseUrl, string expectedScope)
    {
        // Derived rather than configured separately, so the host the request is addressed to and the audience the token is minted for cannot disagree.
        RecordingTokenProvider provider = new();
        Context<MessageActivity> context = new(
            BuildApp(graphBaseUrl is null ? null : new Uri(graphBaseUrl), provider),
            ActivityWith(agenticRecipient: false));

        IncomingFile file = Assert.Single(await context.Files.ListAsync());
        await file.Credential!.GetTokenAsync();

        Assert.Equal(expectedScope, Assert.Single(provider.Scopes));
    }

    [Fact]
    public async Task AcquiresTheAppTokenInTheActivitysTenant_NotTheApps()
    {
        // A multi-tenant app's own tenant cannot read a file that lives in the tenant the message came from, and an
        // app-only token minted against `common` is refused outright.
        RecordingTokenProvider provider = new();
        Context<MessageActivity> context = new(BuildApp(tokenProvider: provider), ActivityWith(agenticRecipient: false));

        IncomingFile file = Assert.Single(await context.Files.ListAsync());
        await file.Credential!.GetTokenAsync();

        Assert.Equal("activity-tenant", Assert.Single(provider.Tenants));
    }

    [Fact]
    public async Task ReportsNoToken_WhenTheAppWasBuiltWithoutATokenProvider()
    {
        // Constructed outside the hosting extensions, so there is nothing to acquire with. This must degrade to "no
        // credential", which surfaces as a named failure before any request, rather than throwing at fetch time.
        GraphCredential? credential = await CredentialOnFirstFileAsync(agenticRecipient: false);

        Assert.Null(await credential!.GetTokenAsync());
    }
}
