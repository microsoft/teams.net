// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps;

public class AppOptions
{
    public IServiceProvider? Provider { get; set; }

    public ILoggerFactory? LoggerFactory { get; set; }
    
    public Common.Storage.IStorage<string, object>? Storage { get; set; }
    public Common.Http.IHttpClient? Client { get; set; }
    public Common.Http.IHttpClientFactory? ClientFactory { get; set; }
    public Common.Http.IHttpCredentials? Credentials { get; set; }
    public IList<IPlugin> Plugins { get; set; } = [];
    public OAuthSettings OAuth { get; set; } = new OAuthSettings();

    public AppOptions()
    {

    }

    public AppOptions(IServiceProvider provider)
    {
        Provider = provider;
    }
}