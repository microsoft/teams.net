// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// Verifies that the conversation update handler registrations wire up routes whose
/// selectors match the correct <see cref="ConversationEventType"/>, and only that type.
/// </summary>
public class ConversationUpdateHandlerTests
{
    /// <summary>
    /// Every specific conversation update registration, paired with the event type it should match.
    /// </summary>
    public static TheoryData<string, ConversationEventType> Registrations => new()
    {
        { nameof(ConversationUpdateExtensions.OnChannelCreated), ConversationEventTypes.ChannelCreated },
        { nameof(ConversationUpdateExtensions.OnChannelDeleted), ConversationEventTypes.ChannelDeleted },
        { nameof(ConversationUpdateExtensions.OnChannelRenamed), ConversationEventTypes.ChannelRenamed },
        { nameof(ConversationUpdateExtensions.OnChannelRestored), ConversationEventTypes.ChannelRestored },
        { nameof(ConversationUpdateExtensions.OnChannelShared), ConversationEventTypes.ChannelShared },
        { nameof(ConversationUpdateExtensions.OnChannelUnshared), ConversationEventTypes.ChannelUnShared },
        { nameof(ConversationUpdateExtensions.OnChannelMemberAdded), ConversationEventTypes.ChannelMemberAdded },
        { nameof(ConversationUpdateExtensions.OnChannelMemberRemoved), ConversationEventTypes.ChannelMemberRemoved },
        { nameof(ConversationUpdateExtensions.OnTeamMemberAdded), ConversationEventTypes.TeamMemberAdded },
        { nameof(ConversationUpdateExtensions.OnTeamMemberRemoved), ConversationEventTypes.TeamMemberRemoved },
        { nameof(ConversationUpdateExtensions.OnTeamArchived), ConversationEventTypes.TeamArchived },
        { nameof(ConversationUpdateExtensions.OnTeamDeleted), ConversationEventTypes.TeamDeleted },
        { nameof(ConversationUpdateExtensions.OnTeamRenamed), ConversationEventTypes.TeamRenamed },
        { nameof(ConversationUpdateExtensions.OnTeamRestored), ConversationEventTypes.TeamRestored },
        { nameof(ConversationUpdateExtensions.OnTeamUnarchived), ConversationEventTypes.TeamUnarchived },
    };

    [Theory]
    [MemberData(nameof(Registrations))]
    public async Task Handler_IsInvoked_ForMatchingEventType(string registration, ConversationEventType eventType)
    {
        TeamsBotApplication app = CreateApp();
        bool invoked = false;

        Register(app, registration, (_, _) =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        await app.Router.DispatchAsync(CreateContext(app, eventType));

        Assert.True(invoked, $"{registration} did not run for '{eventType}'.");
    }

    [Theory]
    [MemberData(nameof(Registrations))]
    public async Task Handler_IsNotInvoked_ForOtherEventTypes(string registration, ConversationEventType eventType)
    {
        foreach (ConversationEventType other in Registrations
            .Select(row => (ConversationEventType)row[1])
            .Where(type => !type.Equals(eventType)))
        {
            TeamsBotApplication app = CreateApp();
            bool invoked = false;

            Register(app, registration, (_, _) =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            await app.Router.DispatchAsync(CreateContext(app, other));

            Assert.False(invoked, $"{registration} ran for '{other}' but should only run for '{eventType}'.");
        }
    }

    [Theory]
    [MemberData(nameof(Registrations))]
    public async Task Handler_IsNotInvoked_WhenChannelDataIsMissing(string registration, ConversationEventType eventType)
    {
        TeamsBotApplication app = CreateApp();
        bool invoked = false;

        Register(app, registration, (_, _) =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        // No ChannelData at all; the selector must null-check rather than throw.
        await app.Router.DispatchAsync(new Context<TeamsActivity>(app, new ConversationUpdateActivity()));

        Assert.False(invoked, $"{registration} ran for '{eventType}' without any channel data.");
    }

    [Fact]
    public async Task OnChannelRestored_ReceivesChannelDetails()
    {
        TeamsBotApplication app = CreateApp();
        string? channelName = null;

        app.OnChannelRestored((ctx, _) =>
        {
            channelName = ctx.Activity.ChannelData?.Channel?.Name;
            return Task.CompletedTask;
        });

        Context<TeamsActivity> context = CreateContext(app, ConversationEventTypes.ChannelRestored);
        context.Activity.ChannelData!.Channel = new TeamsChannel { Name = "restored-channel" };

        await app.Router.DispatchAsync(context);

        Assert.Equal("restored-channel", channelName);
    }

    [Fact]
    public async Task OnTeamRestored_ReceivesTeamDetails()
    {
        TeamsBotApplication app = CreateApp();
        string? teamName = null;

        app.OnTeamRestored((ctx, _) =>
        {
            teamName = ctx.Activity.ChannelData?.Team?.Name;
            return Task.CompletedTask;
        });

        Context<TeamsActivity> context = CreateContext(app, ConversationEventTypes.TeamRestored);
        context.Activity.ChannelData!.Team = new Team { Name = "restored-team" };

        await app.Router.DispatchAsync(context);

        Assert.Equal("restored-team", teamName);
    }

    [Fact]
    public async Task OnConversationUpdate_AlsoRuns_ForSpecificEventTypes()
    {
        TeamsBotApplication app = CreateApp();
        bool generic = false;
        bool specific = false;

        app.OnConversationUpdate((_, _) =>
        {
            generic = true;
            return Task.CompletedTask;
        });

        app.OnChannelRestored((_, _) =>
        {
            specific = true;
            return Task.CompletedTask;
        });

        await app.Router.DispatchAsync(CreateContext(app, ConversationEventTypes.ChannelRestored));

        Assert.True(generic, "The catch-all conversationUpdate handler should also run.");
        Assert.True(specific, "The channelRestored handler should run.");
    }

    private static void Register(TeamsBotApplication app, string registration, ConversationUpdateHandler handler)
    {
        typeof(ConversationUpdateExtensions)
            .GetMethod(registration, [typeof(TeamsBotApplication), typeof(ConversationUpdateHandler)])!
            .Invoke(null, [app, handler]);
    }

    private static Context<TeamsActivity> CreateContext(TeamsBotApplication app, ConversationEventType eventType)
    {
        ConversationUpdateActivity activity = new()
        {
            ChannelData = new TeamsChannelData { EventType = eventType }
        };

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
