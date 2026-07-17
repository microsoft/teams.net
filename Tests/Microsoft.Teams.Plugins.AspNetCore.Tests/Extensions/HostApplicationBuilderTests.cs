using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using static Microsoft.Teams.Plugins.AspNetCore.Extensions.HostApplicationBuilderExtensions;

namespace Microsoft.Teams.Plugins.AspNetCore.Tests;

public class HostApplicationBuilderTests
{
    [Fact]
    public async Task AddTeamsTokenAuthentication_ShouldRegisterJwtBearerScheme()
    {
        var builder = WebApplication.CreateBuilder();
        var mockSettings = new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "test-client-id",
        };
        builder.Configuration.AddInMemoryCollection(mockSettings);
        builder.AddTeams();

        var services = builder.Build().Services;
        var schemes = services.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemes.GetSchemeAsync(TeamsTokenAuthConstants.AuthenticationScheme);
        var authOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await authOptions.GetPolicyAsync(TeamsTokenAuthConstants.AuthorizationPolicy);
        var mvcBuilder = services.GetService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider>();

        Assert.NotNull(scheme);
        Assert.NotNull(policy);
        Assert.Equal("JwtBearerHandler", scheme.HandlerType.Name);
        Assert.True(policy.Requirements.OfType<RolesAuthorizationRequirement>().Any() ||
            policy.Requirements.OfType<IAuthorizationRequirement>().Any(r => r is not AssertionRequirement));
        Assert.NotNull(mvcBuilder);
    }


    [Fact]
    public async Task AddTeamsTokenAuthentication_ShouldRejectRequests_WhenClientIdIsMissing()
    {
        var builder = WebApplication.CreateBuilder();
        var mockSettings = new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = null,
        };
        builder.Configuration.AddInMemoryCollection(mockSettings);
        builder.AddTeams();
        var services = builder.Build().Services;
        var authOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorizationService = services.GetRequiredService<IAuthorizationService>();
        var pluginOptions = services.GetRequiredService<AspNetCorePluginOptions>();

        var policy = await authOptions.GetPolicyAsync(TeamsTokenAuthConstants.AuthorizationPolicy);
        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), null, policy!);

        Assert.NotNull(policy);
        Assert.True(policy.Requirements.OfType<AssertionRequirement>().Any());
        Assert.False(result.Succeeded);
        Assert.False(pluginOptions.DangerouslyAllowUnauthenticatedRequests);
    }

    [Fact]
    public async Task AddTeamsTokenAuthentication_ShouldAllowUnauthenticatedRequests_WhenDangerouslyAllowed()
    {
        var builder = WebApplication.CreateBuilder();
        var mockSettings = new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "test-client-id",
        };
        builder.Configuration.AddInMemoryCollection(mockSettings);
        builder.AddTeams(new AspNetCorePluginOptions { DangerouslyAllowUnauthenticatedRequests = true });
        var services = builder.Build().Services;
        var authOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorizationService = services.GetRequiredService<IAuthorizationService>();
        var pluginOptions = services.GetRequiredService<AspNetCorePluginOptions>();

        var policy = await authOptions.GetPolicyAsync(TeamsTokenAuthConstants.AuthorizationPolicy);
        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), null, policy!);

        // Should allow all requests
        Assert.NotNull(policy);
        Assert.True(policy.Requirements.OfType<AssertionRequirement>().Any());
        Assert.True(result.Succeeded);
        Assert.True(pluginOptions.DangerouslyAllowUnauthenticatedRequests);
    }

    [Fact]
    public async Task AddTeamsTokenAuthentication_ShouldAllowUnauthenticatedRequests_WhenConfigured()
    {
        var builder = WebApplication.CreateBuilder();
        var mockSettings = new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "test-client-id",
            ["Teams:DangerouslyAllowUnauthenticatedRequests"] = "true",
        };
        builder.Configuration.AddInMemoryCollection(mockSettings);
        builder.AddTeams();
        var services = builder.Build().Services;
        var authOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorizationService = services.GetRequiredService<IAuthorizationService>();
        var pluginOptions = services.GetRequiredService<AspNetCorePluginOptions>();

        var policy = await authOptions.GetPolicyAsync(TeamsTokenAuthConstants.AuthorizationPolicy);
        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), null, policy!);

        Assert.NotNull(policy);
        Assert.True(policy.Requirements.OfType<AssertionRequirement>().Any());
        Assert.True(result.Succeeded);
        Assert.True(pluginOptions.DangerouslyAllowUnauthenticatedRequests);
    }

    [Fact]
    public async Task AddTeamsTokenAuthentication_ShouldAllowUnauthenticatedRequests_WhenObsoleteSkipAuthIsTrue()
    {
        var builder = WebApplication.CreateBuilder();
        var mockSettings = new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "test-client-id",
        };
        builder.Configuration.AddInMemoryCollection(mockSettings);
#pragma warning disable CS0618 // Verifies backward compatibility for the deprecated skipAuth parameter.
        builder.AddTeams(skipAuth: true);
#pragma warning restore CS0618
        var services = builder.Build().Services;
        var authOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorizationService = services.GetRequiredService<IAuthorizationService>();
        var pluginOptions = services.GetRequiredService<AspNetCorePluginOptions>();

        var policy = await authOptions.GetPolicyAsync(TeamsTokenAuthConstants.AuthorizationPolicy);
        var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), null, policy!);

        Assert.NotNull(policy);
        Assert.True(policy.Requirements.OfType<AssertionRequirement>().Any());
        Assert.True(result.Succeeded);
        Assert.True(pluginOptions.DangerouslyAllowUnauthenticatedRequests);
    }

    [Fact]
    public async Task AddTeamsTokenAuthentication_ShouldRegisterEntraTokenValidation()
    {
        var builder = WebApplication.CreateBuilder();
        var mockSettings = new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "test-client-id",
        };
        builder.Configuration.AddInMemoryCollection(mockSettings);
        builder.AddTeams();

        var services = builder.Build().Services;
        var schemes = services.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemes.GetSchemeAsync(EntraTokenAuthConstants.AuthenticationScheme);
        var authOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await authOptions.GetPolicyAsync(EntraTokenAuthConstants.AuthorizationPolicy);
        var mvcBuilder = services.GetService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider>();

        Assert.NotNull(scheme);
        Assert.NotNull(policy);
        Assert.Equal("JwtBearerHandler", scheme.HandlerType.Name);
        Assert.True(policy.Requirements.OfType<RolesAuthorizationRequirement>().Any() ||
            policy.Requirements.OfType<IAuthorizationRequirement>().Any(r => r is not AssertionRequirement));
        Assert.NotNull(mvcBuilder);
    }
}