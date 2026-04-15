// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;
using CoreUserTokenClient = Microsoft.Teams.Bot.Core.UserTokenClient;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible root client that aggregates all Teams API sub-clients.
/// Use <see cref="ApiClientFactory"/> to create instances via dependency injection.
/// </summary>
public class ApiClient
{
    internal ApiClient(CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, TeamsApiClient teamsApiClient, Uri serviceUrl, AgenticIdentity? agenticIdentity = null)
    {
        ServiceUrl = serviceUrl;
        AgenticIdentity = agenticIdentity;
        Conversations = new ConversationClient(conversationClient, serviceUrl, agenticIdentity);
        Bots = new BotClient(userTokenClient);
        Users = new UserClient(userTokenClient);
        Teams = new TeamClient(teamsApiClient, serviceUrl, agenticIdentity);
        Meetings = new MeetingClient(teamsApiClient, serviceUrl, agenticIdentity);
    }

    public Uri ServiceUrl { get; }

    public AgenticIdentity? AgenticIdentity { get; set; }

    public ConversationClient Conversations { get; }

    public BotClient Bots { get; }

    public UserClient Users { get; }

    public TeamClient Teams { get; }

    public MeetingClient Meetings { get; }
}
