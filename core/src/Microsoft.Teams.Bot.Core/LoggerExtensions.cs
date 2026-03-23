// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Bot.Core;

#pragma warning disable CA2254 // Template should be a static expression - by design, these are guarded wrappers
internal static class LoggerExtensions
{
    public static void LogInformationGuarded(this ILogger? logger, string message)
    {
        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            logger.LogInformation(message);
        }
    }

    public static void LogInformationGuarded<T0>(this ILogger? logger, string message, T0 arg0)
    {
        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            logger.LogInformation(message, arg0);
        }
    }

    public static void LogInformationGuarded<T0, T1, T2>(this ILogger? logger, string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            logger.LogInformation(message, arg0, arg1, arg2);
        }
    }

    public static void LogInformationGuarded<T0, T1, T2, T3>(this ILogger? logger, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (logger?.IsEnabled(LogLevel.Information) == true)
        {
            logger.LogInformation(message, arg0, arg1, arg2, arg3);
        }
    }

    public static void LogTraceGuarded(this ILogger? logger, string message)
    {
        if (logger?.IsEnabled(LogLevel.Trace) == true)
        {
            logger.LogTrace(message);
        }
    }

    public static void LogTraceGuarded<T0>(this ILogger? logger, string message, T0 arg0)
    {
        if (logger?.IsEnabled(LogLevel.Trace) == true)
        {
            logger.LogTrace(message, arg0);
        }
    }

    public static void LogTraceGuarded<T0, T1>(this ILogger? logger, string message, T0 arg0, T1 arg1)
    {
        if (logger?.IsEnabled(LogLevel.Trace) == true)
        {
            logger.LogTrace(message, arg0, arg1);
        }
    }

    public static void LogTraceGuarded<T0, T1, T2>(this ILogger? logger, string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (logger?.IsEnabled(LogLevel.Trace) == true)
        {
            logger.LogTrace(message, arg0, arg1, arg2);
        }
    }
}
#pragma warning restore CA2254
