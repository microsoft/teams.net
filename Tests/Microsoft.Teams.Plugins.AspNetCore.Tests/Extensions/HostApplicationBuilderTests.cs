using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;
using Microsoft.Extensions.Hosting;

public class TeamsExtensionsTests
{
    [Fact]
    public void AddTeamsTokenAuthentication_Should_Register_JwtBearerScheme()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.AddTeams();

        var services = builder.Build().Services;
        var schemes = services.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = schemes.GetSchemeAsync("TeamsJWTScheme").Result;
        var authOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = authOptions.GetPolicyAsync("TeamsJWTPolicy").Result;
        var mvcBuilder = services.GetService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider>();

        Assert.NotNull(scheme);
        Assert.Equal("JwtBearerHandler", scheme.HandlerType.Name);
        Assert.True(policy.Requirements.OfType<RolesAuthorizationRequirement>().Any() ||
            policy.Requirements.OfType<IAuthorizationRequirement>().Any(r => r is not AssertionRequirement));
        Assert.NotNull(mvcBuilder); 
    }

    [Fact]
    public void AddTeamsTokenAuthentication_WithSkipAuth()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.AddTeams(skipAuth: true);
        var services = builder.Build().Services;
        var authOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = authOptions.GetPolicyAsync("TeamsJWTPolicy").Result;

        // Should allow all requests
        Assert.True(policy.Requirements.OfType<AssertionRequirement>().Any());
    }
}
