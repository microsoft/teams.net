// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Api.Clients;

internal static class ApiRequestContext
{
    public static BotRequestContext? Resolve(AgenticIdentity? defaultAgenticIdentity, AgenticIdentity? agenticIdentity)
        => BotRequestContext.FromAgenticIdentity(agenticIdentity ?? defaultAgenticIdentity);

    public static BotRequestContext? ResolveActivity(AgenticIdentity? defaultAgenticIdentity, AgenticIdentity? agenticIdentity, CoreActivity? activity)
        => BotRequestContext.Merge(Resolve(defaultAgenticIdentity, agenticIdentity), BotRequestContext.FromActivity(activity));

    public static BotRequestOptions? CreateOptions(AgenticIdentity? defaultAgenticIdentity, AgenticIdentity? agenticIdentity)
    {
        BotRequestContext? requestContext = Resolve(defaultAgenticIdentity, agenticIdentity);
        return requestContext is null ? null : new BotRequestOptions { RequestContext = requestContext };
    }
}
