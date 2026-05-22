// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Bundles the dependencies required to construct a <see cref="TeamsBotApplication"/>.
/// Pass a single <c>TeamsBotApplicationDependencies</c> to the base constructor when subclassing,
/// so derived types do not need to thread every dependency by hand.
/// </summary>
/// <example>
/// <code>
/// public class MyBot : TeamsBotApplication
/// {
///     public MyBot(TeamsBotApplicationDependencies deps) : base(deps) { }
/// }
/// </code>
/// </example>
public sealed record TeamsBotApplicationDependencies(
    ConversationClient ConversationClient,
    UserTokenClient UserTokenClient,
    ApiClient TeamsApiClient,
    IHttpContextAccessor HttpContextAccessor,
    ILogger<TeamsBotApplication> Logger,
    BotApplicationOptions? Options = null,
    TeamsBotApplicationOptions? TeamsOptions = null);
