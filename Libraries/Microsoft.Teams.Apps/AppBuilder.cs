// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps;

public partial class AppBuilder
{
    protected AppOptions _options;

    public AppBuilder(AppOptions? options = null)
    {
        _options = options ?? new AppOptions();
    }

    public AppBuilder AddLogger(Common.Logging.ILogger logger)
    {
        _options.Logger = logger;
        return this;
    }

    public AppBuilder AddLogger(string? name = null, Common.Logging.LogLevel level = Common.Logging.LogLevel.Info)
    {
        _options.Logger = new Common.Logging.ConsoleLogger(name, level);
        return this;
    }

    public AppBuilder AddStorage<TStorage>(TStorage storage) where TStorage : Common.Storage.IStorage<string, object>
    {
        _options.Storage = storage;
        return this;
    }

    public AppBuilder AddClient(Common.Http.IHttpClient client)
    {
        _options.Client = client;
        return this;
    }

    public AppBuilder AddClient(Common.Http.IHttpClientFactory factory)
    {
        _options.ClientFactory = factory;
        return this;
    }

    public AppBuilder AddClient(Func<Common.Http.IHttpClient> @delegate)
    {
        _options.Client = @delegate();
        return this;
    }

    public AppBuilder AddClient(Func<Task<Common.Http.IHttpClient>> @delegate)
    {
        _options.Client = @delegate().GetAwaiter().GetResult();
        return this;
    }

    public AppBuilder AddCredentials(Common.Http.IHttpCredentials credentials)
    {
        _options.Credentials = credentials;
        return this;
    }

    public AppBuilder AddCredentials(Func<Common.Http.IHttpCredentials> @delegate)
    {
        _options.Credentials = @delegate();
        return this;
    }

    public AppBuilder AddCredentials(Func<Task<Common.Http.IHttpCredentials>> @delegate)
    {
        _options.Credentials = @delegate().GetAwaiter().GetResult();
        return this;
    }

    public AppBuilder AddPlugin(IPlugin plugin)
    {
        _options.Plugins.Add(plugin);
        return this;
    }

    public AppBuilder AddPlugin(Func<IPlugin> @delegate)
    {
        _options.Plugins.Add(@delegate());
        return this;
    }

    public AppBuilder AddPlugin(Func<Task<IPlugin>> @delegate)
    {
        _options.Plugins.Add(@delegate().GetAwaiter().GetResult());
        return this;
    }

    public AppBuilder AddOAuth(string defaultConnectionName) {
        _options.OAuth = new OAuthSettings(defaultConnectionName);
        return this;
    }

    public App Build()
    {
        return new App(_options);
    }
}