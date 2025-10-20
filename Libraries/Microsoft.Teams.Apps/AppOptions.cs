// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps;

public class AppOptions
{
    /// <summary>
    /// The applications optional storage provider that allows
    /// the application to access shared dependencies.
    /// </summary>
    public IServiceProvider? Provider { get; set; }

    /// <summary>
    /// The applications optional ILogger instance.
    /// </summary>
    public Common.Logging.ILogger? Logger { get; set; }

    /// <summary>
    /// The applications optional IStorage instance.
    /// </summary>
    public Common.Storage.IStorage<string, object>? Storage { get; set; }

    /// <summary>
    /// When provided, the application will use this <code>IHttpClient</code> instance
    /// to send all http requests.
    /// </summary>
    public Common.Http.IHttpClient? Client { get; set; }

    /// <summary>
    /// When provided, the application will use this <code>IHttpClientFactory</code> to
    /// initialize a new client whenever needed.
    /// </summary>
    public Common.Http.IHttpClientFactory? ClientFactory { get; set; }

    /// <summary>
    /// When provided, the application will use these credentials to resolve tokens it
    /// uses to make API requests.
    /// </summary>
    public Common.Http.IHttpCredentials? Credentials { get; set; }

    /// <summary>
    /// A list of plugins to import into the application.
    /// </summary>
    public IList<IPlugin> Plugins { get; set; } = [];

    /// <summary>
    /// User <code>OAuth</code> settings for the deferred (User) auth flows.
    /// </summary>
    public OAuthSettings OAuth { get; set; } = new();

    public AppOptions()
    {

    }

    public AppOptions(IServiceProvider provider)
    {
        Provider = provider;
    }
}