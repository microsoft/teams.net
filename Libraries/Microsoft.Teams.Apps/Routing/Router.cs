// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

public interface IRouter
{
    public int Length { get; }

    public IList<IRoute> Select(IActivity activity);
    public IRouter Register(IRoute route);
    public IRouter Register(Func<IContext<IActivity>, Task<object?>> handler);
    public IRouter Register(string name, Func<IContext<IActivity>, Task<object?>> handler);
}

public class Router : IRouter
{
    public int Length { get => _routes.Count; }

    protected readonly List<IRoute> _routes = [];

    public IList<IRoute> Select(IActivity activity)
    {
        return _routes
            .Where(route => route.Select(activity))
            .ToList();
    }

    public IRouter Register(IRoute route)
    {
        _routes.Add(route);
        return this;
    }

    public IRouter Register(Func<IContext<IActivity>, Task<object?>> handler)
    {
        return Register(new Route()
        {
            Selector = _ => true,
            Handler = handler
        });
    }

    public IRouter Register(string? name, Func<IContext<IActivity>, Task<object?>> handler)
    {
        return Register(new Route()
        {
            Name = name,
            Handler = handler,
            Selector = (activity) =>
            {
                if (name is null || name == "activity") return true;
                if (activity.Type.Equals(name)) return true;
                return false;
            }
        });
    }
}