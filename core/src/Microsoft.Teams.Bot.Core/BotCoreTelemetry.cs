// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Teams.Bot.Core;

/// <summary>
/// Telemetry class for BotCore, providing a centralized ActivitySource for consistent tracing and diagnostics across the bot application.
/// This allows for better observability and monitoring of bot operations, enabling developers to track performance, identify issues, and gain insights into the bot's behavior in production environments.
/// </summary>
public static class BotCoreTelemetry
{
    /// <summary>
    /// Activity Source
    /// </summary>
    public static readonly ActivitySource ActivitySource = new (ThisAssembly.AssemblyTitle, ThisAssembly.NuGetPackageVersion);
}

/// <summary>
/// BotCoreMetrics
/// </summary>
public static class BotCoreMetrics
{
    /// <summary>
    /// Meter
    /// </summary>
    public static readonly Meter Meter = new(ThisAssembly.AssemblyTitle, ThisAssembly.NuGetPackageVersion);

    /// <summary>
    /// Counter with number of turns processed
    /// </summary>
    public static readonly Counter<long> BotTurnsCounter = Meter.CreateCounter<long>(
        "bot_turns_total",
        "Total number of bot turns processed, categorized by outcome.",
        "outcome");

    /// <summary>
    /// Historogram for processing duration of bot turns
    /// </summary>
    public static readonly Histogram<double> ProcessingDuration =
       Meter.CreateHistogram<double>(
           "bot_processing_duration",
           unit: "ms",
           description: "Time to process a bot turn ");
}


///// <summary>
///// BotCore OTel Extensions
///// </summary>
//public static class BotCoreOpenTelemetryExtensions
//{
//    // For traces — the app calls: .AddSource("MyCompany.MyLibrary")
//    public static TracerProviderBuilder AddMyLibraryInstrumentation(
//        this TracerProviderBuilder builder)
//        => builder.AddSource(MyLibraryTelemetry.ActivitySource.Name);

//    // For metrics — the app calls: .AddMeter("MyCompany.MyLibrary")
//    public static MeterProviderBuilder AddMyLibraryInstrumentation(
//        this MeterProviderBuilder builder)
//        => builder.AddMeter(MyLibraryMetrics.Meter.Name);
//}
