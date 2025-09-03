using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Micosoft.Teams.Apps.Tests.Routing;

public class RouterTests
{
    private readonly Router _router;

    public RouterTests()
    {
        _router = new();
    }

    [Fact]
    public void Should_Register_Routes()
    {
        _router.Register(ActivityType.Message, ctx =>
        {
            return Task.FromResult<object?>(null);
        });

        _router.Register(new Route()
        {
            Name = ActivityType.Message,
            Selector = activity =>
            {
                if (activity is MessageActivity message)
                {
                    return message.Text == "hi";
                }

                return false;
            },
            Handler = ctx =>
            {
                return Task.FromResult<object?>(null);
            }
        });

        Assert.Single(_router.Select(new MessageActivity()));
        Assert.Equal(2, _router.Select(new MessageActivity("hi")).Count);
    }

    [Fact]
    public void Should_Override_System_Route()
    {
        _router.Register(new Route()
        {
            Name = ActivityType.Message,
            Type = RouteType.System,
            Selector = activity =>
            {
                if (activity is MessageActivity message)
                {
                    return message.Text == "hi";
                }

                return false;
            },
            Handler = ctx =>
            {
                return Task.FromResult<object?>(null);
            }
        });

        _router.Register(ActivityType.Message, ctx =>
        {
            return Task.FromResult<object?>(null);
        });

        Assert.Single(_router.Select(new MessageActivity()));
        Assert.Single(_router.Select(new MessageActivity("hi")));
    }
}