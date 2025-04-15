using Humanizer;

using Json.Schema;

namespace Microsoft.Teams.AI.Prompts;

public partial class ChatPrompt<TOptions>
{
    public ChatPrompt<TOptions> Function(IFunction function)
    {
        Functions.Add(function);
        return this;
    }

    public ChatPrompt<TOptions> Function(string name, string? description, Func<object?, Task<object?>> handler)
    {
        Functions.Add(new Function(name, description, handler));
        return this;
    }

    public ChatPrompt<TOptions> Function(string name, string? description, Func<object?, Task> handler)
    {
        Functions.Add(new Function(name, description, async (args) =>
        {
            await handler(args);
            return null;
        }));

        return this;
    }

    public ChatPrompt<TOptions> Function<T>(string name, string? description, Func<T, Task<object?>> handler)
    {
        Functions.Add(new Function<T>(name, description, handler));
        return this;
    }

    public ChatPrompt<TOptions> Function<T>(string name, string? description, Func<T, Task> handler)
    {
        Functions.Add(new Function<T>(name, description, async (args) =>
        {
            await handler(args);
            return null;
        }));

        return this;
    }

    public ChatPrompt<TOptions> Function(string name, string? description, JsonSchema parameters, Func<object?, Task<object?>> handler)
    {
        Functions.Add(new Function(name, description, parameters, handler));
        return this;
    }

    public ChatPrompt<TOptions> Function(string name, string? description, JsonSchema parameters, Func<object?, Task> handler)
    {
        Functions.Add(new Function(name, description, parameters, async (args) =>
        {
            await handler(args);
            return null;
        }));

        return this;
    }

    public ChatPrompt<TOptions> Function<T>(string name, string? description, JsonSchema parameters, Func<T, Task<object?>> handler)
    {
        Functions.Add(new Function<T>(name, description, parameters, handler));
        return this;
    }

    public ChatPrompt<TOptions> Function<T>(string name, string? description, JsonSchema parameters, Func<T, Task> handler)
    {
        Functions.Add(new Function<T>(name, description, parameters, async (args) =>
        {
            await handler(args);
            return null;
        }));

        return this;
    }

    public async Task<object?> Invoke(string name, object? args = null, CancellationToken cancellationToken = default)
    {
        var function = Functions.Get(name) ?? throw new NotImplementedException();
        var logger = Logger.Child($"Functions.{name}");

        if (function is Function func)
        {
            foreach (var plugin in ChatPlugins)
            {
                args = await plugin.OnBeforeFunctionCall(this, func, args, cancellationToken);
            }

            var startedAt = DateTime.Now;
            logger.Debug(args);
            var res = await func.Invoke(args);
            var endedAt = DateTime.Now;
            logger.Debug(res);
            logger.Debug($"elapse time: {(endedAt - startedAt).Humanize(3)}");

            foreach (var plugin in ChatPlugins)
            {
                res = await plugin.OnAfterFunctionCall(this, func, res, cancellationToken);
            }

            return res;
        }

        return Task.FromResult<object?>(null);
    }

    public async Task<object?> Invoke<T>(string name, T args, CancellationToken cancellationToken = default)
    {
        var function = Functions.Get(name) ?? throw new NotImplementedException();
        var logger = Logger.Child($"Functions.{name}");

        if (function is Function<T> func)
        {
            foreach (var plugin in ChatPlugins)
            {
                args = await plugin.OnBeforeFunctionCall(this, func, args, cancellationToken);
            }

            var startedAt = DateTime.Now;
            logger.Debug(args);
            var res = await func.Invoke(args);
            var endedAt = DateTime.Now;
            logger.Debug(res);
            logger.Debug($"elapse time: {(endedAt - startedAt).Humanize(3)}");

            foreach (var plugin in ChatPlugins)
            {
                res = await plugin.OnAfterFunctionCall(this, func, res, cancellationToken);
            }

            return res;
        }

        return Task.FromResult<object?>(null);
    }
}