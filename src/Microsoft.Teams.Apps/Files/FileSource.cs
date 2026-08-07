// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// Where the SDK found an inbound file.
/// <list type="bullet">
/// <item><description><see cref="BotActivity"/> files come straight from the inbound activity's attachments.</description></item>
/// <item><description><see cref="Graph"/> files are hydrated through Microsoft Graph.</description></item>
/// </list>
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<FileSource>))]
public class FileSource(string value) : StringEnum(value)
{
    /// <summary>File taken straight from the inbound activity's attachments.</summary>
    public static readonly FileSource BotActivity = new("botActivity");
    /// <summary>File hydrated through Microsoft Graph.</summary>
    public static readonly FileSource Graph = new("graph");
}

/// <summary>
/// Common file source values.
/// </summary>
public static class FileSources
{
    /// <summary>Gets the bot activity file source.</summary>
    public static FileSource BotActivity => FileSource.BotActivity;

    /// <summary>Gets the Microsoft Graph file source.</summary>
    public static FileSource Graph => FileSource.Graph;
}
