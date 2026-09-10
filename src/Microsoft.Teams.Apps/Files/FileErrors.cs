// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// Base class for the diagnosable failures on the inbound-file path: an expired URL, an unsupported scope, and a refused Graph read.
/// <para>Lets a caller catch those with one <c>catch</c> clause, so a new one can be added without callers changing. A transport or service failure the SDK cannot attribute, such as a Graph 5xx, is not one of these and surfaces as an <see cref="HttpRequestException"/>.</para>
/// </summary>
public class FileException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="FileException"/> class.</summary>
    public FileException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileException"/> class with a specified error message.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public FileException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileException"/> class with a specified error message and inner exception.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    /// <param name="innerException">The underlying exception that caused this exception.</param>
    public FileException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// The identity a file fetch was attempted as. Reported on <see cref="FileRetrievalException"/> so a failure names who was refused, not merely that something was.
/// </summary>
public enum FileActor
{
    /// <summary>The app's own identity, reading with application permissions.</summary>
    App,

    /// <summary>The Agentic User the bot is acting as, reading what was shared with the agent.</summary>
    AgenticUser,

}

/// <summary>
/// Why a file's bytes could not be retrieved. See <see cref="FileRetrievalException"/>.
/// </summary>
public enum FileRetrievalFailureReason
{
    /// <summary>No credential was available for the Graph call. Detectable before any HTTP request.</summary>
    NoGraphCredential,

    /// <summary>The identity used was refused by the storage service. Covers an unconsented scope, a file the identity was never granted, and a drive item that does not exist, which are indistinguishable on the wire: Graph answers all three with <c>403</c>, because telling an unauthorized caller whether a resource exists would disclose it.</summary>
    AccessDenied,
}

/// <summary>
/// Distinguishes the two ways an inbound file's short-lived download URL can be found expired.
/// </summary>
public enum FileUrlExpiredReason
{
    /// <summary>The first fetch came after the URL lapsed, so no bytes were retrieved. There is no recovery: the URL carried its own credential, and the SDK does not fall back to Graph with an app identity. The file has to be sent again.</summary>
    FirstFetch,

    /// <summary>Edge case. An earlier download succeeded, then a later re-fetch through the same handle lapsed. Avoid it by calling <c>DownloadAsync()</c> once and reusing the returned <see cref="DownloadedFile"/> rather than re-reading the handle.</summary>
    Reread,
}

/// <summary>
/// Raised when an inbound file's short-lived download URL has expired and can no longer fetch bytes.
/// <para>A personal file's pre-authorized <c>tempauth</c> download URL is valid only briefly. A fetch after it lapses gets a <c>401</c>/<c>403</c> from the platform. A handler that downloads once (and does not keep the handle) should not hit this.</para>
/// <para><see cref="Reason"/> distinguishes the two cases; see <see cref="FileUrlExpiredReason"/>.</para>
/// </summary>
public class FileUrlExpiredException : FileException
{
    /// <summary>Lets callers branch without string-matching the message. <c>null</c> when the reason was not specified, matching how <see cref="FileScopeNotSupportedException.Scope"/> reports an unknown scope. See <see cref="FileUrlExpiredReason"/>.</summary>
    public FileUrlExpiredReason? Reason { get; }

    /// <summary>Initializes a new instance of the <see cref="FileUrlExpiredException"/> class with the specified reason and a default message.</summary>
    /// <param name="reason">The reason the download URL was found expired.</param>
    public FileUrlExpiredException(FileUrlExpiredReason reason) : this(reason, DefaultMessage(reason))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileUrlExpiredException"/> class with the specified reason and error message.</summary>
    /// <param name="reason">The reason the download URL was found expired.</param>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public FileUrlExpiredException(FileUrlExpiredReason reason, string message) : base(message)
    {
        Reason = reason;
    }

    /// <summary>Initializes a new instance of the <see cref="FileUrlExpiredException"/> class. <see cref="Reason"/> is left <c>null</c>.</summary>
    public FileUrlExpiredException() : base("file download URL expired and can no longer fetch bytes")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileUrlExpiredException"/> class with a specified error message. <see cref="Reason"/> is left <c>null</c>.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public FileUrlExpiredException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileUrlExpiredException"/> class with a specified error message and inner exception. <see cref="Reason"/> is left <c>null</c>.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    /// <param name="innerException">The underlying exception that caused this exception.</param>
    public FileUrlExpiredException(string message, Exception innerException) : base(message, innerException)
    {
    }

    private static string DefaultMessage(FileUrlExpiredReason reason)
        => reason == FileUrlExpiredReason.FirstFetch
            ? "file download URL expired before any bytes were fetched. The URL is short-lived and cannot be renewed, so the file has to be sent again. Download on arrival rather than holding the handle."
            : "file download URL expired before a repeat read; reuse a single DownloadedFile from one DownloadAsync() call instead of re-reading the handle";
}

/// <summary>
/// Raised when file bytes are requested for a conversation scope whose download path is not implemented.
/// <para>Only <c>personal</c> (1:1) uploaded files download directly. <c>groupChat</c> files are surfaced by <c>ListAsync()</c>, but fetching their bytes needs Graph; <c>DownloadAsync()</c>/<c>StreamAsync()</c> throws until that path lands.</para>
/// </summary>
public class FileScopeNotSupportedException : FileException
{
    /// <summary>The conversation scope that is not yet fetchable.</summary>
    public ConversationType? Scope { get; }

    /// <summary>Initializes a new instance of the <see cref="FileScopeNotSupportedException"/> class for the specified scope.</summary>
    /// <param name="scope">The conversation scope that is not yet fetchable.</param>
    public FileScopeNotSupportedException(ConversationType? scope) : base($"downloading files from '{scope?.Value ?? "unknown"}' conversations is not supported via SDK at this time")
    {
        Scope = scope;
    }

