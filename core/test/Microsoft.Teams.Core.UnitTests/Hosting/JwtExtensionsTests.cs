// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Core.UnitTests.Hosting;

public class JwtExtensionsTests
{
    private const string Tenant = "00000000-0000-0000-0000-000000000001";
    private const string ClientId = "11111111-1111-1111-1111-111111111111";

    private static SecurityToken FakeJsonWebToken(string tenantId)
    {
        // Minimal JWT-shaped string with a tid claim for token-claim extraction.
        JsonWebTokenHandler handler = new();
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = "unused-by-test",
            Claims = new Dictionary<string, object> { ["tid"] = tenantId },
        };
        return new JsonWebToken(handler.CreateToken(descriptor));
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsBotFrameworkIssuer()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);

        string result = JwtExtensions.ValidateTeamsIssuer(
            "https://api.botframework.com", token, Tenant, "https://login.microsoftonline.com/", "https://api.botframework.com");

        Assert.Equal("https://api.botframework.com", result);
    }

    [Theory]
    [InlineData("https://api.botframework.us")]
    [InlineData("https://api.botframework.azure.cn")]
    public void ValidateTeamsIssuer_AcceptsSovereignBotFrameworkIssuer_WhenConfigured(string sovereignBotIssuer)
    {
        SecurityToken token = FakeJsonWebToken(Tenant);

        string result = JwtExtensions.ValidateTeamsIssuer(
            sovereignBotIssuer, token, Tenant, "https://login.microsoftonline.us/", sovereignBotIssuer);

        Assert.Equal(sovereignBotIssuer, result);
    }

    [Fact]
    public void ValidateTeamsIssuer_RejectsPublicBotIssuer_WhenSovereignBotIssuerConfigured()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);

        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            JwtExtensions.ValidateTeamsIssuer(
                "https://api.botframework.com", token, Tenant,
                "https://login.microsoftonline.us/", "https://api.botframework.us"));
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsPublicEntraV2Issuer()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);
        string issuer = $"https://login.microsoftonline.com/{Tenant}/v2.0";

        string result = JwtExtensions.ValidateTeamsIssuer(
            issuer, token, Tenant, "https://login.microsoftonline.com/", "https://api.botframework.com");

        Assert.Equal(issuer, result);
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsSovereignEntraIssuer_WhenInstanceConfigured()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);
        string sovereignInstance = "https://login.microsoftonline.us/";
        string issuer = $"{sovereignInstance}{Tenant}/v2.0";

        string result = JwtExtensions.ValidateTeamsIssuer(
            issuer, token, Tenant, sovereignInstance, "https://api.botframework.com");

        Assert.Equal(issuer, result);
    }

    [Fact]
    public void ValidateTeamsIssuer_RejectsPublicEntraIssuer_WhenSovereignInstanceConfigured()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);
        string publicIssuer = $"https://login.microsoftonline.com/{Tenant}/v2.0";

        SecurityTokenInvalidIssuerException ex = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            JwtExtensions.ValidateTeamsIssuer(
                publicIssuer, token, Tenant, "https://login.microsoftonline.us/", "https://api.botframework.com"));

        Assert.Contains(publicIssuer, ex.Message, StringComparison.Ordinal);
        Assert.Contains(Tenant, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsStsWindowsNetV1Issuer()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);
        string issuer = $"https://sts.windows.net/{Tenant}/";

        string result = JwtExtensions.ValidateTeamsIssuer(
            issuer, token, Tenant, "https://login.microsoftonline.com/", "https://api.botframework.com");

        Assert.Equal(issuer, result);
    }

    [Fact]
    public void ResolveSigningAuthority_RoutesPublicBotIssuer_ToConfiguredBotOidcUrl()
    {
        string authority = JwtExtensions.ResolveSigningAuthority(
            iss: "https://api.botframework.com",
            tid: Tenant,
            botTokenIssuer: "https://api.botframework.com",
            botOidcUrl: "https://login.botframework.com/v1/.well-known/openid-configuration",
            entraInstance: "https://login.microsoftonline.com/");

        Assert.Equal("https://login.botframework.com/v1/.well-known/openid-configuration", authority);
    }

    [Theory]
    [InlineData("https://api.botframework.us", "https://login.botframework.azure.us/v1/.well-known/openid-configuration")]
    [InlineData("https://api.botframework.azure.cn", "https://login.botframework.azure.cn/v1/.well-known/openid-configuration")]
    public void ResolveSigningAuthority_RoutesSovereignBotIssuer_ToConfiguredBotOidcUrl(string sovereignBotIssuer, string sovereignBotOidcUrl)
    {
        string authority = JwtExtensions.ResolveSigningAuthority(
            iss: sovereignBotIssuer,
            tid: Tenant,
            botTokenIssuer: sovereignBotIssuer,
            botOidcUrl: sovereignBotOidcUrl,
            entraInstance: "https://login.microsoftonline.us/");

        Assert.Equal(sovereignBotOidcUrl, authority);
    }

    [Fact]
    public void ResolveSigningAuthority_RoutesEntraIssuer_ToInstanceDerivedAuthority()
    {
        string authority = JwtExtensions.ResolveSigningAuthority(
            iss: $"https://login.microsoftonline.com/{Tenant}/v2.0",
            tid: Tenant,
            botTokenIssuer: "https://api.botframework.com",
            botOidcUrl: "https://login.botframework.com/v1/.well-known/openid-configuration",
            entraInstance: "https://login.microsoftonline.com/");

        Assert.Equal($"https://login.microsoftonline.com/{Tenant}/v2.0/.well-known/openid-configuration", authority);
    }

    [Fact]
    public void ResolveSigningAuthority_RoutesEntraIssuer_ToSovereignInstanceWhenConfigured()
    {
        string authority = JwtExtensions.ResolveSigningAuthority(
            iss: $"https://login.microsoftonline.us/{Tenant}/v2.0",
            tid: Tenant,
            botTokenIssuer: "https://api.botframework.us",
            botOidcUrl: "https://login.botframework.azure.us/v1/.well-known/openid-configuration",
            entraInstance: "https://login.microsoftonline.us/");

        Assert.Equal($"https://login.microsoftonline.us/{Tenant}/v2.0/.well-known/openid-configuration", authority);
    }

    [Fact]
    public void ResolveSigningAuthority_ReturnsEmpty_WhenIssuerNull()
    {
        string authority = JwtExtensions.ResolveSigningAuthority(
            iss: null,
            tid: Tenant,
            botTokenIssuer: "https://api.botframework.com",
            botOidcUrl: "https://login.botframework.com/v1/.well-known/openid-configuration",
            entraInstance: "https://login.microsoftonline.com/");

        Assert.Equal(string.Empty, authority);
    }

    [Fact]
    public void AddBotAuthentication_ManualOverload_DoesNotThrow_WhenNoIConfigurationRegistered()
    {
        // Regression: AddBotAuthentication(clientId, tenantId) manual overload should remain usable
        // even when no IConfiguration is registered (e.g. plain ServiceCollection scenarios).
        ServiceCollection services = new();
        services.AddLogging();

        Exception? caught = Record.Exception(() => services.AddBotAuthentication(ClientId, Tenant));

        Assert.Null(caught);
    }

    [Fact]
    public async Task AddBotAuthorization_DangerouslyAllowUnauthenticatedRequests_AuthenticatesWithoutAuthorizationHeader()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = ClientId,
                ["AzureAd:TenantId"] = Tenant,
                ["AzureAd:DangerouslyAllowUnauthenticatedRequests"] = "true",
            })
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddBotAuthorization();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };

        AuthenticateResult result = await httpContext.AuthenticateAsync("AzureAd");

        Assert.True(result.Succeeded);
        Assert.Equal("BypassAuth", result.Principal?.Identity?.AuthenticationType);
    }

    [Fact]
    public async Task AddBotAuthorization_NoClientId_ChallengesWithAuthenticationNotConfigured()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:TenantId"] = Tenant,
            })
            .Build();

        ServiceCollection services = new();
        ListLoggerProvider loggerProvider = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddProvider(loggerProvider));
        services.AddBotAuthorization();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        await using MemoryStream responseBody = new();
        DefaultHttpContext httpContext = new()
        {
            RequestServices = serviceProvider
        };
        httpContext.Response.Body = responseBody;

        AuthenticateResult result = await httpContext.AuthenticateAsync("AzureAd");
        await httpContext.ChallengeAsync("AzureAd");

        responseBody.Position = 0;
        string body = await new StreamReader(responseBody).ReadToEndAsync();
        Assert.False(result.Succeeded);
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        using JsonDocument problem = JsonDocument.Parse(body);
        Assert.Equal("Authentication not configured", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.RootElement.GetProperty("status").GetInt32());
        Assert.False(problem.RootElement.TryGetProperty("detail", out _));
        Assert.Contains(
            "Authentication is not configured. Configure ClientId or enable DangerouslyAllowUnauthenticatedRequests for local development.",
            loggerProvider.Messages);
    }

    private sealed class ListLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new ListLogger(Messages);

        public void Dispose()
        {
        }
    }

    private sealed class ListLogger(List<string> messages) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                messages.Add(formatter(state, exception));
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
