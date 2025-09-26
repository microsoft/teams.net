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
    public async Task AddTeamsTokenAuthentication_ShouldSkipJwtAuthentication_WhenClientIdIsMissing()
    {
        var builder = WebApplication.CreateBuilder();
        var mockSettings = new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = null,
        };
        builder.Configuration.AddInMemoryCollection(mockSettings);
        builder.AddTeams(skipAuth: false);
        var services = builder.Build().Services;
        var authOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await authOptions.GetPolicyAsync(TeamsTokenAuthConstants.AuthorizationPolicy);

        // Should allow all requests
        Assert.NotNull(policy);
        Assert.True(policy.Requirements.OfType<AssertionRequirement>().Any());
    }

    [Fact]
    public async Task AddTeamsTokenAuthentication_ShouldSkipJwtAuthentication_WhenWithSkipIsTrue()
    {
        var builder = WebApplication.CreateBuilder();
        var mockSettings = new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "test-client-id",
        };
        builder.Configuration.AddInMemoryCollection(mockSettings);
        builder.AddTeams(skipAuth: true);
        var services = builder.Build().Services;
        var authOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await authOptions.GetPolicyAsync(TeamsTokenAuthConstants.AuthorizationPolicy);

        // Should allow all requests
        Assert.NotNull(policy);
        Assert.True(policy.Requirements.OfType<AssertionRequirement>().Any());
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