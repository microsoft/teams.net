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
    /// When true, <c>App.Process</c> performs a per-activity user OAuth token lookup
    /// (<see cref="Api.Clients.UserTokenClient.GetAsync"/>) to populate
    /// <c>IContext.IsSignedIn</c> / <c>IContext.UserGraphToken</c>. Defaults to true to
    /// preserve existing behaviour. Set to false to skip the call -- it adds ~200ms per
    /// activity and is only useful for bots with an SSO connection configured via
    /// <see cref="OAuthSettings"/>.
    /// </summary>
    public bool AutoUserTokenLookup { get; set; } = true;

    public AppOptions()
    {

    }

    public AppOptions(IServiceProvider provider)
    {
        Provider = provider;
    }
}