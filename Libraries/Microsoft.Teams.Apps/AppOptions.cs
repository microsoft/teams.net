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
    /// When true, skips the per-activity user OAuth token lookup
    /// (<see cref="Api.Clients.UserTokenClient.GetAsync"/>) in <c>App.Process</c>.
    /// The lookup adds ~200ms to every activity and is only useful for bots that have
    /// configured an SSO connection via <see cref="OAuthSettings"/>. Defaults to true.
    /// </summary>
    public bool DisableUserTokenLookup { get; set; } = true;

    public AppOptions()
    {

    }

    public AppOptions(IServiceProvider provider)
    {
        Provider = provider;
    }
}