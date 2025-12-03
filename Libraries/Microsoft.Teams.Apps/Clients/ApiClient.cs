

using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Clients;


public class CreateRequest
{
    [JsonPropertyName("isGroup")]
    [JsonPropertyOrder(0)]
    public bool? IsGroup { get; set; }

    [JsonPropertyName("bot")]
    [JsonPropertyOrder(1)]
    public Account? Bot { get; set; }

    [JsonPropertyName("members")]
    [JsonPropertyOrder(2)]
    public IList<Account>? Members { get; set; }

    [JsonPropertyName("topicName")]
    [JsonPropertyOrder(3)]
    public string? TopicName { get; set; }

    [JsonPropertyName("tenantId")]
    [JsonPropertyOrder(4)]
    public string? TenantId { get; set; }

    [JsonPropertyName("activity")]
    [JsonPropertyOrder(5)]
    public IActivity? Activity { get; set; }

    [JsonPropertyName("channelData")]
    [JsonPropertyOrder(6)]
    public IDictionary<string, object>? ChannelData { get; set; }
}


public class ConversationResource
{
    /// <summary>
    /// Id of the resource
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// ID of the Activity (if sent)
    /// </summary>
    [JsonPropertyName("activityId")]
    [JsonPropertyOrder(1)]
    public string? ActivityId { get; set; }

    /// <summary>
    /// Service endpoint where operations concerning the conversation may be performed
    /// </summary>
    [JsonPropertyName("serviceUrl")]
    [JsonPropertyOrder(2)]
    public string? ServiceUrl { get; set; }

    public void Deconstruct(out string id, out string? activityId, out string? serviceUrl)
    {
        id = Id;
        activityId = ActivityId;
        serviceUrl = ServiceUrl;
    }
}

public class ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
{
    internal async Task<ConversationResource> ConversationsCreateAsync(CreateRequest value)
    {
        logger.LogInformation(httpClient.Timeout.ToString());
        throw new NotImplementedException();
    }

    internal async Task<Api.Token.Response> UsersTokenExchangeAsync(object value)
    {
        throw new NotImplementedException();
    }
}
