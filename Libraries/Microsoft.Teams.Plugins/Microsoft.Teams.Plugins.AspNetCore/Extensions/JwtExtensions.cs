using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;


public static class JwtExtensions
{
    public static AuthenticationBuilder AddBotAuthentication(this IServiceCollection services, string aadSectionName = "Teams")
    {
        var authenticationBuilder = services.AddAuthentication();
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        //string agentScope = configuration[$"{aadSectionName}:AgentScope"]!;
        string audience = configuration[$"{aadSectionName}:ClientId"]!;
        string tenantId = configuration[$"{aadSectionName}:TenantId"]!;

        services
            .AddAuthentication()
            .AddCustomJwtBearer("Bot", "botframework.com", audience)
            .AddCustomJwtBearer("Agent", tenantId, audience);
        return authenticationBuilder;
    }

    public static AuthenticationBuilder AddBotAuthenticationEx(this IServiceCollection services, IEnumerable<string> aadSectionNames)
    {
        var authenticationBuilder = services.AddAuthentication();
        List<string> audiences = [];
        List<string> tenants = [];
        foreach (var aadSectionName in aadSectionNames)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            // string agentScope = configuration[$"{aadSectionName}:AgentScope"]!;
            string audience = configuration[$"{aadSectionName}:ClientId"]!;
            string tenantId = configuration[$"{aadSectionName}:TenantId"]!;
            audiences.Add(audience);
            tenants.Add(tenantId);
        }
        authenticationBuilder.AddCustomJwtBearerEx("BotAndAgentScheme", tenants, audiences);
        return authenticationBuilder;
    }

    public static AuthorizationBuilder AddBotAuthorization(this IServiceCollection services)
    {
        var authorizationBuilder = services
            .AddAuthorizationBuilder()
            .AddDefaultPolicy("DefaultPolicy", policy =>
            {
                policy.AuthenticationSchemes.Add("Bot");
                policy.AuthenticationSchemes.Add("Agent");
                policy.RequireAuthenticatedUser();
            });
        return authorizationBuilder;
    }

    public static AuthorizationBuilder AddBotAuthorizationEx(this IServiceCollection services)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        var authorizationBuilder = services.AddAuthorizationBuilder();
        authorizationBuilder = authorizationBuilder.AddDefaultPolicy("DefaultPolicy", policy =>
        {
            policy.AuthenticationSchemes.Add("BotAndAgentScheme");
            policy.RequireAuthenticatedUser();
        });
        return authorizationBuilder;
    }


    public static AuthenticationBuilder AddCustomJwtBearer(this AuthenticationBuilder builder, string schemeName, string tenantId, string audience)
    {
        string metadataAddress = tenantId.Equals("botframework.com", StringComparison.OrdinalIgnoreCase)
            ? "https://login.botframework.com/v1/.well-known/openidconfiguration"
            : $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";

        string[] validIssuers = tenantId.Equals("botframework.com", StringComparison.OrdinalIgnoreCase)
            ? ["https://api.botframework.com"]
            : [$"https://sts.windows.net/{tenantId}/", $"https://login.microsoftonline.com/{tenantId}/v2", "https://api.botframework.com"];

        builder.AddJwtBearer(schemeName, jwtOptions =>
         {
             jwtOptions.SaveToken = true;
             jwtOptions.IncludeErrorDetails = true;
             jwtOptions.MetadataAddress = metadataAddress;
             jwtOptions.Audience = audience;
             jwtOptions.TokenValidationParameters = new TokenValidationParameters
             {
                 ValidateIssuerSigningKey = true,
                 RequireSignedTokens = true,
                 ValidateIssuer = true,
                 ValidateAudience = true,
                 ValidIssuers = validIssuers
             };
             jwtOptions.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
             jwtOptions.MapInboundClaims = true;
             // jwtOptions.Events = jwtEvents;
             jwtOptions.Validate();
         });
        return builder;
    }

    public static AuthenticationBuilder AddCustomJwtBearerEx(this AuthenticationBuilder builder, string schemeName, IEnumerable<string> tenants, IEnumerable<string> audiences)
    {
        //string metadataAddress = tenantId.Equals("botframework.com", StringComparison.OrdinalIgnoreCase)
        //    ? "https://login.botframework.com/v1/.well-known/openidconfiguration"
        //    : $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";

        List<string> validIssuers = ["https://api.botframework.com"];

        foreach (var tenantId in tenants)
        {
            validIssuers.Add($"https://sts.windows.net/{tenantId}/");
            validIssuers.Add($"https://login.microsoftonline.com/{tenantId}/v2");
        }

        builder.AddJwtBearer(schemeName, jwtOptions =>
        {
            jwtOptions.SaveToken = true;
            jwtOptions.IncludeErrorDetails = true;
            //jwtOptions.MetadataAddress = metadataAddress;
            jwtOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidAudiences = audiences,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuers = validIssuers
            };
            jwtOptions.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
            jwtOptions.MapInboundClaims = true;
            jwtOptions.Events = new JwtBearerEvents
            {
                OnMessageReceived = async context =>
                {
                    string authorizationHeader = context.Request.Headers.Authorization.ToString();

                    if (string.IsNullOrEmpty(authorizationHeader))
                    {
                        // Default to AadTokenValidation handling
                        context.Options.TokenValidationParameters.ConfigurationManager ??= jwtOptions.ConfigurationManager as BaseConfigurationManager;
                        await Task.CompletedTask.ConfigureAwait(false);
                        return;
                    }

                    string[] parts = authorizationHeader?.Split(' ')!;
                    if (parts.Length != 2 || parts[0] != "Bearer")
                    {
                        // Default to AadTokenValidation handling
                        context.Options.TokenValidationParameters.ConfigurationManager ??= jwtOptions.ConfigurationManager as BaseConfigurationManager;
                        await Task.CompletedTask.ConfigureAwait(false);
                        return;
                    }

                    JwtSecurityToken token = new(parts[1]);
                    string issuer = token.Claims.FirstOrDefault(claim => claim.Type == "iss")?.Value!;
                    string tid = token.Claims.FirstOrDefault(claim => claim.Type == "tid")?.Value!;

                    string oidcAuthority = issuer.Equals("https://api.botframework.com", StringComparison.OrdinalIgnoreCase)
                        ? "https://login.botframework.com/v1/.well-known/openid-configuration"
                        : $"https://login.microsoftonline.com/{tid ?? "botframework.com"}/v2.0/.well-known/openid-configuration";

                    jwtOptions.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                        oidcAuthority,
                        new OpenIdConnectConfigurationRetriever(),
                        new HttpDocumentRetriever
                        {
                            RequireHttps = jwtOptions.RequireHttpsMetadata
                        });


                    await Task.CompletedTask.ConfigureAwait(false);
                },
                OnTokenValidated = context =>
                {
                    return Task.CompletedTask;
                },
                OnForbidden = context =>
                {
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    return Task.CompletedTask;
                }
            };
            jwtOptions.Validate();
        });
        return builder;
    }


    readonly static JwtBearerEvents jwtEvents = new()
    {
        OnMessageReceived = context =>
        {
            string accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last()!;
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            var f = context.Principal;
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var ex = context.Exception;
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.ToString());
            return System.Threading.Tasks.Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var v = context.SecurityToken;
            Console.WriteLine("Token validated");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine("token challenged");
            var error = context.Error;
            return Task.CompletedTask;
        }
    };
}
