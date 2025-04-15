using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps;

public interface IAppBuilder
{
    public IAppBuilder AddLogger(Common.Logging.ILogger logger);
    public IAppBuilder AddLogger(string? name = null, Common.Logging.LogLevel level = Common.Logging.LogLevel.Info);

    public IAppBuilder AddStorage<TStorage>(TStorage storage) where TStorage : Common.Storage.IStorage<string, object>;

    public IAppBuilder AddClient(Common.Http.IHttpClient client);
    public IAppBuilder AddClient(Common.Http.IHttpClientFactory factory);
    public IAppBuilder AddClient(Func<Common.Http.IHttpClient> @delegate);
    public IAppBuilder AddClient(Func<Task<Common.Http.IHttpClient>> @delegate);

    public IAppBuilder AddCredentials(Common.Http.IHttpCredentials credentials);
    public IAppBuilder AddCredentials(Func<Common.Http.IHttpCredentials> @delegate);
    public IAppBuilder AddCredentials(Func<Task<Common.Http.IHttpCredentials>> @delegate);

    public IAppBuilder AddPlugin(IPlugin plugin);
    public IAppBuilder AddPlugin(Func<IPlugin> @delegate);
    public IAppBuilder AddPlugin(Func<Task<IPlugin>> @delegate);

    public IApp Build();
}

public partial class AppBuilder : IAppBuilder
{
    protected IAppOptions _options;

    public AppBuilder(IAppOptions? options = null)
    {
        _options = options ?? new AppOptions();
    }

    public IAppBuilder AddLogger(Common.Logging.ILogger logger)
    {
        _options.Logger = logger;
        return this;
    }

    public IAppBuilder AddLogger(string? name = null, Common.Logging.LogLevel level = Common.Logging.LogLevel.Info)
    {
        _options.Logger = new Common.Logging.ConsoleLogger(name, level);
        return this;
    }

    public IAppBuilder AddStorage<TStorage>(TStorage storage) where TStorage : Common.Storage.IStorage<string, object>
    {
        _options.Storage = storage;
        return this;
    }

    public IAppBuilder AddClient(Common.Http.IHttpClient client)
    {
        _options.Client = client;
        return this;
    }

    public IAppBuilder AddClient(Common.Http.IHttpClientFactory factory)
    {
        _options.ClientFactory = factory;
        return this;
    }

    public IAppBuilder AddClient(Func<Common.Http.IHttpClient> @delegate)
    {
        _options.Client = @delegate();
        return this;
    }

    public IAppBuilder AddClient(Func<Task<Common.Http.IHttpClient>> @delegate)
    {
        _options.Client = @delegate().GetAwaiter().GetResult();
        return this;
    }

    public IAppBuilder AddCredentials(Common.Http.IHttpCredentials credentials)
    {
        _options.Credentials = credentials;
        return this;
    }

    public IAppBuilder AddCredentials(Func<Common.Http.IHttpCredentials> @delegate)
    {
        _options.Credentials = @delegate();
        return this;
    }

    public IAppBuilder AddCredentials(Func<Task<Common.Http.IHttpCredentials>> @delegate)
    {
        _options.Credentials = @delegate().GetAwaiter().GetResult();
        return this;
    }

    public IAppBuilder AddPlugin(IPlugin plugin)
    {
        _options.Plugins.Add(plugin);
        return this;
    }

    public IAppBuilder AddPlugin(Func<IPlugin> @delegate)
    {
        _options.Plugins.Add(@delegate());
        return this;
    }

    public IAppBuilder AddPlugin(Func<Task<IPlugin>> @delegate)
    {
        _options.Plugins.Add(@delegate().GetAwaiter().GetResult());
        return this;
    }

    public IApp Build()
    {
        return new App(_options);
    }
}