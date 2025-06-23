// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Microsoft.Teams.Extensions.Logging;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddTeams(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();
        builder.Services.AddSingleton<Common.Logging.ILogger, Common.Logging.ConsoleLogger>();
        builder.Services.AddSingleton<ILogger, TeamsLogger>();
        builder.Services.AddSingleton<ILoggerProvider, TeamsLoggerProvider>();
        LoggerProviderOptions.RegisterProviderOptions<Common.Logging.LoggingSettings, TeamsLoggerProvider>(builder.Services);
        return builder;
    }

    public static ILoggingBuilder AddTeams(this ILoggingBuilder builder, Common.Logging.ILogger logger)
    {
        builder.AddConfiguration();
        builder.Services.AddSingleton(logger);
        builder.Services.AddSingleton<ILogger, TeamsLogger>();
        builder.Services.AddSingleton<ILoggerProvider, TeamsLoggerProvider>();
        LoggerProviderOptions.RegisterProviderOptions<Common.Logging.LoggingSettings, TeamsLoggerProvider>(builder.Services);
        return builder;
    }
}