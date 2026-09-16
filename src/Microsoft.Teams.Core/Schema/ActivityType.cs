// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.Schema;

/// <summary>
/// Provides constant values that represent activity types used in messaging workflows.
/// </summary>
/// <remarks>Use the fields of this class to specify or compare activity types in message-based systems. This
/// class is typically used to avoid hardcoding string literals for activity type identifiers.</remarks>
public static class ActivityType
{
    /// <summary>
    /// Represents a message activity type, used for sending and receiving text and rich content.
    /// </summary>
    public const string Message = "message";
    /// <summary>
    /// Represents a typing indicator activity.
    /// </summary>
    public const string Typing = "typing";
}
