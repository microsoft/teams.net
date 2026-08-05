// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Moq;

namespace Microsoft.Teams.Apps.BotBuilder.UnitTests;

public class CompatUserTokenClientTests
{
    [Fact]
    public async Task DelegatedMethods_PassRequestContextToCoreClient()
    {
        BotRequestContext requestContext = new() { BotAppId = "bot-app-id" };
        Mock<UserTokenClient> coreUserTokenClient = CreateMockUserTokenClient();
        CompatUserTokenClient compatUserTokenClient = new(coreUserTokenClient.Object) { RequestContext = requestContext };
        string[] resourceUrls = ["https://graph.microsoft.com"];

        coreUserTokenClient
            .Setup(c => c.GetTokenStatusAsync(
                "user-id",
                "msteams",
                "include",
                It.Is<BotRequestContext?>(c => ReferenceEquals(c, requestContext)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GetTokenStatusResult { ConnectionName = "connection", HasToken = true }]);
        coreUserTokenClient
            .Setup(c => c.GetTokenAsync(
                "user-id",
                "connection",
                "msteams",
                "magic-code",
                It.Is<BotRequestContext?>(c => ReferenceEquals(c, requestContext)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTokenResult { ConnectionName = "connection", Token = "token" });
        coreUserTokenClient
            .Setup(c => c.GetSignInResourceAsync(
                "user-id",
                "connection",
                "msteams",
                "https://redirect.example.com",
                It.Is<BotRequestContext?>(c => ReferenceEquals(c, requestContext)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSignInResourceResult { SignInLink = "https://signin.example.com" });
        coreUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(
                "user-id",
                "connection",
                "msteams",
                "exchange-token",
                It.Is<BotRequestContext?>(c => ReferenceEquals(c, requestContext)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTokenResult { ConnectionName = "connection", Token = "exchanged-token" });
        coreUserTokenClient
            .Setup(c => c.SignOutUserAsync(
                "user-id",
                "connection",
                "msteams",
                It.Is<BotRequestContext?>(c => ReferenceEquals(c, requestContext)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        coreUserTokenClient
            .Setup(c => c.GetAadTokensAsync(
                "user-id",
                "connection",
                "msteams",
                resourceUrls,
                It.Is<BotRequestContext?>(c => ReferenceEquals(c, requestContext)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTokenResult>
            {
                ["https://graph.microsoft.com"] = new() { ConnectionName = "connection", Token = "aad-token" }
            });

        await compatUserTokenClient.GetTokenStatusAsync("user-id", "msteams", "include", CancellationToken.None);
        await compatUserTokenClient.GetUserTokenAsync("user-id", "connection", "msteams", "magic-code", CancellationToken.None);
        await compatUserTokenClient.GetSignInResourceAsync(
            "connection",
            new Activity
            {
                From = new ChannelAccount { Id = "user-id" },
                ChannelId = "msteams"
            },
            "https://redirect.example.com",
            CancellationToken.None);
        await compatUserTokenClient.ExchangeTokenAsync(
            "user-id",
            "connection",
            "msteams",
            new TokenExchangeRequest { Token = "exchange-token" },
            CancellationToken.None);
        await compatUserTokenClient.SignOutUserAsync("user-id", "connection", "msteams", CancellationToken.None);
        await compatUserTokenClient.GetAadTokensAsync("user-id", "connection", resourceUrls, "msteams", CancellationToken.None);

        coreUserTokenClient.VerifyAll();
    }

    private static Mock<UserTokenClient> CreateMockUserTokenClient()
    {
        Mock<IConfiguration> configuration = new();
        configuration.Setup(c => c["UserTokenApiEndpoint"]).Returns("https://token.botframework.com");
        return new Mock<UserTokenClient>(
            new HttpClient(),
            configuration.Object,
            NullLogger<UserTokenClient>.Instance);
    }
}
