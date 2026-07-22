// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

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
        router.Register(MakeRoute<InvokeActivity>($"{TeamsActivityTypes.Invoke}/{InvokeNames.AdaptiveCardAction}"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(()
            => router.Register(MakeRoute<InvokeActivity>(TeamsActivityTypes.Invoke)));

        Assert.Contains("catch-all", ex.Message);
    }

    [Fact]
    public void Register_SpecificInvokeAfterCatchAll_Throws()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<InvokeActivity>(TeamsActivityTypes.Invoke));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(()
            => router.Register(MakeRoute<InvokeActivity>($"{TeamsActivityTypes.Invoke}/{InvokeNames.TaskFetch}")));

        Assert.Contains("invoke", ex.Message);
    }

    [Fact]
    public void Register_MultipleCatchAllInvokes_ThrowsDuplicateName()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<InvokeActivity>(TeamsActivityTypes.Invoke));

        Assert.Throws<InvalidOperationException>(()
            => router.Register(MakeRoute<InvokeActivity>(TeamsActivityTypes.Invoke)));
    }

    [Fact]
    public void Register_MultipleSpecificInvokeHandlers_Succeeds()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<InvokeActivity>($"{TeamsActivityTypes.Invoke}/{InvokeNames.AdaptiveCardAction}"));
        router.Register(MakeRoute<InvokeActivity>($"{TeamsActivityTypes.Invoke}/{InvokeNames.TaskFetch}"));
        router.Register(MakeRoute<InvokeActivity>($"{TeamsActivityTypes.Invoke}/{InvokeNames.TaskSubmit}"));

        Assert.Equal(3, router.GetRoutes().Count);
    }

    // ==================== Non-invoke catch-all + specific is allowed ====================

    [Fact]
    public void Register_ConversationUpdateCatchAllAndSpecific_Succeeds()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<ConversationUpdateActivity>(TeamsActivityTypes.ConversationUpdate));
        router.Register(MakeRoute<ConversationUpdateActivity>($"{TeamsActivityTypes.ConversationUpdate}/membersAdded"));

        Assert.Equal(2, router.GetRoutes().Count);
    }

    [Fact]
    public void Register_InstallUpdateCatchAllAndSpecific_Succeeds()
    {
        Router router = new(NullLogger.Instance);
        router.Register(MakeRoute<InstallUpdateActivity>(TeamsActivityTypes.InstallationUpdate));
        router.Register(MakeRoute<InstallUpdateActivity>($"{TeamsActivityTypes.InstallationUpdate}/add"));
        router.Register(MakeRoute<InstallUpdateActivity>($"{TeamsActivityTypes.InstallationUpdate}/remove"));

        Assert.Equal(3, router.GetRoutes().Count);
    }
}
