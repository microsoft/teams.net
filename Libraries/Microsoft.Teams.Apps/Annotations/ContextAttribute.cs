using System.Reflection;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Extensions;
using Microsoft.Teams.Common.Logging;
using Microsoft.Teams.Common.Storage;

namespace Microsoft.Teams.Apps.Annotations;

public class ContextAttribute : ContextAccessorAttribute
{
    public override object? GetValue(IContext<IActivity> context, ParameterInfo parameter)
    {
        var type = parameter.ParameterType;

        if (type is ILogger) return context.Log;
        if (type is IStorage<string, object>) return context.Storage;
        if (type is IStreamer) return context.Stream;
        if (type.IsAssignableTo(typeof(IActivity))) return context.Activity.ToType(parameter.ParameterType, null);
        if (type == typeof(ApiClient)) return context.Api;
        if (type == typeof(CancellationToken)) return context.CancellationToken;
        if (type == typeof(ConversationReference)) return context.Ref;
        if (type == typeof(IContext.Client)) return new IContext.Client(context);
        if (type == typeof(Graph.GraphServiceClient)) return context.UserGraph;
        if (type == typeof(IContext.Next)) return new IContext.Next(context.Next);
        return context;
    }
}