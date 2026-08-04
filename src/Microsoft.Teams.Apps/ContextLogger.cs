// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Provides backward-compatible logging methods (<c>.Info()</c>, <c>.Error()</c>, <c>.Debug()</c>, <c>.Warn()</c>)
/// that delegate to an underlying <see cref="ILogger"/> instance.
/// </summary>
/// <param name="logger">The underlying logger to delegate to.</param>
public class ContextLogger(ILogger logger)
{
    /// <summary>
    /// Gets the underlying <see cref="ILogger"/> instance.
    /// </summary>
    public ILogger Logger { get; } = logger;

    /// <summary>
    /// Logs a message at the <see cref="LogLevel.Information"/> level.
    /// </summary>
    /// <param name="args">The message arguments. The first string argument is used as the message template.</param>
    public void Info(params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!Logger.IsEnabled(LogLevel.Information)) return;
        Logger.LogInformation("{Message}", FormatArgs(args));
    }

    /// <summary>
    /// Logs a message at the <see cref="LogLevel.Error"/> level.
    /// </summary>
    /// <param name="args">The message arguments. The first string argument is used as the message template.</param>
    public void Error(params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!Logger.IsEnabled(LogLevel.Error)) return;
        Logger.LogError("{Message}", FormatArgs(args));
    }

    /// <summary>
    /// Logs a message at the <see cref="LogLevel.Debug"/> level.
    /// </summary>
    /// <param name="args">The message arguments. The first string argument is used as the message template.</param>
    public void Debug(params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!Logger.IsEnabled(LogLevel.Debug)) return;
        Logger.LogDebug("{Message}", FormatArgs(args));
    }

    /// <summary>
    /// Logs a message at the <see cref="LogLevel.Warning"/> level.
    /// </summary>
    /// <param name="args">The message arguments. The first string argument is used as the message template.</param>
    public void Warn(params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!Logger.IsEnabled(LogLevel.Warning)) return;
        Logger.LogWarning("{Message}", FormatArgs(args));
    }

    private static string FormatArgs(object?[] args)
    {
        return args.Length switch
        {
            0 => string.Empty,
            1 => args[0]?.ToString() ?? string.Empty,
            _ => string.Join(" ", args.Select(a => a?.ToString() ?? "null"))
        };
    }
}
