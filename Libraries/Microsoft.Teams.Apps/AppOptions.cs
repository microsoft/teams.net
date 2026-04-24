// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps;

public class AppOptions
{
    public IServiceProvider? Provider { get; set; }
    public Common.Logging.ILogger? Logger { get; set; }
    public Common.Storage.IStorage<string, object>? Storage { get; set; }
    public Common.Http.IHttpClient? Client { get; set; }
    public Common.Http.IHttpClientFactory? ClientFactory { get; set; }
    public Common.Http.IHttpCredentials? Credentials { get; set; }
    public IList<IPlugin> Plugins { get; set; } = [];
    public OAuthSettings OAuth { get; set; } = new OAuthSettings();
    public CloudEnvironment? Cloud { get; set; }

    /// <summary>
    /// Additional service URL hostnames accepted beyond the cloud preset.
    /// Entries must be bare hostnames matched exactly (case-insensitive)
    /// wildcard patterns like <c>"*.example.com"</c>, URL suffixes, or full URLs are NOT supported.
    /// Pass <c>["*"]</c> as the sole wildcard to accept any hostname (disables service-URL validation).
    /// </summary>
    /// <example>new[] { "api.my-custom-channel.com" }</example>
    public IEnumerable<string>? AdditionalAllowedDomains { get; set; }

    public AppOptions()
    {

    }

    public AppOptions(IServiceProvider provider)
    {
        Provider = provider;
    }
}