// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Logging;

public class LoggingSettings
{
    public string Enable { get; set; } = "*";
    public LogLevel Level { get; set; } = LogLevel.Info;
}