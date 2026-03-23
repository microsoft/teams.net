// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class RouterTests
{
    private static Route<TActivity> MakeRoute<TActivity>(string name) where TActivity : TeamsActivity
        => new() { Name = name, Selector = _ => true };

    // ==================== Duplicate name ====================

    [Fact]
    public void Register_DuplicateName_Throws()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<MessageActivity>("Message"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(()
            => router.Register(MakeRoute<MessageActivity>("Message")));

        Assert.Contains("Message", ex.Message);
    }

    [Fact]
    public void Register_UniqueNames_Succeeds()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<MessageActivity>("Message/hello"));
        router.Register(MakeRoute<MessageActivity>("Message/bye"));

        Assert.Equal(2, router.GetRoutes().Count);
    }

    // ==================== Invoke conflict ====================

    [Fact]
    public void Register_CatchAllInvokeAfterSpecific_Throws()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<InvokeActivity>($"{TeamsActivityType.Invoke}/{InvokeNames.AdaptiveCardAction}"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(()
            => router.Register(MakeRoute<InvokeActivity>(TeamsActivityType.Invoke)));

        Assert.Contains("catch-all", ex.Message);
    }

    [Fact]
    public void Register_SpecificInvokeAfterCatchAll_Throws()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<InvokeActivity>(TeamsActivityType.Invoke));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(()
            => router.Register(MakeRoute<InvokeActivity>($"{TeamsActivityType.Invoke}/{InvokeNames.TaskFetch}")));

        Assert.Contains("invoke", ex.Message);
    }

    [Fact]
    public void Register_MultipleCatchAllInvokes_ThrowsDuplicateName()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<InvokeActivity>(TeamsActivityType.Invoke));

        Assert.Throws<InvalidOperationException>(()
            => router.Register(MakeRoute<InvokeActivity>(TeamsActivityType.Invoke)));
    }

    [Fact]
    public void Register_MultipleSpecificInvokeHandlers_Succeeds()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<InvokeActivity>($"{TeamsActivityType.Invoke}/{InvokeNames.AdaptiveCardAction}"));
        router.Register(MakeRoute<InvokeActivity>($"{TeamsActivityType.Invoke}/{InvokeNames.TaskFetch}"));
        router.Register(MakeRoute<InvokeActivity>($"{TeamsActivityType.Invoke}/{InvokeNames.TaskSubmit}"));

        Assert.Equal(3, router.GetRoutes().Count);
    }

    // ==================== Non-invoke catch-all + specific is allowed ====================

    [Fact]
    public void Register_ConversationUpdateCatchAllAndSpecific_Succeeds()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<ConversationUpdateActivity>(TeamsActivityType.ConversationUpdate));
        router.Register(MakeRoute<ConversationUpdateActivity>($"{TeamsActivityType.ConversationUpdate}/membersAdded"));

        Assert.Equal(2, router.GetRoutes().Count);
    }

    [Fact]
    public void Register_InstallUpdateCatchAllAndSpecific_Succeeds()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<InstallUpdateActivity>(TeamsActivityType.InstallationUpdate));
        router.Register(MakeRoute<InstallUpdateActivity>($"{TeamsActivityType.InstallationUpdate}/add"));
        router.Register(MakeRoute<InstallUpdateActivity>($"{TeamsActivityType.InstallationUpdate}/remove"));

        Assert.Equal(3, router.GetRoutes().Count);
    }
}
