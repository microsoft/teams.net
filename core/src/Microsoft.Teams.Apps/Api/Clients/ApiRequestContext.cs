// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Api.Clients;

internal static class ApiRequestContext
{
    public static BotRequestContext? Resolve(BotRequestContext? defaultRequestContext, AgenticIdentity? agenticIdentity)
        => BotRequestContext.Merge(defaultRequestContext, BotRequestContext.FromAgenticIdentity(agenticIdentity));

    public static BotRequestContext? ResolveActivity(BotRequestContext? defaultRequestContext, AgenticIdentity? agenticIdentity, CoreActivity? activity)
        => BotRequestContext.Merge(Resolve(defaultRequestContext, agenticIdentity), BotRequestContext.FromActivity(activity));

    public static BotRequestOptions? CreateOptions(BotRequestContext? defaultRequestContext, AgenticIdentity? agenticIdentity)
    {
        BotRequestContext? requestContext = Resolve(defaultRequestContext, agenticIdentity);
        return requestContext is null ? null : new BotRequestOptions { RequestContext = requestContext };
    }
}
