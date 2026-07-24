// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.UnitTests;

public class UserTokenClientTests
{
    [Fact]
    public async Task GetTokenAsync_WithRequestContext_StampsRequestOptions()
    {
        CapturingHandler handler = new();
        UserTokenClient client = CreateClient(handler);
        AgenticUser identity = new()
        {
            AgenticAppInstanceId = "agent-app-instance",
            AgenticUserId = "agent-user",
            AgenticBlueprintId = "agent-identity-blueprint",
        };
        BotRequestContext requestContext = new()
        {
            AgenticUser = identity,
            BotAppId = "bot-app",
        };

        await client.GetTokenAsync("user", "connection", "msteams", code: null, requestContext);

        Assert.NotNull(handler.Request);
        Assert.True(handler.Request.Options.TryGetValue(new HttpRequestOptionsKey<object?>(BotRequestContext.AgenticUserKey), out object? identityValue));
        Assert.Same(identity, identityValue);
        Assert.True(handler.Request.Options.TryGetValue(new HttpRequestOptionsKey<object?>(BotRequestContext.BotAppIdKey), out object? botAppIdValue));
        Assert.Equal("bot-app", botAppIdValue);
    }

    private static UserTokenClient CreateClient(HttpMessageHandler handler)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UserTokenApiEndpoint"] = "https://token.test"
            })
            .Build();

        return new UserTokenClient(new HttpClient(handler), configuration, NullLogger<UserTokenClient>.Instance);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"connectionName":"connection","token":"token"}""", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
