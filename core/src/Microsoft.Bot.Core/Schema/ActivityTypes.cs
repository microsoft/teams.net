namespace Microsoft.Bot.Core.Schema;

/// <summary>
/// Provides constant values that represent activity types used in messaging workflows.
/// </summary>
/// <remarks>Use the fields of this class to specify or compare activity types in message-based systems. This
/// class is typically used to avoid hardcoding string literals for activity type identifiers.</remarks>
public static class ActivityTypes
{
    /// <summary>
    /// Represents the default message string used for communication or display purposes.
    /// </summary>
    public const string Message = "message";
}