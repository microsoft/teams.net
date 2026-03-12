// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Core.Schema;

/// <summary>
/// Represents an agentic identity for user-delegated token acquisition.
/// </summary>
public sealed class AgenticIdentity
{
    /// <summary>
    /// Agentic application ID.
    /// </summary>
    public string? AgenticAppId { get; set; }
    /// <summary>
    /// Agentic user ID.
    /// </summary>
    public string? AgenticUserId { get; set; }

    /// <summary>
    /// Agentic application blueprint ID.
    /// </summary>
    public string? AgenticAppBlueprintId { get; set; }

    /// <summary>
    /// Creates an <see cref="AgenticIdentity"/> instance from the provided properties dictionary.
    /// </summary>
    /// <param name="properties"></param>
    /// <returns></returns>
    public static AgenticIdentity? FromProperties(ExtendedPropertiesDictionary? properties)
    {
        if (properties is null)
        {
            return null;
        }

        properties.TryGetValue("agenticAppId", out object? appIdObj);
        properties.TryGetValue("agenticUserId", out object? userIdObj);
        properties.TryGetValue("agenticAppBlueprintId", out object? bluePrintObj);
        return new AgenticIdentity
        {
            AgenticAppId = appIdObj?.ToString(),
            AgenticUserId = userIdObj?.ToString(),
            AgenticAppBlueprintId = bluePrintObj?.ToString()
        };
    }
}
