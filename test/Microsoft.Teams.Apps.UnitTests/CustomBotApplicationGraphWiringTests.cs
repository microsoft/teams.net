// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// That a custom <see cref="TeamsBotApplication"/> subclass still reaches Graph.
/// <para>A subclass forwards only the constructor arguments it declares, and the documented shape declares four, so
/// anything added to the base constructor later arrives as <c>null</c>. For the file path that is not a degraded
/// mode: every content-URL-only file would report no credential, which is the entire Agentic User scenario.</para>
/// </summary>
public class CustomBotApplicationGraphWiringTests
{
    /// <summary>The subclass shape the SDK documents on the <see cref="TeamsBotApplication"/> constructor.</summary>
    private sealed class CustomBot(
        ApiClient api,
        IHttpContextAccessor accessor,
        ILogger<TeamsBotApplication> logger,
        TeamsBotApplicationOptions? options = null)
        : TeamsBotApplication(api, accessor, logger, options)
    {
    }

    /// <summary>
    /// Fakes the Identity abstraction underneath <see cref="BotTokenProvider"/> rather than the provider itself, so
    /// the real provider runs and the wiring under test is the real wiring.
    /// </summary>
    private sealed class RecordingTokenProvider
    {
        public List<string> Scopes { get; } = [];

        public BotTokenProvider Provider { get; }

        public RecordingTokenProvider()
        {
            Mock<IAuthorizationHeaderProvider> header = new();

            header
                .Setup(h => h.CreateAuthorizationHeaderForAppAsync(It.IsAny<string>(), It.IsAny<AuthorizationHeaderProviderOptions>(), It.IsAny<CancellationToken>()))
                .Returns((string scope, AuthorizationHeaderProviderOptions _, CancellationToken __) =>
                {
                    Scopes.Add(scope);
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

    private static ServiceProvider BuildContainer<TApp>(RecordingTokenProvider provider) where TApp : TeamsBotApplication
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "graph-wiring-client-id",
                ["AzureAd:TenantId"] = "graph-wiring-tenant-id",
            })
            .Build());
        services.AddLogging();
        services.AddTeamsBotApplication<TApp>();

        // Replaces the real acquisition, which would need a live Entra tenant. Registered last so it wins over the
        // TryAddSingleton the hosting extensions perform.
        services.AddSingleton(provider.Provider);

        return services.BuildServiceProvider();
    }

    private static MessageActivity AgenticFileActivity()
    {
        CoreActivity core = new() { Type = TeamsActivityTypes.Message };

        core.Properties["attachments"] = JsonSerializer.SerializeToElement<IList<TeamsAttachment>>(
        [
            new TeamsAttachment
            {
                ContentType = AttachmentContentType.FileDownloadInfo,
                ContentUrl = new Uri("https://contoso.sharepoint.com/personal/a/Documents/report.pdf"),
                Name = "report.pdf",
                Content = new FileDownloadInfo { FileType = "pdf" },
            }
        ]);

        Conversation conversation = new("conv-1");
        conversation.Properties["conversationType"] = JsonSerializer.SerializeToElement("personal");
        core.Conversation = conversation;
        core.Recipient = new ChannelAccount
        {
            Id = "bot-1",
            AgenticAppId = "agent-app",
            AgenticUserId = "31e29ddb-e4ce-427e-8bda-1a37eb12d43f",
            TenantId = "tenant-id",
        };

        return MessageActivity.FromActivity(core);
    }

    private static async Task<string?> TokenThroughFilesAsync<TApp>() where TApp : TeamsBotApplication
    {
        RecordingTokenProvider provider = new();
        using ServiceProvider container = BuildContainer<TApp>(provider);

        Context<MessageActivity> context = new(container.GetRequiredService<TApp>(), AgenticFileActivity());
        IncomingFile file = Assert.Single(await context.Files.ListAsync());

        return await file.Credential!.GetTokenAsync();
    }

    [Fact]
    public async Task ACustomSubclassStillAcquiresAGraphToken()
        => Assert.Equal("agent-token", await TokenThroughFilesAsync<CustomBot>());

    [Fact]
    public async Task TheBuiltInApplicationAcquiresAGraphToken()
        => Assert.Equal("agent-token", await TokenThroughFilesAsync<TeamsBotApplication>());
}
