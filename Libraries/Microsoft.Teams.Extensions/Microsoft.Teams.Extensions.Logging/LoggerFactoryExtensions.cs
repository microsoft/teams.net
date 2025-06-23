// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Extensions.Logging;

public static class LoggerFactoryExtensions
{
    public static ILoggerFactory AddTeams(this ILoggerFactory factory, Common.Logging.ILogger? logger = null)
    {
        factory.AddProvider(new TeamsLoggerProvider(new TeamsLogger(logger ?? new ConsoleLogger())));
        return factory;
    }
}