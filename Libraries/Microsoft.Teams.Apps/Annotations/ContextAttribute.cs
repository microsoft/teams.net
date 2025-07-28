// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Extensions;
using Microsoft.Teams.Common.Logging;
using Microsoft.Teams.Common.Storage;

namespace Microsoft.Teams.Apps.Annotations;

[AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
public class ContextAttribute : ContextAccessorAttribute
{
    public override object? GetValue(IContext<IActivity> context, ParameterInfo parameter)
    {
        var type = parameter.ParameterType;

        if (type == typeof(ILogger)) return context.Log;
        if (type == typeof(IStorage<string, object>)) return context.Storage;
        if (type == typeof(IStreamer)) return context.Stream;
        if (type.IsAssignableTo(typeof(IActivity))) return context.Activity.ToType(parameter.ParameterType, null);
        if (type == typeof(ApiClient)) return context.Api;
        if (type == typeof(CancellationToken)) return context.CancellationToken;
        if (type == typeof(ConversationReference)) return context.Ref;
        if (type == typeof(IContext.Client)) return new IContext.Client(context);
        if (type == typeof(IContext.Next)) return new IContext.Next(context.Next);
        if (type == typeof(JsonWebToken)) return context.UserGraphToken;
        return context;
    }
}