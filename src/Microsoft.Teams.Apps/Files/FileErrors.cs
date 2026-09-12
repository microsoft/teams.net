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
/// The identity a file fetch was attempted as. Reported on <see cref="FileCredentialException"/> and <see cref="FileAccessException"/> so a failure names who was refused, not merely that something was.
/// </summary>
public enum FileActor
{
    /// <summary>The app's own identity, reading with application permissions.</summary>
    App,

    /// <summary>The Agentic User the bot is acting as, reading what was shared with the agent.</summary>
    AgenticUser,

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
/// Raised when no credential was available for the Graph call. Detectable before any HTTP request.
/// <para>Distinct from <see cref="FileUrlExpiredException"/>, which is terminal: a pre-authorized URL lapsed, and the SDK does not resolve those bytes another way even when a <c>contentUrl</c> and a Graph credential are both present. This exception means the Graph route was ruled out before the request, because no usable credential was available: either a token could not be acquired, or the one acquired carries no file-capable permission.</para>
/// </summary>
public class FileCredentialException : FileException
{
    /// <summary>The identity the fetch was attempted as, when one was selected. <c>null</c> when the failure preceded credential selection.</summary>
    public FileActor? Actor { get; }

    /// <summary>
    /// What went wrong while acquiring the token, when the attempt failed rather than simply returning nothing.
    /// <para>An acquisition that threw and an identity with no permissions both arrive here as "no token", but the fixes differ: one is a transient or configuration fault, the other is a consent problem. Local to this process; contrast <see cref="FileAccessException.Details"/>, which is the service's own words.</para>
    /// </summary>
    public string? Cause { get; }

    /// <summary>Initializes a new instance of the <see cref="FileCredentialException"/> class with the identity a credential was being resolved for and why acquisition failed.</summary>
    /// <param name="actor">The identity a credential was being resolved for, when one had been selected.</param>
    /// <param name="cause">What went wrong while acquiring the token, when the attempt failed.</param>
    public FileCredentialException(FileActor? actor, string? cause = null)
        : base(string.IsNullOrEmpty(cause)
            ? $"cannot fetch file bytes through Graph: {NoCredentialGuidance(actor)}"
            : $"cannot fetch file bytes through Graph: {NoCredentialGuidance(actor)} ({cause})")
    {
        Actor = actor;
        Cause = cause;
    }

    /// <summary>Initializes a new instance of the <see cref="FileCredentialException"/> class. <see cref="Actor"/> is left <c>null</c>.</summary>
    public FileCredentialException() : base("cannot fetch file bytes through Graph: no Graph credential was available")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileCredentialException"/> class with a specified error message. <see cref="Actor"/> is left <c>null</c>.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public FileCredentialException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileCredentialException"/> class with a specified error message and inner exception. <see cref="Actor"/> is left <c>null</c>.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    /// <param name="innerException">The underlying exception that caused this exception.</param>
    public FileCredentialException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Where to go to fix a missing credential, which differs per identity, and says so plainly when no identity had been selected.</summary>
    // CS8524 only covers values outside the declared set, which no caller can produce. Suppressed so that a new
    // declared actor still fails the build as CS8509 instead of being waved through by a default arm. The `null` arm
    // is listed explicitly for the same reason: it is a real case, not a catch-all.
#pragma warning disable CS8524
    internal static string NoCredentialGuidance(FileActor? actor) => actor switch
    {
        // Linked rather than described because the agent permission model is still moving, and stale instructions in an error message are worse than none.
        FileActor.AgenticUser => "the agentic user has no usable Graph permissions. An agent identity gets Graph scopes from its blueprint's inheritable permissions or from a direct grant, and an administrator must consent to them. See https://learn.microsoft.com/entra/agent-id/concept-inheritable-permissions",
        // Graph file reads happen as the agentic user. Granting the app file permissions would make this succeed, which is why the message says it may be used rather than that it cannot.
        FileActor.App => "the app has no usable Graph credential for this file. Graph file retrieval is supported for Agentic Users, which read as their own identity; an app identity and/or user-delegated permissions may be used but are not supported via the SDK at this time",
        // No identity was selected, so neither remedy above applies and naming one would send the reader somewhere wrong.
        null => "no Graph credential was available, and no identity had been selected when the attempt was made",
    };
#pragma warning restore CS8524
}

/// <summary>
/// Raised when the identity used was refused by the storage service.
/// <para>Distinct from <see cref="FileUrlExpiredException"/>, which is terminal: a pre-authorized URL lapsed, and the SDK does not resolve those bytes another way even when a <c>contentUrl</c> and a Graph credential are both present. This exception means the Graph route was the one that failed, refused by the service after the request was made.</para>
/// </summary>
public class FileAccessException : FileException
{
    /// <summary>Lets callers branch without string-matching the message. <c>401</c> means the token itself was rejected; <c>403</c> means the identity lacks the grant, and the two have different remedies.</summary>
    public int Status { get; }

    /// <summary>The identity the fetch was attempted as, when one was selected. <c>null</c> when the failure preceded credential selection.</summary>
    public FileActor? Actor { get; }

    /// <summary>
    /// What the storage service itself said, verbatim and truncated, when it said anything.
    /// <para>A <c>403</c> covers an unconsented scope, a file the identity was never granted, and a drive item that does not exist, which are indistinguishable on the wire: Graph answers all three with <c>403</c>, because telling an unauthorized caller whether a resource exists would disclose it. That collapse is right for branching and wrong for diagnosis, so the original text is kept here rather than discarded.</para>
    /// </summary>
    public string? Details { get; }

    /// <summary>Initializes a new instance of the <see cref="FileAccessException"/> class with the status Graph returned, the identity the fetch was attempted as, and whatever the service said.</summary>
    /// <param name="status">The HTTP status Graph returned.</param>
    /// <param name="actor">The identity the fetch was attempted as.</param>
    /// <param name="details">What the storage service said, already truncated.</param>
    public FileAccessException(int status, FileActor? actor = null, string? details = null)
        : base(string.IsNullOrEmpty(details)
            ? $"cannot fetch file bytes through Graph: {AccessGuidance(status, actor)}"
            : $"cannot fetch file bytes through Graph: {AccessGuidance(status, actor)} (service said: {details})")
    {
        Status = status;
        Actor = actor;
        Details = details;
    }

    /// <summary>Initializes a new instance of the <see cref="FileAccessException"/> class. <see cref="Status"/> is left <c>0</c>.</summary>
    public FileAccessException() : base("cannot fetch file bytes through Graph: access was denied")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileAccessException"/> class with a specified error message. <see cref="Status"/> is left <c>0</c>.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public FileAccessException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileAccessException"/> class with a specified error message and inner exception. <see cref="Status"/> is left <c>0</c>.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    /// <param name="innerException">The underlying exception that caused this exception.</param>
    public FileAccessException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Say what a refusal means, which depends on the status: a rejected token and an insufficient grant have different remedies.</summary>
    private static string AccessGuidance(int status, FileActor? actor)
    {
#pragma warning disable CS8524
        string asWho = actor switch
        {
            FileActor.AgenticUser => "the agentic user",
            FileActor.App => "the app",
            null => "the identity used",
        };
#pragma warning restore CS8524

        return status switch
        {
            401 => $"the token presented for {asWho} was rejected. It may have expired, or been issued for the wrong audience or tenant",
            403 => $"access was denied for {asWho}. The required scope may not be consented, the file may never have been shared with that identity, or the drive item may not exist",
            _ => $"the request for {asWho} was refused with status {status}",
        };
    }
}