    /// <summary>Initializes a new instance of the <see cref="FileScopeNotSupportedException"/> class for the specified scope and error message.</summary>
    /// <param name="scope">The conversation scope that is not yet fetchable.</param>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public FileScopeNotSupportedException(ConversationType? scope, string message) : base(message)
    {
        Scope = scope;
    }

    /// <summary>Initializes a new instance of the <see cref="FileScopeNotSupportedException"/> class.</summary>
    public FileScopeNotSupportedException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileScopeNotSupportedException"/> class with a specified error message.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public FileScopeNotSupportedException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileScopeNotSupportedException"/> class with a specified error message and inner exception.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    /// <param name="innerException">The underlying exception that caused this exception.</param>
    public FileScopeNotSupportedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Raised when a file's bytes could not be retrieved through Microsoft Graph.
/// <para>Distinct from <see cref="FileUrlExpiredException"/>, which means a pre-authorized URL lapsed and no usable Graph route existed. This exception means a Graph fetch was attempted and did not produce bytes.</para>
/// </summary>
public class FileRetrievalException : FileException
{
    /// <summary>Lets callers branch without string-matching the message. <c>null</c> when the reason was not specified, matching how <see cref="FileUrlExpiredException.Reason"/> reports an unspecified one. See <see cref="FileRetrievalFailureReason"/>.</summary>
    public FileRetrievalFailureReason? Reason { get; }

    /// <summary>The identity the fetch was attempted as, when one was selected. <c>null</c> when the failure preceded credential selection.</summary>
    public FileActor? Actor { get; }

    /// <summary>
    /// What the storage service itself said, verbatim and truncated, when it said anything.
    /// <para><see cref="Reason"/> deliberately collapses causes that are indistinguishable to the SDK: an unconsented scope and a file that was never shared both arrive as 403. That collapse is right for branching and wrong for diagnosis, so the original text is kept here rather than discarded.</para>
    /// </summary>
    public string? Details { get; }

    /// <summary>Initializes a new instance of the <see cref="FileRetrievalException"/> class with the specified reason, the identity the fetch was attempted as, and whatever the service said.</summary>
    /// <param name="reason">Why the bytes could not be retrieved.</param>
    /// <param name="actor">The identity the fetch was attempted as, when one was selected.</param>
    /// <param name="details">What the storage service said, already truncated.</param>
    public FileRetrievalException(FileRetrievalFailureReason reason, FileActor? actor = null, string? details = null)
        : base(string.IsNullOrEmpty(details)
            ? DefaultMessage(reason, actor)
            : $"{DefaultMessage(reason, actor)} (service said: {details})")
    {
        Reason = reason;
        Actor = actor;
        Details = details;
    }

    /// <summary>Initializes a new instance of the <see cref="FileRetrievalException"/> class. <see cref="Reason"/> and <see cref="Actor"/> are left <c>null</c>.</summary>
    public FileRetrievalException() : base("cannot fetch file bytes through Graph")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileRetrievalException"/> class with a specified error message. <see cref="Reason"/> and <see cref="Actor"/> are left <c>null</c>.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public FileRetrievalException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileRetrievalException"/> class with a specified error message and inner exception. <see cref="Reason"/> and <see cref="Actor"/> are left <c>null</c>.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    /// <param name="innerException">The underlying exception that caused this exception.</param>
    public FileRetrievalException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Names the identity in prose. Exhaustive on purpose: a new <see cref="FileActor"/> must fail the build here rather than silently inherit the app's wording, which would send that identity's failures to the wrong remedy.
    /// </summary>
    private static string DescribeActor(FileActor actor)
        // CS8524 only covers values outside the declared set, which no caller can produce. Suppressed so that a new
        // declared actor still fails the build as CS8509 instead of being waved through by a default arm.
#pragma warning disable CS8524
        => actor switch
        {
            FileActor.AgenticUser => "the agentic user",
            FileActor.App => "the app",
        };
#pragma warning restore CS8524

    /// <summary>Where to go to fix a missing credential, which differs per identity. Exhaustive for the same reason.</summary>
    private static string NoCredentialGuidance(FileActor actor)
#pragma warning disable CS8524
        => actor switch
        {
            // Linked rather than described because the agent permission model is still moving, and stale instructions in an error message are worse than none.
            FileActor.AgenticUser => "the agentic user has no usable Graph permissions. An agent identity gets Graph scopes from its blueprint's inheritable permissions or from a direct grant, and an administrator must consent to them. See https://learn.microsoft.com/entra/agent-id/concept-inheritable-permissions",
            // Not a route the SDK takes on its own: Graph file reads happen as the agentic user. An app reaching here means a file arrived in a shape that should not occur, so the remedy is not a permission grant.
            FileActor.App => "the app has no usable Graph credential for this file. Graph file retrieval is supported for Agentic Users, which read as their own identity; an app identity and/or user-delegated permissions may be used but are not supported via the SDK at this time",
        };
#pragma warning restore CS8524

    private static string DefaultMessage(FileRetrievalFailureReason reason, FileActor? actor)
    {
        string asWho = DescribeActor(actor ?? FileActor.App);

#pragma warning disable CS8524
        return reason switch
        {
            FileRetrievalFailureReason.NoGraphCredential => $"cannot fetch file bytes through Graph: {NoCredentialGuidance(actor ?? FileActor.App)}",
            FileRetrievalFailureReason.AccessDenied => $"cannot fetch file bytes through Graph: access was denied for {asWho}. The required scope may not be consented, the file may never have been shared with that identity, or the drive item may not exist",
        };
#pragma warning restore CS8524
    }
}
