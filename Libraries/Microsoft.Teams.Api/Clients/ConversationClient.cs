// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Common.Http;

using IHttpClientFactory = Microsoft.Teams.Common.Http.IHttpClientFactory;
namespace Microsoft.Teams.Api.Clients;

public class ConversationClient : Client
{
    public readonly string ServiceUrl;
    public readonly ActivityClient Activities;
    public readonly MemberClient Members;

    public ConversationClient(string serviceUrl, string scope, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Activities = new ActivityClient(serviceUrl, _http, scope, cancellationToken);
        Members = new MemberClient(serviceUrl, _http, scope, cancellationToken);
    }

    public ConversationClient(string serviceUrl, IHttpClient client, string scope, CancellationToken cancellationToken = default) : base(client, scope, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Activities = new ActivityClient(serviceUrl, _http, scope, cancellationToken);
        Members = new MemberClient(serviceUrl, _http, scope, cancellationToken);
    }

    public ConversationClient(string serviceUrl, IHttpClientOptions options, string scope, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Activities = new ActivityClient(serviceUrl, _http, scope, cancellationToken);
        Members = new MemberClient(serviceUrl, _http, scope, cancellationToken);
    }

    public ConversationClient(string serviceUrl, IHttpClientFactory factory, string scope, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Activities = new ActivityClient(serviceUrl, _http, scope, cancellationToken);
        Members = new MemberClient(serviceUrl, _http, scope, cancellationToken);
    }

    public async Task<ConversationResource> CreateAsync(CreateRequest request)
    {
        var req = HttpRequest.Post($"{ServiceUrl}v3/conversations", body: request);
        var res = await _http.SendAsync<ConversationResource>(req, null, _cancellationToken);
        return res.Body;
    }

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
}