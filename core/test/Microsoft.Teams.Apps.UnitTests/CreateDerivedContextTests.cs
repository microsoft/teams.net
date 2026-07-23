// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// Unit tests for <see cref="Context{TActivity}.CreateDerivedContext()"/>
/// and <see cref="Context{TActivity}.CreateDerivedContext{TNew}(TNew)"/>.
/// </summary>
public class CreateDerivedContextTests
{
    // ===== Parameterless overload =====

    [Fact]
    public void Parameterless_PreservesBotApplication()
    {
        TeamsBotApplication app = CreateApp();
        Context<TeamsActivity> source = CreateContext(app);

        Context<TeamsActivity> derived = source.CreateDerivedContext();

        Assert.Same(app, derived.TeamsBotApplication);
    }

    [Fact]
    public void Parameterless_PreservesActivity()
    {
        TeamsBotApplication app = CreateApp();
        InvokeActivity activity = new() { Type = TeamsActivityTypes.Invoke, Name = "test" };
        Context<TeamsActivity> source = new(app, activity);

        Context<TeamsActivity> derived = source.CreateDerivedContext();

        Assert.Same(activity, derived.Activity);
    }

    [Fact]
    public void Parameterless_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        Context<TeamsActivity> source = CreateContext(app);
        TurnStateContainer state = CreateState();
        source.State = state;

        Context<TeamsActivity> derived = source.CreateDerivedContext();

        Assert.Same(state, derived.State);
    }

    [Fact]
    public void Parameterless_WithoutState_DoesNotSetState()
    {
        TeamsBotApplication app = CreateApp();
        Context<TeamsActivity> source = CreateContext(app);

        Context<TeamsActivity> derived = source.CreateDerivedContext();

        Assert.False(derived.HasState);
    }

    // ===== Typed overload =====

    [Fact]
    public void Typed_PreservesBotApplication()
    {
        TeamsBotApplication app = CreateApp();
        Context<TeamsActivity> source = CreateContext(app);
        InvokeActivity typedActivity = new() { Type = TeamsActivityTypes.Invoke, Name = "test" };

        Context<InvokeActivity> derived = source.CreateDerivedContext(typedActivity);

        Assert.Same(app, derived.TeamsBotApplication);
    }

    [Fact]
    public void Typed_UsesNewActivity()
    {
        TeamsBotApplication app = CreateApp();
        Context<TeamsActivity> source = CreateContext(app);
        InvokeActivity typedActivity = new() { Type = TeamsActivityTypes.Invoke, Name = "test" };

        Context<InvokeActivity> derived = source.CreateDerivedContext(typedActivity);

        Assert.Same(typedActivity, derived.Activity);
        Assert.NotSame(source.Activity, derived.Activity);
    }

    [Fact]
    public void Typed_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        Context<TeamsActivity> source = CreateContext(app);
        TurnStateContainer state = CreateState();
        source.State = state;
        InvokeActivity typedActivity = new() { Type = TeamsActivityTypes.Invoke, Name = "test" };

        Context<InvokeActivity> derived = source.CreateDerivedContext(typedActivity);

        Assert.Same(state, derived.State);
    }

    [Fact]
    public void Typed_WithoutState_DoesNotSetState()
    {
        TeamsBotApplication app = CreateApp();
        Context<TeamsActivity> source = CreateContext(app);
        InvokeActivity typedActivity = new() { Type = TeamsActivityTypes.Invoke, Name = "test" };

        Context<InvokeActivity> derived = source.CreateDerivedContext(typedActivity);

        Assert.False(derived.HasState);
    }

    [Fact]
    public void Typed_WidensActivityType()
    {
        TeamsBotApplication app = CreateApp();
        InvokeActivity invokeActivity = new() { Type = TeamsActivityTypes.Invoke, Name = "test" };
        Context<InvokeActivity> source = new(app, invokeActivity);
        TurnStateContainer state = CreateState();
        source.State = state;

        Context<TeamsActivity> derived = source.CreateDerivedContext((TeamsActivity)invokeActivity);

        Assert.Same(invokeActivity, derived.Activity);
        Assert.Same(state, derived.State);
    }

    // ===== Helpers =====

    private static TurnStateContainer CreateState()
    {
        TurnState convState = new();
        convState.Set("test-key", "test-value");
        return new TurnStateContainer(convState, new TurnState());
    }

    private static Context<TeamsActivity> CreateContext(TeamsBotApplication app)
    {
        TeamsActivity activity = new() { Type = TeamsActivityTypes.Message };
        return new Context<TeamsActivity>(app, activity);
    }

    private static TeamsBotApplication CreateApp()
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

        return new TeamsBotApplication(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });
    }
}
