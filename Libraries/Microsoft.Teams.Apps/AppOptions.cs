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
    /// Additional allowed service URL hostnames beyond the built-in defaults.
    /// Use this if your bot receives activities from non-standard channels.
    /// </summary>
    public IEnumerable<string>? AdditionalAllowedDomains { get; set; }

    public AppOptions()
    {

    }

    public AppOptions(IServiceProvider provider)
    {
        Provider = provider;
    }
}