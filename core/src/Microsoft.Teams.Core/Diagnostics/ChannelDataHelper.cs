// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.Diagnostics;

/// <summary>
/// Shared helpers for extracting values from the untyped <c>channelData</c> property bag.
/// </summary>
internal static class ChannelDataHelper
{
    /// <summary>
    /// Best-effort extraction of <c>channelData.tenant.id</c> from the activity's
    /// <see cref="CoreActivity.Properties"/> dictionary. Returns <see langword="null"/>
    /// when the property is missing or malformed.
    /// </summary>
    internal static string? TryReadTenantId(CoreActivity activity)
    {
        if (!activity.Properties.TryGetValue("channelData", out object? channelData) || channelData is null)
        {
            return null;
        }

        try
        {
            JsonElement root = channelData switch
            {
                JsonElement je => je,
                _ => JsonSerializer.SerializeToElement(channelData),
            };
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("tenant", out JsonElement tenant) &&
                tenant.ValueKind == JsonValueKind.Object &&
                tenant.TryGetProperty("id", out JsonElement id) &&
                id.ValueKind == JsonValueKind.String)
            {
                return id.GetString();
            }
        }
        catch (JsonException)
        {
            // Best-effort fallback; ignore malformed channelData.
        }

        return null;
    }
}
