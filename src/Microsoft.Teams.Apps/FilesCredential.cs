// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Chooses which identity reads a file's bytes, for the current inbound activity.
/// </summary>
internal static class FilesCredential
{
    /// <summary>
    /// Choose which identity reads a file's bytes, for the current inbound activity.
    /// <para>An Agentic User reads as itself, never the app token: an app-only token sees what the app may read tenant-wide, a different set from what was shared with the agent, so it would 403 on exactly the files the agent was given.</para>
    /// <para>The token is resolved lazily, so a turn that never touches files never acquires one.</para>
    /// <para>The actor branch here is the same three-way split the routed token pattern in <c>samples/PABot</c> performs on a <see cref="DelegatingHandler"/>. It is done per turn and passed down rather than stamped on the request, because a handler authenticates every request on its client and the pre-authorized download URL must carry no credential at all.</para>
    /// </summary>
    /// <param name="agenticIdentity">The agentic identity on the inbound activity's recipient, or <c>null</c> for an ordinary bot.</param>
    /// <param name="graphBaseUrlRoot">Graph host root, carried on the credential so a new code path cannot wire the token through and forget its destination.</param>
    /// <param name="getAppGraphToken">Acquires a Graph token for the app, or <c>null</c> when the app has no credentials.</param>
    /// <param name="getAgenticGraphToken">Acquires a Graph token for an Agentic User, or <c>null</c> when the app has no credentials or the identity is not user-backed.</param>
    internal static GraphCredential Select(
        AgenticIdentity? agenticIdentity,
        Uri? graphBaseUrlRoot,
        Func<CancellationToken, Task<string?>> getAppGraphToken,
        Func<AgenticIdentity, CancellationToken, Task<string?>> getAgenticGraphToken)
    {
        // Only the actor and how its token is fetched vary. Building the rest here means a new actor is one arm
        // rather than a third copy of the whole credential, and cannot silently omit the host root.
        GraphCredential As(FileActor actor, Func<CancellationToken, Task<string?>> token)
            => new(actor, token, graphBaseUrlRoot);

        if (agenticIdentity is not null)
        {
            return As(FileActor.AgenticUser, ct => getAgenticGraphToken(agenticIdentity, ct));
        }

        // No tenant parameter is threaded through this function: the caller closes over whichever tenant it wants,
        // and Microsoft.Identity.Web resolves the authority from the named MSAL options when none is supplied.
        return As(FileActor.App, getAppGraphToken);
    }
}
