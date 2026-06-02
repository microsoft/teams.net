// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class StateWiringTests
{
    [Fact]
    public void UseState_RegistersStateMiddlewareInPipeline()
    {
        var storage = new MemoryStorage();
        TeamsBotApplication app = CreateApp(o => o.UseState(storage));

        Assert.Contains(app.MiddleWare, m => m is StateMiddleware);
    }

    [Fact]
    public void WithoutUseState_NoStateMiddlewareRegistered()
    {
        TeamsBotApplication app = CreateApp();

        Assert.DoesNotContain(app.MiddleWare, m => m is StateMiddleware);
    }

    [Fact]
    public void Options_UseState_SetsStorage()
    {
        var storage = new MemoryStorage();
        var options = new TeamsBotApplicationOptions();

        TeamsBotApplicationOptions returned = options.UseState(storage);

        Assert.Same(options, returned);
        Assert.Same(storage, options.StateStorage);
    }

    [Fact]
    public void Options_UseState_ThrowsOnNull()
        => Assert.Throws<ArgumentNullException>(() => new TeamsBotApplicationOptions().UseState(null!));

    [Fact]
    public void AppBuilder_UseState_SetsStorageOnOptions()
    {
        var storage = new MemoryStorage();

        AppBuilder builder = App.Builder().UseState(storage);

        Assert.Same(storage, builder.Options.StateStorage);
    }

    private static TeamsBotApplication CreateApp(Action<TeamsBotApplicationOptions>? configure = null)
    {
        Mock<UserTokenClient> mockUserTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);

        Mock<ConversationClient> mockConversationClient = new(
            new HttpClient(),
            NullLogger<ConversationClient>.Instance);

        ApiClient apiClient = new(
            new HttpClient(),
            mockConversationClient.Object,
            mockUserTokenClient.Object);

        var options = new TeamsBotApplicationOptions { AppId = "test-app-id" };
        configure?.Invoke(options);

        return new TeamsBotApplication(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            options);
    }
}
