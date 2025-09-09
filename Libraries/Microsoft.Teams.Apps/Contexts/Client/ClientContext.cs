// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps;

/// <summary>
/// context that comes from client (tab/embed) requests
/// </summary>
public interface IClientContext
{
    /// <summary>
    /// This ID is the unique identifier assigned to the app after deployment and is critical for ensuring the correct app instance is recognized across hosts.
    /// </summary>
    public string? AppId { get; }

    /// <summary>
    /// Unique ID for the current session for use in correlating telemetry data. A session corresponds to the lifecycle of an app. A new session begins upon the creation of a webview (on Teams mobile) or iframe (in Teams desktop) hosting the app, and ends when it is destroyed.
    /// </summary>
    public string AppSessionId { get; }

    /// <summary>
    /// The Microsoft Entra tenant ID of the current user, extracted from request auth  token.
    /// </summary>
    public string TenantId { get; }

    /// <summary>
    /// The Microsoft Entra object id of the current user, extracted from the request auth token.
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// The name of the current user, extracted from the request auth token.
    /// </summary>
    public string UserName { get; }

    /// <summary>
    /// The Microsoft Teams ID for the team with which the content is associated.
    /// </summary>
    public string? TeamId { get; }

    /// <summary>
    /// The ID of the parent message from which this task module was launched.
    /// This is only available in task modules launched from bot cards.
    /// </summary>
    public string? MessageId { get; }

    /// <summary>
    /// The Microsoft Teams ID for the channel with which the content is associated.
    /// </summary>
    public string? ChannelId { get; }

    /// <summary>
    /// The Microsoft Teams ID for the chat with which the content is associated.
    /// </summary>
    public string? ChatId { get; }

    /// <summary>
    /// The Microsoft Teams ID for the conversation with which the content is associated.
    /// A conversation can be a personal/group chat or channel
    /// </summary>
    public string? ConversationId { get; }

    /// <summary>
    /// Meeting ID used by tab when running in meeting context
    /// </summary>
    public string? MeetingId { get; }

    /// <summary>
    /// The developer-defined unique ID for the page this content points to.
    /// </summary>
    public string PageId { get; }

    /// <summary>
    /// The developer-defined unique ID for the sub-page this content points to.
    /// This field should be used to restore to a specific state within a page,
    /// such as scrolling to or activating a specific piece of content.
    /// </summary>
    public string? SubPageId { get; }

    /// <summary>
    /// The MSAL entra token.
    /// </summary>
    public string AuthToken { get; }
}

public class ClientContext : IClientContext
{
    public string? AppId { get; set; }
    public required string AppSessionId { get; set; }
    public required string TenantId { get; set; }
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public string? TeamId { get; set; }
    public string? MessageId { get; set; }
    public string? ChannelId { get; set; }
    public string? ChatId { get; set; }
    public string? MeetingId { get; set; }
    public required string PageId { get; set; }
    public string? SubPageId { get; set; }
    public required string AuthToken { get; set; }
    public string? ConversationId => ChatId ?? ChannelId;
}