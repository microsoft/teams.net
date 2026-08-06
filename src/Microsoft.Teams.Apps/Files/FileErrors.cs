// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// Distinguishes the two ways an inbound file's short-lived download URL can be found expired.
/// </summary>
public enum FileUrlExpiredReason
{
    /// <summary>The first fetch came after the URL lapsed, so no bytes were retrieved. Recovery needs Graph drive-item re-resolution, not available via the SDK at this time.</summary>
    FirstFetch,

    /// <summary>Edge case. An earlier download succeeded, then a later re-fetch through the same handle lapsed. Avoid it by calling <c>DownloadAsync()</c> once and reusing the returned <see cref="DownloadedFile"/> rather than re-reading the handle.</summary>
    Reread,
}

/// <summary>
/// Raised when an inbound file's short-lived download URL has expired and can no longer fetch bytes.
/// <para>A personal file's pre-authorized <c>tempauth</c> download URL is valid only briefly. A fetch after it lapses gets a <c>401</c>/<c>403</c> from the platform. A handler that downloads once (and does not keep the handle) should not hit this.</para>
/// <para><see cref="Reason"/> distinguishes the two cases; see <see cref="FileUrlExpiredReason"/>.</para>
/// </summary>
public class FileUrlExpiredException : Exception
{
    /// <summary>Lets callers branch without string-matching the message. See <see cref="FileUrlExpiredReason"/>.</summary>
    public FileUrlExpiredReason Reason { get; }

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

    /// <summary>Initializes a new instance of the <see cref="FileUrlExpiredException"/> class.</summary>
    public FileUrlExpiredException() : this(FileUrlExpiredReason.FirstFetch)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileUrlExpiredException"/> class with a specified error message.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public FileUrlExpiredException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileUrlExpiredException"/> class with a specified error message and inner exception.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    /// <param name="innerException">The underlying exception that caused this exception.</param>
    public FileUrlExpiredException(string message, Exception innerException) : base(message, innerException)
    {
    }

    private static string DefaultMessage(FileUrlExpiredReason reason)
        => reason == FileUrlExpiredReason.FirstFetch
            ? "file download URL expired before any bytes were fetched; recovery needs Graph drive-item re-resolution (not available via the SDK at this time)"
            : "file download URL expired before a repeat read; reuse a single DownloadedFile from one DownloadAsync() call instead of re-reading the handle";
}

/// <summary>
/// Raised when file bytes are requested for a conversation scope whose download path is not implemented.
/// <para>Only <c>personal</c> (1:1) uploaded files download directly. <c>groupChat</c> and <c>channel</c> files are surfaced by <c>ListAsync()</c>, but fetching their bytes needs Graph; <c>DownloadAsync()</c>/<c>StreamAsync()</c> throws until that path lands.</para>
/// </summary>
public class FileScopeNotSupportedException : Exception
{
    /// <summary>The conversation scope that is not yet fetchable.</summary>
    public string? Scope { get; }

    /// <summary>Initializes a new instance of the <see cref="FileScopeNotSupportedException"/> class for the specified scope.</summary>
    /// <param name="scope">The conversation scope that is not yet fetchable.</param>
    public FileScopeNotSupportedException(string scope) : base($"downloading files from '{scope}' conversations is not supported via SDK at this time")
    {
        Scope = scope;
    }

    /// <summary>Initializes a new instance of the <see cref="FileScopeNotSupportedException"/> class for the specified scope and error message.</summary>
    /// <param name="scope">The conversation scope that is not yet fetchable.</param>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    public FileScopeNotSupportedException(string scope, string message) : base(message)
    {
        Scope = scope;
    }

    /// <summary>Initializes a new instance of the <see cref="FileScopeNotSupportedException"/> class.</summary>
    public FileScopeNotSupportedException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FileScopeNotSupportedException"/> class with a specified error message and inner exception.</summary>
    /// <param name="message">The error message that describes the reason for the exception.</param>
    /// <param name="innerException">The underlying exception that caused this exception.</param>
    public FileScopeNotSupportedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
