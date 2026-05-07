// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Microsoft.Teams.Apps.Diagnostics;

/// <summary>
/// Singletons for the Apps-level <see cref="ActivitySource"/> and <see cref="Meter"/>.
/// Internal to <c>Microsoft.Teams.Apps</c>.
/// </summary>
internal static class AppsTelemetry
{
    private static readonly string s_version = 
        typeof(AppsTelemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(AppsTelemetry).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    public static readonly ActivitySource Source =
        new(TeamsBotApplicationTelemetry.ActivitySourceName, s_version);

    public static readonly Meter Meter =
        new(TeamsBotApplicationTelemetry.MeterName, s_version);

    public static class Spans
    {
        public const string Handler = "handler";
    }

    public static class Tags
    {
        public const string HandlerType = "handler.type";
        public const string HandlerDispatch = "handler.dispatch";
    }
}
