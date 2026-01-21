using System.Text.Json.Serialization;

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Apps.Clients;

public class ApiClient(ConversationClient conversationClient)
{
    public ConversationClient Conversations = conversationClient;
}
public class ConversationClient(ActivityClient activityClient)
{
    public ActivityClient Activities = activityClient;
}

public class ActivityClient(Bot.Core.ConversationClient conversationClient)
{
    private readonly Bot.Core.ConversationClient _conversationClient = conversationClient;

    public async Task<Resource?> CreateAsync(string conversationId, TeamsActivity activity, CancellationToken cancellationToken)
    {
        SendActivityResponse resp = await _conversationClient.SendActivityAsync(activity, null, cancellationToken);

        Resource resourceResponse = new()
        {
            Id = resp.Id ?? throw new InvalidOperationException("Response JSON does not contain an 'id' property.")
        };
        return resourceResponse;
    }
}


/// <summary>
/// A response containing a resource ID
/// </summary>
public class Resource
{
    /// <summary>
    /// Id of the resource
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }
}
